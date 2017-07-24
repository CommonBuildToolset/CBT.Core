using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging.Core;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetPackageReferenceProjectParser : INuGetPackageConfigParser
    {
        CBTTaskLogHelper _log = null;
        public NuGetPackageReferenceProjectParser(CBTTaskLogHelper logger)
        {
            _log = logger;
        }
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, PackageRestoreData packageRestoreData)
        {
            // This assumes that if it is a non packages.config or project.json being restored that it is a msbuild project using the new PackageReference.  
            if (ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath) || packageConfigPath.EndsWith(NuGet.ProjectManagement.Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }
            if (string.IsNullOrWhiteSpace(packageRestoreData?.RestoreOutputAbsolutePath))
            {
                _log.LogWarning($"Missing expected assests file directory.  This is typically because the flag generated at $(CBTModuleNuGetAssetsFlagFile) does not exist or is empty.  Ensure the GenerateModuleAssetFlagFile target is running. It may also be because the CBTModules.proj does not import cbt build.props in some fashion.");
                yield break;
            }
            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(packagesPath);

            string lockFilePath = Path.Combine(packageRestoreData.RestoreOutputAbsolutePath, LockFileFormat.AssetsFileName);

            if (!File.Exists(lockFilePath))
            {
                _log.LogWarning($"Missing expected nuget assests file '{lockFilePath}'.  If you are redefining BaseIntermediateOutputPath ensure it is unique per project. ");
                yield break;
            }
            HashSet<string> processedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            LockFile lockFile = LockFileUtilities.GetLockFile(lockFilePath, NullLogger.Instance);

            foreach (var pkg in packageRestoreData.PackageImportOrder)
            {
                // In <PackageReference only one version of a nuGet package will be installed.  That version may not be the one specified in the <PackageReference item.  So we can not match the version specified in the cbtmodules project with the version actually installed.  If we want to do any such mathcing it would simply need to result in a build error.
                var dependencies = lockFile.Targets.First().Libraries
                    .Where(lib => lib.Name.Equals(pkg.Id, StringComparison.OrdinalIgnoreCase)).Select(lib => lib.Dependencies).SelectMany(p => p.Select(i => i));
                foreach (PackageDependency dependency in dependencies)
                {
                    // In the <PackageReference scenario nuGet will only install one packageId.  If you have two packages that reference different package versions of a third package then it will choose the common highest version and if there is no common version it will error.  If you have two packages listed with two different versions it will choose the first entry and silently not install the other.
                    // If the package is already processed then skip.  If the package is explicitly added then skip to use that order.
                    if (!processedPackages.Contains(dependency.Id, StringComparer.OrdinalIgnoreCase) && !packageRestoreData.PackageImportOrder.Any(pio => pio.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        var installedPackage = lockFile.Libraries
                            .First(lockPkg => lockPkg.Name.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase));
                        processedPackages.Add(dependency.Id);
                        yield return new PackageIdentityWithPath(installedPackage.Name, installedPackage.Version, versionFolderPathResolver.GetPackageDirectory(installedPackage.Name, installedPackage.Version), versionFolderPathResolver.GetInstallPath(installedPackage.Name, installedPackage.Version));
                    }
                }
                if (!processedPackages.Contains(pkg.Id))
                {
                    var installedPackage = lockFile.Libraries
                        .First(lockPkg => lockPkg.Name.Equals(pkg.Id, StringComparison.OrdinalIgnoreCase));
                    processedPackages.Add(pkg.Id);
                    yield return new PackageIdentityWithPath(installedPackage.Name, installedPackage.Version, versionFolderPathResolver.GetPackageDirectory(installedPackage.Name, installedPackage.Version), versionFolderPathResolver.GetInstallPath(installedPackage.Name, installedPackage.Version));
                }
            }
        }
    }
}