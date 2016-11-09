using NuGet.Common;
using NuGet.Configuration;
using System;
using System.Collections.Generic;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a class that can read NuGet settings.
    /// </summary>
    internal sealed class NuGetSettingsHelper : INuGetSettingsHelper
    {
        private readonly Lazy<IMachineWideSettings> _machineWideSettings = new Lazy<IMachineWideSettings>(() => new MyMachineWideSettings());
        private readonly Lazy<ISettings> _settings;

        public NuGetSettingsHelper(string rootPath)
        {
            _settings = new Lazy<ISettings>(() => NuGet.Configuration.Settings.LoadDefaultSettings(rootPath, null, MachineWideSettings));
        }

        /// <summary>
        /// Gets the current machine wide settings.
        /// </summary>
        public IMachineWideSettings MachineWideSettings => _machineWideSettings.Value;

        /// <summary>
        /// Gets the current local settings.
        /// </summary>
        public ISettings Settings => _settings.Value;

        /// <summary>
        /// Represents a class that reads machine wide settings.
        /// </summary>
        private class MyMachineWideSettings : IMachineWideSettings
        {
            private readonly Lazy<IEnumerable<Settings>> _machineWideSettings = new Lazy<IEnumerable<Settings>>(() => NuGet.Configuration.Settings.LoadMachineWideSettings(NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory)));

            public IEnumerable<Settings> Settings => _machineWideSettings.Value;
        }
    }
}