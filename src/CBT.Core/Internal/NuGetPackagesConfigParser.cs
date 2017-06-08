using NuGet.Packaging;
using NuGet.Packaging.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet packages.config file.
    /// </summary>
    internal sealed class NuGetPackagesConfigParser : INuGetPackageConfigParser
    {
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, PackageRestoreData packageRestoreData)
        {
            PackagePathResolver packagePathResolver = new PackagePathResolver(packagesPath);

            if (!packageConfigPath.EndsWith(NuGet.ProjectManagement.Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            XDocument document = XDocument.Load(packageConfigPath);

            PackagesConfigReader packagesConfigReader = new PackagesConfigReader(document);

            foreach (PackageIdentity item in packagesConfigReader.GetPackages(allowDuplicatePackageIds: true).Select(i => i.PackageIdentity))
            {
                yield return new PackageIdentityWithPath(item, packagePathResolver.GetPackageDirectoryName(item), packagePathResolver.GetInstallPath(item));
            }
        }
    }
}