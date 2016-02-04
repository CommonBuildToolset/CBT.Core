using CBT.Core.Internal;
using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Threading;

namespace CBT.Core.Tasks
{
    public sealed class RestoreModules : ITask
    {
        private readonly CBTTaskLogHelper _log;

        public RestoreModules()
        {
            _log = new CBTTaskLogHelper(this);
        }

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

                    _log.LogMessage(MessageImportance.Low, "Create CBT module imports");

                    ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator();

                    return modulePropertyGenerator.Generate(OutputFile, PropertyNamePrefix, PropertyValuePrefix, IncludeAllVersions, ImportRelativePaths, new[] {PackageConfig}, BeforeModuleImports, AfterModuleImports);
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
        }

        public bool Execute(string outputFile, string restoreCommand, string restoreArguments, string packageConfig, string packagesPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] beforeModuleImports, string[] afterModuleImports, string[] moduleInputs)
        {
            if (CoreUtility.IsFileUpToDate(outputFile, moduleInputs))
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

            BuildEngine = new CBTBuildEngine();

            return Execute();
        }

        private void RestorePackages()
        {
            DataReceivedEventHandler outputDataReceived = (sender, args) =>
            {
                if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                {
                    _log.LogMessage(args.Data);
                }
            };

            DataReceivedEventHandler errorDataReceived = (sender, args) =>
            {
                if (args != null && !String.IsNullOrWhiteSpace(args.Data))
                {
                    _log.LogError(args.Data);
                }
            };

            _log.LogMessage(MessageImportance.High, "Restore CBT modules:");

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = RestoreArguments,
                    CreateNoWindow = true,
                    FileName = RestoreCommand,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
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

                process.StandardInput.Close();

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
                            _log.LogMessage("Exception occurred while ending process: {0}", e.Message);
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

            _log.LogMessage("Successfully restored CBT module packages");
        }
    }
}