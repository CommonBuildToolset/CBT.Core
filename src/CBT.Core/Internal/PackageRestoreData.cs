using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CBT.Core.Internal
{
    internal class RestorePackage
    {
        public RestorePackage(string id, string version)
        {
            Id = id;
            Version = string.IsNullOrWhiteSpace(version)? "0.0.0" : version;
        }
        public string Id { get; }
        public string Version { get; }
    }

    internal class PackageRestoreData
    {
        public string RestoreOutputAbsolutePath { get; set; }

        // Using custom RestorePackage item since NuGetVersion requires a custom serializer and reasoning about the formation of the nuget version can be tricky for all the potential ways versions are done.
        public IList<RestorePackage> PackageImportOrder { get; set; }
    }
}
