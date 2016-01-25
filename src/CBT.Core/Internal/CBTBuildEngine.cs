using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace CBT.Core.Internal
{
    internal delegate void LogBuildEventHandler(BuildEventArgs args);

    /// <summary>
    /// An internally used implementation of <see cref="IBuildEngine" /> that allows CBT to log events to MSBuild even though it isn't running as a task.
    /// </summary>
    internal sealed class CBTBuildEngine : IBuildEngine
    {
        private readonly LogBuildEventHandler _logBuildEventHandler;

        public CBTBuildEngine()
        {
            BuildManager buildManager = BuildManager.DefaultBuildManager;

            PropertyInfo loggingServiceProperty = buildManager.GetType().GetProperty("Microsoft.Build.BackEnd.IBuildComponentHost.LoggingService", BindingFlags.Instance | BindingFlags.NonPublic);

            if (loggingServiceProperty != null)
            {
                try
                {
                    object loggingService = loggingServiceProperty.GetMethod.Invoke(buildManager, null);

                    if (loggingService != null)
                    {
                        MethodInfo logBuildEventMethod = loggingService.GetType().GetMethod("LogBuildEvent", new[] {typeof (BuildEventArgs)});

                        if (logBuildEventMethod != null)
                        {
                            _logBuildEventHandler = (LogBuildEventHandler) logBuildEventMethod.CreateDelegate(typeof (LogBuildEventHandler), loggingService);
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }
        }

        public int ColumnNumberOfTaskNode { get; private set; }

        public bool ContinueOnError { get; private set; }

        public int LineNumberOfTaskNode { get; private set; }

        public string ProjectFileOfTaskNode { get; private set; }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        private void OnLogBuildEvent(BuildEventArgs buildEventArgs)
        {
            if (_logBuildEventHandler == null)
            {
                if (buildEventArgs is BuildErrorEventArgs)
                {
                    Console.Error.WriteLine(buildEventArgs.Message);
                }
                else
                {
                    Console.WriteLine(buildEventArgs.Message);
                }
            }
            else
            {
                if (buildEventArgs.BuildEventContext == null)
                {
                    buildEventArgs.BuildEventContext = BuildEventContext.Invalid;
                }

                _logBuildEventHandler(buildEventArgs);
            }
        }
    }
}