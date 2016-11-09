using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents the core identity of a package along with it's currently installed path.
    /// </summary>
    internal sealed class PackageIdentityWithPath : PackageIdentity
    {
        public PackageIdentityWithPath(string id, string version, string path, string fullPath)
            : this(new PackageIdentity(id, new NuGetVersion(version)), path, fullPath)
        {
        }

        public PackageIdentityWithPath(string id, NuGetVersion version, string path, string fullPath)
            : this(new PackageIdentity(id, version), path, fullPath)
        {
        }

        public PackageIdentityWithPath(string id, NuGetVersion version)
            : base(id, version)
        {
        }

        public PackageIdentityWithPath(PackageIdentity packageIdentity, string path, string fullPath)
            : this(packageIdentity.Id, packageIdentity.Version)
        {
            Path = path;
            FullPath = fullPath;
        }

        /// <summary>
        /// Gets the full path of the package.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets or sets the relative path of the package in the packages folder.
        /// </summary>
        public string Path { get; }
    }
}