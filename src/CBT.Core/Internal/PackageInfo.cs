using System;
using System.IO;

namespace CBT.Core.Internal
{
    internal sealed class PackageInfo
    {
        private Version _version;

        public PackageInfo(string id, string version, string path)
        {
            Id = id;
            VersionString = version;

            Version.TryParse(version, out _version);

            Path = path;
        }

        public bool Exists
        {
            get { return File.Exists(Path); }
        }

        public string Id { get; private set; }

        public string Path { get; private set; }

        public Version Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public string VersionString { get; private set; }
    }
}