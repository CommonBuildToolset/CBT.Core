using Microsoft.Build.Framework;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetPackageReferenceProjectParser : NuGetPackageConfigParserBase
    {
        public NuGetPackageReferenceProjectParser(ISettings settings, CBTTaskLogHelper logger)
            : base(settings, logger)
        {
        }

        public override bool TryGetPackages(string packageConfigPath, ModuleRestoreInfo moduleRestoreInfo, out IEnumerable<PackageIdentityWithPath> packages)
        {
            packages = null;

            // This parser cannot do anything for packages.config or project.json
            //
            if (NuGetPackagesConfigParser.IsPackagesConfigFile(packageConfigPath) || ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath))
            {
                return false;
            }

            // This parser requires that the restore info file was created
            //
            if (moduleRestoreInfo == null)
            {
                Log.LogMessage("Missing expected assets file directory.  This is typically because the flag generated at $(CBTModuleNuGetAssetsFlagFile) does not exist or is empty.  Ensure the GenerateModuleAssetFlagFile target is running. It may also be because the CBTModules.proj does not import CBT build.props in some fashion.");
                return false;
            }

            if (!String.Equals("PackageReference", moduleRestoreInfo.RestoreProjectStyle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string lockFilePath = Path.Combine(moduleRestoreInfo.RestoreOutputAbsolutePath, LockFileFormat.AssetsFileName);

            if (!File.Exists(lockFilePath))
            {
                throw new FileNotFoundException($"Missing expected NuGet assets file '{lockFilePath}'.  If you are redefining BaseIntermediateOutputPath ensure it is unique per project. ");
            }

            HashSet<string> processedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            LockFile lockFile = LockFileUtilities.GetLockFile(lockFilePath, NullLogger.Instance);

            string globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(NuGetSettings);

            if (String.IsNullOrWhiteSpace(globalPackagesFolder))
            {
                throw new NuGetConfigurationException(@"Unable to determine the NuGet repository path.  This usually defaults to ""%UserProfile%\.nuget\packages"", ""%NUGET_PACKAGES%"", or the ""globalPackagesFolder"" in your NuGet.config.");
            }

            globalPackagesFolder = Path.GetFullPath(globalPackagesFolder);

            if (!Directory.Exists(globalPackagesFolder))
            {
                throw new DirectoryNotFoundException($"The NuGet repository '{globalPackagesFolder}' does not exist.  Ensure that NuGet restored packages to the location specified in your NuGet.config.");
            }

            Log.LogMessage(MessageImportance.Low, $"Using repository path: '{globalPackagesFolder}'");

            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(globalPackagesFolder);

            packages = new List<PackageIdentityWithPath>();

            foreach (RestorePackage package in moduleRestoreInfo.PackageImportOrder)
            {
                // In <PackageReference only one version of a nuGet package will be installed.  That version may not be the one specified in the <PackageReference item.  So we can not match the version specified in the CBTModules.proj with the version actually installed.  If we want to do any such matching it would simply need to result in a build error.
                IEnumerable<PackageDependency> dependencies = lockFile.Targets.First().Libraries.Where(lib => lib.Name.Equals(package.Id, StringComparison.OrdinalIgnoreCase)).Select(lib => lib.Dependencies).SelectMany(p => p.Select(i => i));

                foreach (PackageDependency dependency in dependencies)
                {
                    // In the <PackageReference scenario nuGet will only install one packageId.  If you have two packages that reference different package versions of a third package then it will choose the common highest version and if there is no common version it will error.  If you have two packages listed with two different versions it will choose the first entry and silently not install the other.
                    // If the package is already processed then skip.  If the package is explicitly added then skip to use that order.
                    if (!processedPackages.Contains(dependency.Id, StringComparer.OrdinalIgnoreCase) && !moduleRestoreInfo.PackageImportOrder.Any(pio => pio.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        LockFileLibrary installedPackage = lockFile.Libraries.First(lockPkg => lockPkg.Name.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase));

                        processedPackages.Add(dependency.Id);

                        AddPackage((List<PackageIdentityWithPath>)packages, installedPackage, versionFolderPathResolver);
                    }
                }

                if (!processedPackages.Contains(package.Id))
                {
                    LockFileLibrary installedPackage = lockFile.Libraries.First(lockPkg => lockPkg.Name.Equals(package.Id, StringComparison.OrdinalIgnoreCase));

                    processedPackages.Add(package.Id);

                    AddPackage((List<PackageIdentityWithPath>)packages, installedPackage, versionFolderPathResolver);
                }
            }

            return true;
        }

        private void AddPackage(List<PackageIdentityWithPath> packages, LockFileLibrary installedPackage, VersionFolderPathResolver versionFolderPathResolver)
        {
            string installPath = versionFolderPathResolver.GetInstallPath(installedPackage.Name, installedPackage.Version);

            if (!String.IsNullOrWhiteSpace(installPath))
            {
                packages.Add(new PackageIdentityWithPath(installedPackage.Name, installedPackage.Version, Path.GetFullPath(installPath)));
            }
            else
            {
                Log.LogWarning($"The package '{installedPackage.Name}' was not found in the repository.");
            }
        }
    }
}