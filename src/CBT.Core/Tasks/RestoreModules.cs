using CBT.Core.Internal;
using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CBT.Core.Tasks
{
    public sealed class RestoreModules : ICancelableTask, IDisposable
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
        /// <param name="buildEngine">An <see cref="IBuildEngine"/> instance to use for logging.</param>
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

        public string NuGetDownloaderAssemblyPath { get; set; }

        public string NuGetDownloaderClassName { get; set; }

        public string NuGetDownloaderArguments { get; set; }

        [Required]
        public string PackageConfig { get; set; }

        [Required]
        public string PackagesPath { get; set; }

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

            bool releaseSemaphore;

            using (Semaphore semaphore = new Semaphore(0, 1, semaphoreName, out releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        releaseSemaphore = semaphore.WaitOne(TimeSpan.FromMinutes(30));

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

                    if (!RestorePackages())
                    {
                        return false;
                    }

                    _log.LogMessage(MessageImportance.Low, "Create CBT module imports");

                    ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator(PackagesPath, PackageConfig);

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

        public bool Execute(string[] afterImports, string[] beforeImports, string extensionsPath, string importsFile, string nuGetDownloaderAssemblyPath, string nuGetDownloaderClassName, string nuGetDownloaderArguments, string[] inputs, string packageConfig, string packagesPath, string restoreCommand, string restoreCommandArguments)
        {
            if (Directory.Exists(packagesPath) && IsFileUpToDate(importsFile, inputs))
            {
                return true;
            }

            AfterImports = afterImports;
            BeforeImports = beforeImports;
            ExtensionsPath = extensionsPath;
            ImportsFile = importsFile;
            Inputs = inputs;
            NuGetDownloaderAssemblyPath = nuGetDownloaderAssemblyPath;
            NuGetDownloaderClassName = nuGetDownloaderClassName;
            NuGetDownloaderArguments = nuGetDownloaderArguments;
            PackageConfig = packageConfig;
            PackagesPath = packagesPath;
            RestoreCommand = restoreCommand;
            RestoreCommandArguments = restoreCommandArguments;

            BuildEngine = new CBTBuildEngine();

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

        /// <summary>
        /// Determines if a file is up-to-date in relation to the specified paths.
        /// </summary>
        /// <param name="input">The file to check if it is out-of-date.</param>
        /// <param name="outputs">The list of files to check against.</param>
        /// <returns><code>true</code> if the file does not exist or it is older than any of the other files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <code>null</code>.</exception>
        private static bool IsFileUpToDate(string input, params string[] outputs)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException("input");
            }

            if (!File.Exists(input) || outputs == null || outputs.Length == 0)
            {
                return false;
            }

            long lastWriteTime = File.GetLastWriteTimeUtc(input).Ticks;

            return outputs.All(output => File.Exists(output) && File.GetLastWriteTimeUtc(output).Ticks <= lastWriteTime);
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

                Assembly assembly = Assembly.LoadFrom(NuGetDownloaderAssemblyPath);

                Type type = assembly.GetType(NuGetDownloaderClassName, throwOnError: true);

                NuGetDownloader nuGetDownloader;
                try
                {
                    nuGetDownloader = Delegate.CreateDelegate(typeof (NuGetDownloader), type, "Execute", ignoreCase: true, throwOnBindFailure: true) as NuGetDownloader;
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(String.Format("Specified static method \"{0}.Execute\" does not match required signature 'bool Execute(string, string, Action<string>, Action<string>, CancellationToken)'", NuGetDownloaderClassName), e);
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

        private bool RestorePackages()
        {
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
                    WorkingDirectory = Environment.CurrentDirectory,
                },
            })
            {
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                    {
                        _log.LogError(args.Data);
                    }
                };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                    {
                        _log.LogMessage(args.Data);
                    }
                };

                process.Exited += (sender, args) => processExited.Set();

                _log.LogMessage(MessageImportance.Low, "{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                if (!process.Start())
                {
                    throw new Exception(String.Format("Failed to start process \"{0} {1}\"", process.StartInfo.FileName, process.StartInfo.Arguments));
                }

                process.StandardInput.Close();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                int eventIndex = WaitHandle.WaitAny(new[] {processExited, _cancellationTokenSource.Token.WaitHandle}, RestoreTimeout);

                if (eventIndex == 0)
                {
                    if (process.ExitCode != 0)
                    {
                        throw new Exception(String.Format("Restoring CBT modules failed with an exit code of '{0}'.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.", process.ExitCode));
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

                if (eventIndex == WaitHandle.WaitTimeout)
                {
                    throw new Exception("Timed out waiting for the CBT module restore command to complete.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.");
                }
            }

            return false;
        }
    }
}