using System;
using System.IO;
using System.Linq;
using CBT.Core.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace CBT.Core.Tasks
{
    public class WriteAssetsFlagJsonFile : ITask
    {

        [Required]
        public ITaskItem[] Input { get; set; }

        [Required]
        public string File { get; set; }

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
            if (!Directory.Exists(Path.GetDirectoryName(File)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(File));
            }
            if (System.IO.File.Exists(File))
            {
                System.IO.File.Delete(File);
            }
            PackageRestoreData assetsFile = new PackageRestoreData
            {
                RestoreOutputAbsolutePath = Input
                    .First(i => i.ItemSpec.Equals("RestoreOutputAbsolutePath", StringComparison.OrdinalIgnoreCase))
                    .GetMetadata("value"),
                PackageImportOrder = Input
                    .Where(i => i.ItemSpec.Equals("PackageReference", StringComparison.OrdinalIgnoreCase))
                    .Select(i => new RestorePackage(i.GetMetadata("id"), i.GetMetadata("version"))).ToList()
            };
            System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(assetsFile, Formatting.Indented));
            return System.IO.File.Exists(File);
        }
    }
}
