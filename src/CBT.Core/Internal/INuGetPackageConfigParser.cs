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
        /// /// <param name="packageConfigPath">The path to the NuGet package configuration file.</param>
        /// <param name="moduleRestoreInfo"></param>
        /// <param name="packages">An <see cref="IEnumerable{PackageIdentity}"/> of the packages specified in the configuration.</param>
        /// <returns><code>true</code> if the parser was able to successfully parse the packages, otherwise <code>false</code>.</returns>
        bool TryGetPackages(string packageConfigPath, ModuleRestoreInfo moduleRestoreInfo, out IEnumerable<PackageIdentityWithPath> packages);
    }
}