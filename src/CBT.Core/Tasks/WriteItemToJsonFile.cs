using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CBT.Core.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CBT.Core.Tasks
{
    public class WriteAssetsFlagJsonFile : Task
    {

        [Required]
        public ITaskItem[] Input { get; set; }

        [Required]
        public string File { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(File))
            {
                Log.LogError("File must be specifed and can not be a null/whitespace/empty string.");
                return false;
            }
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
                    .Select(i => new RestorePackage{Id = i.GetMetadata("id"), Version = i.GetMetadata("version") ?? "0.0.0"}).ToList()
            };
            System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(assetsFile, Formatting.Indented));
            return System.IO.File.Exists(File);
        }
    }
}
