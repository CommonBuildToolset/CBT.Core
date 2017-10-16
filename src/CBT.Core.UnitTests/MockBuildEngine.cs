using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace CBT.Core.UnitTests
{
    internal sealed class MockBuildEngine : IBuildEngine
    {
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly IList<BuildEventArgs> _loggedBuildEvents = new List<BuildEventArgs>();

        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => String.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotSupportedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e) => _loggedBuildEvents.Add(e);

        public void LogErrorEvent(BuildErrorEventArgs e) => _loggedBuildEvents.Add(e);

        public void LogMessageEvent(BuildMessageEventArgs e) => _loggedBuildEvents.Add(e);

        public void LogWarningEvent(BuildWarningEventArgs e) => _loggedBuildEvents.Add(e);
    }
}