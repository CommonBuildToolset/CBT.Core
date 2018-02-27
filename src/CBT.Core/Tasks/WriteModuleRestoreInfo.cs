using System;
using System.IO;
using System.Linq;
using System.Threading;
using CBT.Core.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace CBT.Core.Tasks
{
    public class WriteModuleRestoreInfo : ITask
    {

        [Required]
        public ITaskItem[] Input { get; set; }

        [Required]
        public string File { get; set; }

        protected bool RunOnceOnly { get; } = true;

        protected TimeSpan SemaphoreTimeout { get; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            bool result = false;
            using (Semaphore semaphore = new Semaphore(0, 1, File.GetHash(), out bool releaseSemaphore))
            {
                try
                {
                    // releaseSemaphore is false if a new semaphore was not acquired
                    if (!releaseSemaphore)
                    {
                        // Wait for the semaphore
                        releaseSemaphore = semaphore.WaitOne(SemaphoreTimeout);

                        if (RunOnceOnly)
                        {
                            // Return if another thread did the work and the task is marked to only run once (the default)
                            return releaseSemaphore;
                        }
                    }

                    result = Run();
                }
                finally
                {
                    if (releaseSemaphore)
                    {
                        semaphore.Release();
                    }
                }
            }

            return result;
        }

        public bool Run()
        {
            if (!Directory.Exists(Path.GetDirectoryName(File)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(File));
            }

            if (System.IO.File.Exists(File))
            {
                System.IO.File.Delete(File);
            }

            ModuleRestoreInfo assetsFile = new ModuleRestoreInfo
            {
                ProjectJsonPath = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("ProjectJsonPath", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                RestoreProjectStyle = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("RestoreProjectStyle", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                RestoreOutputAbsolutePath = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("RestoreOutputAbsolutePath", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                PackageImportOrder = Input
                    .Where(i => i.ItemSpec.Equals("PackageReference", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.GetMetadata("id")))
                    .Select(i => new RestorePackage(i.GetMetadata("id"), i.GetMetadata("version"))).ToList()
            };
            System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(assetsFile, Formatting.Indented));
            return System.IO.File.Exists(File);
        }
    }
}
