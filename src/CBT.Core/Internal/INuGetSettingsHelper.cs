using NuGet.Configuration;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a settings helper for NuGet.
    /// </summary>
    internal interface INuGetSettingsHelper
    {
        /// <summary>
        /// Gets the <see cref="IMachineWideSettings"/>.
        /// </summary>
        IMachineWideSettings MachineWideSettings { get; }

        /// <summary>
        /// Gets the <see cref="ISettings"/>.
        /// </summary>
        ISettings Settings { get; }
    }
}