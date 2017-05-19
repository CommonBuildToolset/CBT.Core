using System.Collections.Generic;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents an interface of a class that can parse a NuGet package configuration file (packages.config, project.json, etc).
    /// </summary>
    internal interface INuGetPackageConfigParser
    {
        /// <summary>
        /// Parses the specified package configuration.
        /// </summary>
        /// <param name="packagesPath">The path to the packages folder.</param>
        /// <param name="packageConfigPath">The path to the NuGet package configuration file.</param>
        /// <returns>An <see cref="IEnumerable{PackageIdentity}"/> of the packages specified in the configuration.</returns>
        IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, string assetsFileDirectory);
    }
}