using CBT.Core.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using NuGet.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CBT.Core.Tasks
{
    [LoadInSeparateAppDomain]
    public sealed class RestoreModules : MarshalByRefObject, ICancelableTask, IDisposable
    {
        private static readonly TimeSpan NuGetDownloadTimeout = TimeSpan.FromMinutes(2);

        private static readonly TimeSpan RestoreTimeout = TimeSpan.FromMinutes(30);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly CBTTaskLogHelper _log;

        public RestoreModules()
        {
            _log = new CBTTaskLogHelper(this);
        }

        /// <summary>
        /// Represents the method signature of a NuGet downloader.
        /// </summary>
        /// <param name="path">The directory path of where to download NuGet.exe to.</param>
        /// <param name="arguments">Optional arguments to pass to the downloader.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to use for receiving cancellation notifications.</param>
        /// <returns><code>true</code> if NuGet was successfully downloaded, otherwise <code>false</code>.</returns>
        private delegate bool NuGetDownloader(string path, string arguments, Action<string> logInfo, Action<string> logError, CancellationToken cancellationToken);

        public string[] AfterImports { get; set; }

        public string[] BeforeImports { get; set; }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        [Required]
        public string ExtensionsPath { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        [Required]
        public string ImportsFile { get; set; }

        public string[] Inputs { get; set; }

        public string MSBuildBinPath { get; set; }

        public string NuGetDownloaderArguments { get; set; }

        public string NuGetDownloaderAssemblyPath { get; set; }

        public string NuGetDownloaderClassName { get; set; }

        [Required]
        public string PackageConfig { get; set; }

        [Required]
        public string ProjectFullPath { get; set; }

        public string RestoreCommand { get; set; }

        [Required]
        public string RestoreCommandArguments { get; set; }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }

        public bool Execute()
        {
            // Get a semaphore name based on the output file
            //
            string semaphoreName = ImportsFile.ToUpper().GetHashCode().ToString("X");

            using (Semaphore semaphore = new Semaphore(0, 1, semaphoreName, out bool releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        _log.LogMessage(MessageImportance.Low, "Another project is already restoring CBT modules.  Waiting for it to complete.");
                        releaseSemaphore = semaphore.WaitOne(RestoreTimeout);

                        return releaseSemaphore;
                    }

                    _log.LogMessage(MessageImportance.High, "Restore CBT modules:");

                    if (!File.Exists(RestoreCommand))
                    {
                        if (!DownloadNuGet().Result)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        _log.LogMessage(MessageImportance.Low, $"Restore command '{RestoreCommand}' already exists so it was not downloaded.");
                    }

                    if (!TryRestorePackages(out ModuleRestoreInfo packageRestoreData))
                    {
                        return false;
                    }

                    _log.LogMessage(MessageImportance.Low, "Create CBT module imports");

                    ISettings settings = Settings.LoadDefaultSettings(Path.GetDirectoryName(PackageConfig), configFileName: null, machineWideSettings: new XPlatMachineWideSetting());

                    ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator(settings, _log, packageRestoreData, PackageConfig);

                    if (!modulePropertyGenerator.Generate(ImportsFile, ExtensionsPath, BeforeImports, AfterImports))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e.Message);
                    _log.LogMessage(e.ToString());

                    return false;
                }
                finally
                {
                    if (releaseSemaphore)
                    {
                        semaphore.Release();
                    }
                }
            }

            return true;
        }

        public bool Execute(string[] afterImports, string[] beforeImports, string extensionsPath, string importsFile, string nuGetDownloaderAssemblyPath, string nuGetDownloaderClassName, string nuGetDownloaderArguments, string[] inputs, string packageConfig, string restoreCommand, string restoreCommandArguments, string projectFullPath, string msbuildBinPath)
        {
            BuildEngine = new CBTBuildEngine();

            _log.LogMessage(MessageImportance.Low, "CBT Module Restore Properties:");
            _log.LogMessage(MessageImportance.Low, $"  AfterImports = {String.Join(";", afterImports)}");
            _log.LogMessage(MessageImportance.Low, $"  BeforeImports = {String.Join(";", beforeImports)}");
            _log.LogMessage(MessageImportance.Low, $"  ExtensionsPath = {extensionsPath}");
            _log.LogMessage(MessageImportance.Low, $"  ImportsFile = {importsFile}");
            _log.LogMessage(MessageImportance.Low, $"  Inputs = {String.Join(";", inputs)}");
            _log.LogMessage(MessageImportance.Low, $"  MSBuildBinPath = {msbuildBinPath}");
            _log.LogMessage(MessageImportance.Low, $"  NuGetDownloaderArguments = {nuGetDownloaderArguments}");
            _log.LogMessage(MessageImportance.Low, $"  NuGetDownloaderAssemblyPath = {nuGetDownloaderAssemblyPath}");
            _log.LogMessage(MessageImportance.Low, $"  NuGetDownloaderClassName = {nuGetDownloaderClassName}");
            _log.LogMessage(MessageImportance.Low, $"  PackageConfig = {packageConfig}");
            _log.LogMessage(MessageImportance.Low, $"  ProjectFullPath = {projectFullPath}");
            _log.LogMessage(MessageImportance.Low, $"  RestoreCommand = {restoreCommand}");
            _log.LogMessage(MessageImportance.Low, $"  RestoreCommandArguments = {restoreCommandArguments}");

            if (IsFileUpToDate(importsFile, inputs))
            {
                _log.LogMessage(MessageImportance.Low, "Skipping module restoration because everything is up-to-date.");
                return true;
            }

            AfterImports = afterImports;
            BeforeImports = beforeImports;
            ExtensionsPath = extensionsPath;
            ImportsFile = importsFile;
            Inputs = inputs;
            MSBuildBinPath = msbuildBinPath;
            NuGetDownloaderAssemblyPath = nuGetDownloaderAssemblyPath;
            NuGetDownloaderClassName = nuGetDownloaderClassName;
            NuGetDownloaderArguments = nuGetDownloaderArguments;
            PackageConfig = packageConfig;
            ProjectFullPath = projectFullPath;
            RestoreCommand = restoreCommand;
            RestoreCommandArguments = restoreCommandArguments;

            Console.CancelKeyPress += (sender, args) =>
            {
                if (args.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    args.Cancel = true;

                    _cancellationTokenSource.Cancel();
                }
            };

            return Execute();
        }

        private async Task<bool> DownloadNuGet()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(NuGetDownloaderAssemblyPath) || String.IsNullOrWhiteSpace(NuGetDownloaderClassName))
                {
                    throw new ArgumentException("NuGetDownloaderAssemblyPath and NuGetDownloaderClassName must be specified to download NuGet");
                }

                _log.LogMessage("Preparing to download NuGet");

                string directory = Path.GetDirectoryName(RestoreCommand);

                if (!String.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Assembly assembly;
                try
                {
                    _log.LogMessage(MessageImportance.Low, $"Loading NuGet downloader assembly from '{NuGetDownloaderAssemblyPath}'.");
                    assembly = Assembly.LoadFrom(NuGetDownloaderAssemblyPath);
                }
                catch (FileLoadException)
                {
                    _log.LogMessage(MessageImportance.Low, "NuGet downloader assembly failed to load, falling back to unsafe load.");
                    // FileLoadException can happen if the NuGet downloader assembly isn't trusted by the OS so as a fall back use UnsafeLoadFrom()
                    // to "bypass some security checks"
                    //
                    assembly = Assembly.UnsafeLoadFrom(NuGetDownloaderAssemblyPath);
                }

                _log.LogMessage(MessageImportance.Low, $"Getting NuGet downloader type '{NuGetDownloaderClassName}'.");

                Type type = assembly.GetType(NuGetDownloaderClassName, throwOnError: true);

                NuGetDownloader nuGetDownloader;
                try
                {
                    nuGetDownloader = Delegate.CreateDelegate(typeof(NuGetDownloader), type, "Execute", ignoreCase: true, throwOnBindFailure: true) as NuGetDownloader;
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"Specified static method \"{NuGetDownloaderClassName}.Execute\" does not match required signature 'bool Execute(string, string, Action<string>, Action<string>, CancellationToken)'", e);
                }

                // ReSharper disable once PossibleNullReferenceException
                Task downloadTask = Task.Run(() => nuGetDownloader(directory, NuGetDownloaderArguments, message => _log.LogMessage(message), message => _log.LogError(message), _cancellationTokenSource.Token), _cancellationTokenSource.Token);

                Task timeoutTask = Task.Delay(NuGetDownloadTimeout, _cancellationTokenSource.Token);

                Task completedTask = await Task.WhenAny(downloadTask, timeoutTask).ConfigureAwait(continueOnCapturedContext: false);

                if (completedTask == downloadTask)
                {
                    if (downloadTask.IsFaulted && downloadTask.Exception != null)
                    {
                        throw downloadTask.Exception;
                    }

                    if (File.Exists(RestoreCommand))
                    {
                        return true;
                    }

                    _log.LogError("The NuGet downloader succeeded but did the path '{0}' does not exist or is inaccessible.", RestoreCommand);

                    return false;
                }

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();

                    _log.LogError("Timed out downloading NuGet.");
                }

                await downloadTask.ConfigureAwait(continueOnCapturedContext: false);

                return false;
            }
            catch (OperationCanceledException)
            {
                // Ignored because we're canceling
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    e = ((AggregateException) e).Flatten().InnerExceptions.Last();
                }

                _log.LogError("Cannot download NuGet.  {0}", e.Message);
                _log.LogMessage(MessageImportance.Low, e.ToString());
            }

            return false;
        }

        /// <summary>
        /// Determines if a file is up-to-date in relation to the specified paths.
        /// </summary>
        /// <param name="input">The file to check if it is out-of-date.</param>
        /// <param name="outputs">The list of files to check against.</param>
        /// <returns><code>true</code> if the file does not exist or it is older than any of the other files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <code>null</code>.</exception>
        private bool IsFileUpToDate(string input, params string[] outputs)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!File.Exists(input))
            {
                _log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because it does not exist.");
                return false;
            }
            if (outputs == null || outputs.Length == 0)
            {
                _log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because no outputs were specified.");
                return false;
            }

            DateTime lastWriteTime = File.GetLastWriteTimeUtc(input);

            foreach (var output in outputs)
            {
                if (!File.Exists(output))
                {
                    _log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because the output file '{output}' does not exist.");
                    return false;
                }

                var outputLastWriteTime = File.GetLastWriteTimeUtc(output);

                if (outputLastWriteTime.Ticks > lastWriteTime.Ticks)
                {
                    _log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because the output file '{output}' is newer ({lastWriteTime:O} > {outputLastWriteTime:O}).");
                    return false;
                }
            }
            return true;
        }

        private bool TryRestorePackages(out ModuleRestoreInfo moduleRestoreInfo)
        {
            moduleRestoreInfo = null;

            bool isPackagesConfig = PackageConfig.EndsWith("packages.config", StringComparison.OrdinalIgnoreCase);

            string moduleRestoreInfoFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tmp");

            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(RestoreCommand);

                if (fileVersionInfo.ProductMajorPart >= 4 && RestoreCommandArguments.IndexOf("-MSBuildPath", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    string msbuildPathArgument = $" -MSBuildPath \"{MSBuildBinPath}\"";

                    _log.LogMessage(MessageImportance.Low, $"Adding '{msbuildPathArgument}' to the restore command arguments for NuGet {fileVersionInfo.ProductVersion}");

                    RestoreCommandArguments += msbuildPathArgument;
                }
            }
            catch (Exception)
            {
                _log.LogWarning($"Failed to get assembly version information from assembly '{RestoreCommand}'.  Verify that the assembly is not corrupted and is a valid NuGet.exe.");
            }

            using (ManualResetEvent processExited = new ManualResetEvent(false))
            using (Process process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = RestoreCommandArguments,
                    CreateNoWindow = true,
                    FileName = RestoreCommand,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(ProjectFullPath) ?? Environment.CurrentDirectory,
                },
            })
            {
                process.StartInfo.EnvironmentVariables["RestoreCBTModules"] = "false";

                if (!isPackagesConfig)
                {
                    process.StartInfo.EnvironmentVariables["CBTModuleRestoreInfoFile"] = moduleRestoreInfoFile;
                }

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!String.IsNullOrWhiteSpace(args?.Data))
                    {
                        _log.LogError(args.Data);
                    }
                };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!String.IsNullOrWhiteSpace(args?.Data))
                    {
                        _log.LogMessage(args.Data);
                    }
                };

                // ReSharper disable once AccessToDisposedClosure
                process.Exited += (sender, args) => processExited.Set();

                _log.LogMessage(MessageImportance.Low, $"{process.StartInfo.FileName} {process.StartInfo.Arguments} (In '{process.StartInfo.WorkingDirectory}')");

                if (!process.Start())
                {
                    throw new Exception($"Failed to start process \"{process.StartInfo.FileName} {process.StartInfo.Arguments}\"");
                }

                process.StandardInput.Close();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                int eventIndex = WaitHandle.WaitAny(new[] {processExited, _cancellationTokenSource.Token.WaitHandle}, RestoreTimeout);

                if (eventIndex == 0)
                {
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Restoring CBT modules failed with an exit code of '{process.ExitCode}'.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.");
                    }

                    if (_log.HasLoggedErrors)
                    {
                        throw new Exception("Restoring CBT modules failed because of one or more errors.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.");
                    }

                    if (isPackagesConfig)
                    {
                        moduleRestoreInfo = new ModuleRestoreInfo
                        {
                            RestoreProjectStyle = "PackagesConfig",
                        };
                    }
                    else
                    {
                        if (!File.Exists(moduleRestoreInfoFile))
                        {
                            _log.LogError($"The NuGet restore data file '{moduleRestoreInfoFile}' does not exist.  Ensure that your '{PackageConfig}' project is using PackageReference elements and that you are using NuGet.exe v4 or later.");

                            return false;
                        }

                        moduleRestoreInfo = JsonConvert.DeserializeObject<ModuleRestoreInfo>(File.ReadAllText(moduleRestoreInfoFile));
                    }

                    return true;
                }

                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception e)
                    {
                        _log.LogMessage("Exception occurred while ending process: {0}", e.Message);
                    }
                }

                if (isPackagesConfig && File.Exists(moduleRestoreInfoFile))
                {
                    try
                    {
                        File.Delete(moduleRestoreInfoFile);
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }

                if (eventIndex == WaitHandle.WaitTimeout)
                {
                    throw new Exception("Timed out waiting for the CBT module restore command to complete.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.");
                }
            }

            return false;
        }
    }
}