using NuGet.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.Core.UnitTests
{
    internal sealed class MockSettings : Dictionary<string, IDictionary<string, string>>, ISettings
    {
        public MockSettings()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public event EventHandler SettingsChanged;

        public string FileName => throw new NotImplementedException();

        public IEnumerable<ISettings> Priority => throw new NotImplementedException();

        public string Root => throw new NotImplementedException();

        public bool DeleteSection(string section) => Remove(section);

        public bool DeleteValue(string section, string key) => TryGetValue(section, out IDictionary<string, string> values) && values != null && values.Remove(key);

        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string subSection)
        {
            throw new NotImplementedException();
        }

        public IList<SettingValue> GetSettingValues(string section, bool isPath = false)
        {
            if (TryGetValue(section, out IDictionary<string, string> values) && values != null)
            {
                return values.Select(i => new SettingValue(i.Key, i.Value, false)).ToList();
            }
            return new List<SettingValue>();
        }

        public string GetValue(string section, string key, bool isPath = false)
        {
            if (TryGetValue(section, out IDictionary<string, string> values) && values != null && values.TryGetValue(key, out string value))
            {
                return isPath ? Path.GetFullPath(value) : value;
            }

            return null;
        }

        public void SetNestedValues(string section, string subSection, IList<KeyValuePair<string, string>> values)
        {
            throw new NotImplementedException();
        }

        public void SetValue(string section, string key, string value)
        {
            if (!ContainsKey(section) || this[section] == null)
            {
                this[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            this[section][key] = value;

            OnSettingsChanged();
        }

        public void SetValues(string section, IReadOnlyList<SettingValue> values)
        {
            if (!ContainsKey(section) || this[section] == null)
            {
                this[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (SettingValue value in values)
            {
                this[section][value.Key] = value.Value;
            }

            OnSettingsChanged();
        }

        public void UpdateSections(string section, IReadOnlyList<SettingValue> values)
        {
            SetValues(section, values);
        }

        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}