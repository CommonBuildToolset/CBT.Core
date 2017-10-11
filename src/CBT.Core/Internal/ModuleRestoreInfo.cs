using System.Collections.Generic;

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

    internal class ModuleRestoreInfo
    {
        public string RestoreOutputAbsolutePath { get; set; }

        // Using custom RestorePackage item since NuGetVersion requires a custom serializer and reasoning about the formation of the nuget version can be tricky for all the potential ways versions are done.
        public IList<RestorePackage> PackageImportOrder { get; set; }

        // Type of projectStyle that nuget is restoring PackageReference, ProjectJson, Unknown 
        public string RestoreProjectStyle { get; set; }

        // Path of project.json if restoreprojectstyle is projectjson
        public string ProjectJsonPath { get; set; }
    }
}
