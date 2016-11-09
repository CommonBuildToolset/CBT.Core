using NuGet.Common;
using NuGet.Logging;
using NuGet.Packaging;
using NuGet.ProjectModel;
using System.Collections.Generic;
using System.IO;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetProjectJsonParser : INuGetPackageConfigParser
    {
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath)
        {
            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(packagesPath);

            if (!ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath))
            {
                yield break;
            }

            string lockFilePath = ProjectJsonPathUtilities.GetLockFilePath(packageConfigPath);

            if (!File.Exists(lockFilePath))
            {
                yield break;
            }

            LockFile lockFile = LockFileUtilities.GetLockFile(lockFilePath, NullLogger.Instance);

            foreach (LockFileLibrary library in lockFile.Libraries)
            {
                yield return new PackageIdentityWithPath(library.Name, library.Version, versionFolderPathResolver.GetPackageDirectory(library.Name, library.Version), versionFolderPathResolver.GetInstallPath(library.Name, library.Version));
            }
        }
    }
}