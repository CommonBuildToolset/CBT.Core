using System.Collections.Generic;
using NuGet.Packaging.Core;

namespace CBT.Core.Internal
{
    internal class PackageRestoreData
    {
        public string RestoreOutputAbsolutePath { get; set; }
        public IList<PackageIdentity> PackageImportOrder { get; set; }
    }
}
