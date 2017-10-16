using Microsoft.Build.Framework;
using NuGet.Configuration;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet packages.config file.
    /// </summary>
    internal sealed class NuGetPackagesConfigParser : NuGetPackageConfigParserBase
    {
        /// <summary>
        /// The name of this package configuration file which is packages.config.
        /// </summary>
        public const string PackageConfigFilename = "packages.config";

        public NuGetPackagesConfigParser(ISettings settings, CBTTaskLogHelper log)
            : base(settings, log)
        {
        }

        public static bool IsPackagesConfigFile(string packageConfigPath)
        {
            return packageConfigPath.EndsWith(PackageConfigFilename, StringComparison.OrdinalIgnoreCase);
        }

        public override bool TryGetPackages(string packageConfigPath, ModuleRestoreInfo moduleRestoreInfo, out IEnumerable<PackageIdentityWithPath> packages)
        {
            packages = null;

            if (!IsPackagesConfigFile(packageConfigPath))
            {
                return false;
            }

            string repositoryPath = SettingsUtility.GetRepositoryPath(NuGetSettings);

            if (String.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new NuGetConfigurationException("Unable to determine the NuGet repository path.  Ensure that you are you specifying a path in your NuGet.config (https://docs.microsoft.com/en-us/nuget/schema/nuget-config-file#config-section).");
            }

            Log.LogMessage(MessageImportance.Low, $"Using repository path: '{repositoryPath}'");

            PackagePathResolver packagePathResolver = new PackagePathResolver(repositoryPath);

            XDocument document = XDocument.Load(packageConfigPath);

            PackagesConfigReader packagesConfigReader = new PackagesConfigReader(document);

            packages = packagesConfigReader.GetPackages(allowDuplicatePackageIds: true).Select(i =>
            {
                string installPath = packagePathResolver.GetInstallPath(i.PackageIdentity);

                if (!String.IsNullOrWhiteSpace(installPath))
                {
                    installPath = Path.GetFullPath(installPath);
                }
                else
                {
                    Log.LogWarning($"The package '{i.PackageIdentity.Id}' was not found in the repository.");
                }

                return new PackageIdentityWithPath(i.PackageIdentity, installPath);
            }).Where(i => !String.IsNullOrWhiteSpace(i.FullPath));

            return true;
        }
    }
}