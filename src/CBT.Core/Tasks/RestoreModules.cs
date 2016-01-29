using CBT.Core.Internal;
using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Threading;

namespace CBT.Core.Tasks
{
    public sealed class RestoreModules : ITask
    {
        private static readonly string TaskName = typeof (RestoreModules).Name;

        public string[] AfterModuleImports { get; set; }

        public string[] BeforeModuleImports { get; set; }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        public string[] ImportRelativePaths { get; set; }

        public bool IncludeAllVersions { get; set; }

        public string[] ModuleInputs { get; set; }

        [Required]
        public string OutputFile { get; set; }

        [Required]
        public string PackageConfig { get; set; }

        [Required]
        public string PackagesPath { get; set; }

        [Required]
        public string PropertyNamePrefix { get; set; }

        [Required]
        public string PropertyValuePrefix { get; set; }

        [Required]
        public string RestoreArguments { get; set; }

        [Required]
        public string RestoreCommand { get; set; }

        public bool Execute()
        {
            // Get a mutex name based on the output file
            //
            string mutexName = OutputFile.GetHashCode().ToString("X");

            using (var mutex = new Mutex(false, mutexName))
            {
                if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
                {
                    return false;
                }

                try
                {
                    RestorePackages();

                    LogMessage(MessageImportance.Low, "Create CBT module imports");

                    ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator();

                    return modulePropertyGenerator.Generate(OutputFile, PropertyNamePrefix, PropertyValuePrefix, IncludeAllVersions, ImportRelativePaths, new[] {PackageConfig}, BeforeModuleImports, AfterModuleImports);
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                    LogMessage(e.ToString());

                    return false;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public bool Execute(string outputFile, string restoreCommand, string restoreArguments, string packageConfig, string packagesPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] beforeModuleImports, string[] afterModuleImports, string[] moduleInputs)
        {
            BuildEngine = new CBTBuildEngine();

            if (!CoreUtility.IsFileOutOfDate(outputFile, moduleInputs))
            {
                return true;
            }

            PackageConfig = packageConfig;
            PackagesPath = packagesPath;
            PropertyNamePrefix = propertyNamePrefix;
            PropertyValuePrefix = propertyValuePrefix;
            OutputFile = outputFile;
            RestoreCommand = restoreCommand;
            RestoreArguments = restoreArguments;
            IncludeAllVersions = includeAllVersions;
            ImportRelativePaths = importRelativePaths;
            BeforeModuleImports = beforeModuleImports;
            AfterModuleImports = afterModuleImports;
            ModuleInputs = moduleInputs;

            return Execute();
        }

        /// <summary>
        /// Logs a critical message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogCritical(string message)
        {
            LogCritical(message, null);
        }

        /// <summary>
        /// Logs a critical message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogCritical(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            BuildEngine.LogMessageEvent(new CriticalBuildMessageEventArgs(null, null, null, 0, 0, 0, 0, message, null, null, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogError(string message)
        {
            LogError(message, null);
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogError(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            BuildEngine.LogErrorEvent(new BuildErrorEventArgs(null, null, null, 0, 0, 0, 0, message, null, TaskName, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogMessage(string message, params object[] args)
        {
            LogMessage(MessageImportance.Normal, message, args);
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="importance">The importance level of the message.</param>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogMessage(MessageImportance importance, string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, TaskName, importance, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogWarning(string message)
        {
            LogWarning(message, null);
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        private void LogWarning(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            BuildEngine.LogWarningEvent(new BuildWarningEventArgs(null, null, null, 0, 0, 0, 0, message, null, TaskName, DateTime.UtcNow, args));
        }

        private void RestorePackages()
        {
            DataReceivedEventHandler outputDataReceived = (sender, args) =>
            {
                if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                {
                    LogMessage(args.Data);
                }
            };

            DataReceivedEventHandler errorDataReceived = (sender, args) =>
            {
                if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                {
                    LogError(args.Data);
                }
            };

            LogMessage(MessageImportance.High, "Restore CBT modules:");

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = RestoreArguments,
                    CreateNoWindow = true,
                    FileName = RestoreCommand,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                },
            };

            process.OutputDataReceived += outputDataReceived;
            process.ErrorDataReceived += errorDataReceived;

            try
            {
                if (!process.Start())
                {
                    throw new Exception(String.Format("Failed to start process \"{0} {1}\"", process.StartInfo.FileName, process.StartInfo.Arguments));
                }

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                if (!process.WaitForExit((int) TimeSpan.FromMinutes(29).TotalMilliseconds))
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception e)
                        {
                            LogMessage("Exception occurred while ending process: {0}", e.Message);
                        }
                    }

                    throw new Exception("Timed out waiting for the CBT module restore command to complete.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.");
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception(String.Format("Restoring CBT modules failed with an exit code of '{0}'.  More information about the problem including the output of the restoration command is available when the log verbosity is set to Detailed.", process.ExitCode));
                }
            }
            finally
            {
                process.OutputDataReceived -= outputDataReceived;
                process.ErrorDataReceived -= errorDataReceived;
            }

            LogMessage("Successfully restored CBT module packages");
        }
    }
}