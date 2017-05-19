using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetPackageReferenceProjectParser : INuGetPackageConfigParser
    {
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, string assetsFileDirectory)
        {
            // This assumes that if it is a non packages.config or project.json being restored that it is a msbuild project using the new PackageReference.  
            if (string.IsNullOrWhiteSpace(assetsFileDirectory) || ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath) || packageConfigPath.EndsWith(NuGet.ProjectManagement.Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }
            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(packagesPath);
            
            string lockFilePath = Path.Combine(assetsFileDirectory, LockFileFormat.AssetsFileName);

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