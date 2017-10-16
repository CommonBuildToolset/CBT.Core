using System;
using Microsoft.Build.Framework;

namespace CBT.Core.UnitTests
{
    internal sealed class MockTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            throw new NotSupportedException();
        }
    }
}