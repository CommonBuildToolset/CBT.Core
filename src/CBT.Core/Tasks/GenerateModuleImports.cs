using CBT.Core.Internal;
using Microsoft.Build.Framework;
using System;
using System.Threading;

namespace CBT.Core.Tasks
{
    public sealed class GenerateModuleImports : ITask
    {
        private readonly  CBTTaskLogHelper _log;
        public GenerateModuleImports()
        {
            _log = new CBTTaskLogHelper(this);
        }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        [Required]
        public string[] ImportFiles { get; set; }

        public string[] ImportRelativePaths { get; set; }

        public bool IncludeAllVersions { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string PackageConfig { get; set; }

        [Required]
        public string PackagesPath { get; set; }

        [Required]
        public string PropertyNamePrefix { get; set; }

        [Required]
        public string PropertyValuePrefix { get; set; }

        public bool Execute()
        {
            // Get a mutex name based on the output file
            //
            string mutexName = OutputPath.GetHashCode().ToString("X");

            using (var mutex = new Mutex(false, mutexName))
            {
                if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
                {
                    return false;
                }

                try
                {
                    _log.LogMessage(MessageImportance.Low, "Create CBT module imports");

                    ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator();

                    return modulePropertyGenerator.GenerateModuleImports(OutputPath, PropertyNamePrefix, PropertyValuePrefix, ImportFiles, ImportRelativePaths, new[] {PackageConfig});
                }
                catch (Exception e)
                {
                    _log.LogError(e.Message);
                    _log.LogMessage(MessageImportance.Low, e.ToString());

                    return false;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public bool Execute(string outputPath, string propertyNamePrefix, string propertyValuePrefix, string[] importFiles, string[] importRelativePaths, string packageConfig)
        {
            OutputPath = outputPath;
            PropertyNamePrefix = propertyNamePrefix;
            PropertyValuePrefix = propertyValuePrefix;
            ImportFiles = importFiles;
            ImportRelativePaths = importRelativePaths;
            PackageConfig = packageConfig;

            BuildEngine = new CBTBuildEngine();

            return Execute();
        }
    }
}