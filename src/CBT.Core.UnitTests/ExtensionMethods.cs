using System;
using Microsoft.Build.Construction;
using Shouldly;

namespace CBT.Core.UnitTests
{
    public static class ExtensionMethods
    {
        public static void ShouldBeGreaterThan(this ElementLocation actual, ElementLocation expected)
        {
            actual.ShouldBeGreaterThan(expected, () => null);
        }

        public static void ShouldBeGreaterThan(this ElementLocation actual, ElementLocation expected, string customMessage)
        {
            actual.ShouldBeGreaterThan(expected, () => customMessage);
        }

        public static void ShouldBeGreaterThan(this ElementLocation actual, ElementLocation expected, Func<string> customMessage)
        {
            actual.Line.ShouldBeGreaterThan(expected.Line, customMessage);
        }
    }
}