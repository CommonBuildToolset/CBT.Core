namespace CBT.Core.Internal
{
    // TODO: Parse Semantic Version
    internal sealed class PackageInfo
    {
        public PackageInfo(string id, string version, string path)
        {
            Id = id;
            VersionString = version;
            Path = path;
        }

        public string Id { get; private set; }

        public string Path { get; private set; }

        public string VersionString { get; private set; }
    }
}