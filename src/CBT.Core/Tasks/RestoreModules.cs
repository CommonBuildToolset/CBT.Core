using CBT.Core.Internal;
using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CBT.Core.Tasks
{
    public sealed class RestoreModules : ICancelableTask, IDisposable
    {
        private const string NuGetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";

        private static readonly TimeSpan RestoreTimeout = TimeSpan.FromMinutes(30);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly CBTTaskLogHelper _log;

        public RestoreModules()
        {
            _log = new CBTTaskLogHelper(this);
        }

        public string[] AfterImports { get; set; }

        public string[] BeforeImports { get; set; }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        [Required]
        public string ConfigPath { get; set; }

        [Required]
        public string ExtensionsPath { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        [Required]
        public string[] ImportRelativePaths { get; set; }

        [Required]
        public string ImportsFile { get; set; }

        public string[] Inputs { get; set; }

        [Required]
        public string PackageConfig { get; set; }

        [Required]
        public string PackagesPath { get; set; }

        [Required]
        public string PropertyNamePrefix { get; set; }

        [Required]
        public string PropertyValuePrefix { get; set; }

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
            // Get a mutex name based on the output file
            //
            string mutexName = ImportsFile.GetHashCode().ToString("X");

            using (var mutex = new Mutex(false, mutexName))
            {
                if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
                {
                    return false;
                }

                try
                {
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

                    if (!modulePropertyGenerator.Generate(ImportsFile, ExtensionsPath, ConfigPath, PropertyNamePrefix, PropertyValuePrefix, ImportRelativePaths, BeforeImports, AfterImports))
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
                    mutex.ReleaseMutex();
                }
            }

            return true;
        }

        public bool Execute(string[] afterImports, string[] beforeImports, string configPath, string extensionsPath, string[] importRelativePaths, string importsFile, string[] inputs, string packageConfig, string packagesPath, string propertyNamePrefix, string propertyValuePrefix, string restoreCommand, string restoreCommandArguments)
        {
            if (Directory.Exists(packagesPath) && IsFileUpToDate(importsFile, inputs))
            {
                return true;
            }

            AfterImports = afterImports;
            BeforeImports = beforeImports;
            ConfigPath = configPath;
            ExtensionsPath = extensionsPath;
            ImportRelativePaths = importRelativePaths;
            ImportsFile = importsFile;
            Inputs = inputs;
            PackageConfig = packageConfig;
            PackagesPath = packagesPath;
            PropertyNamePrefix = propertyNamePrefix;
            PropertyValuePrefix = propertyValuePrefix;
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
            _log.LogMessage("Downloading NuGet.");

            try
            {
                string directory = Path.GetDirectoryName(RestoreCommand);

                if (!String.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var downloadTask = Task.Run(() =>
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(new Uri(NuGetUrl), RestoreCommand);
                    }
                }, _cancellationTokenSource.Token);

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2), _cancellationTokenSource.Token);

                var completedTask = await Task.WhenAny(downloadTask, timeoutTask).ConfigureAwait(continueOnCapturedContext: false);

                if (completedTask == downloadTask)
                {
                    return true;
                }

                _log.LogError("Timed out downloading NuGet from '{0}'", NuGetUrl);
            }
            catch (TaskCanceledException)
            {
                // Ignored because we're canceling
            }
            catch (Exception e)
            {
                _log.LogError(e.ToString());
            }

            if (File.Exists(RestoreCommand))
            {
                // Delete any file that was partially downloaded when canceling
                //
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.Delete(RestoreCommand);
                        break;
                    }
                    catch (Exception)
                    {
                        // Ignored because in some cases the file is in use still
                        //
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(200));
                }
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