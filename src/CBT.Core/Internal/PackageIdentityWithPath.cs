using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents the core identity of a package along with it's currently installed path.
    /// </summary>
    internal sealed class PackageIdentityWithPath : PackageIdentity
    {
        public PackageIdentityWithPath(string id, string version, string fullPath)
            : this(new PackageIdentity(id, new NuGetVersion(version)), fullPath)
        {
        }

        public PackageIdentityWithPath(string id, NuGetVersion version, string fullPath)
            : this(new PackageIdentity(id, version), fullPath)
        {
        }

        public PackageIdentityWithPath(string id, NuGetVersion version)
            : base(id, version)
        {
        }

        public PackageIdentityWithPath(PackageIdentity packageIdentity, string fullPath)
            : this(packageIdentity.Id, packageIdentity.Version)
        {
            FullPath = fullPath;
        }

        /// <summary>
        /// Gets the full path of the package.
        /// </summary>
        public string FullPath { get; }
    }
}