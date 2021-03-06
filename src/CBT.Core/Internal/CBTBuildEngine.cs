﻿using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace CBT.Core.Internal
{
    /// <summary>
    /// An internally used implementation of <see cref="IBuildEngine" /> that allows CBT to log events to MSBuild even though it isn't running as a task.
    /// </summary>
    internal sealed class CBTBuildEngine : IBuildEngine
    {
        private readonly LogBuildEventHandler _logBuildEventHandler;

        /// <summary>
        /// Initializes a new instance of the CBTBuildEngine class.
        /// </summary>
        public CBTBuildEngine()
        {
            ContinueOnError = false;
            LineNumberOfTaskNode = 0;
            ProjectFileOfTaskNode = String.Empty;
            ColumnNumberOfTaskNode = 0;

            // Get the current build manager and use reflection to get the LoggingService and LogBuildEvent() method
            //
            BuildManager buildManager = BuildManager.DefaultBuildManager;

            PropertyInfo loggingServiceProperty = buildManager?.GetType().GetProperty("Microsoft.Build.BackEnd.IBuildComponentHost.LoggingService", BindingFlags.Instance | BindingFlags.NonPublic);

            if (loggingServiceProperty != null)
            {
                try
                {
                    object loggingService = null;

                    try
                    {
                        loggingService = loggingServiceProperty.GetMethod.Invoke(buildManager, null);
                    }
                    catch (TargetInvocationException e) when(e.InnerException is NullReferenceException)
                    {
                        // When a build is not taking place, there is no logging service.  The LoggingService property attempts to cast which throws a NullReferenceException so this is ignored.
                    }
                
                    MethodInfo logBuildEventMethod = loggingService?.GetType().GetMethod("LogBuildEvent", new[] {typeof (BuildEventArgs)});

                    if (logBuildEventMethod != null)
                    {
                        _logBuildEventHandler = (LogBuildEventHandler) logBuildEventMethod.CreateDelegate(typeof (LogBuildEventHandler), loggingService);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }
        }

        /// <summary>
        /// A delegate for firing a build event.
        /// </summary>
        /// <param name="args"></param>
        private delegate void LogBuildEventHandler(BuildEventArgs args);

        /// <summary>
        /// Retrieves the line number of the task node within the project file that called it.
        /// </summary>
        public int ColumnNumberOfTaskNode { get; }

        /// <summary>
        /// Returns true if the ContinueOnError flag was set to true for this particular task
        /// in the project file.
        /// </summary>
        public bool ContinueOnError { get; }

        /// <summary>
        /// Retrieves the line number of the task node within the project file that called it.
        /// </summary>
        public int LineNumberOfTaskNode { get; }

        /// <summary>
        /// Returns the full path to the project file that contained the call to this task.
        /// </summary>
        public string ProjectFileOfTaskNode { get; }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allows tasks to raise custom events to all registered loggers.
        /// The build engine may perform some filtering or
        /// pre-processing on the events, before dispatching them.
        /// </summary>
        /// <param name="e">Details of event to raise.</param>
        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        /// <summary>
        /// Allows tasks to raise error events to all registered loggers.
        /// The build engine may perform some filtering or
        /// pre-processing on the events, before dispatching them.
        /// </summary>
        /// <param name="e">Details of event to raise.</param>
        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        /// <summary>
        /// Allows tasks to raise message events to all registered loggers.
        /// The build engine may perform some filtering or
        /// pre-processing on the events, before dispatching them.
        /// </summary>
        /// <param name="e">Details of event to raise.</param>
        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            OnLogBuildEvent(e);
        }

        /// <summary>
        /// Allows tasks to raise warning events to all registered loggers.
        /// The build engine may perform some filtering or
        /// pre-processing on the events, before dispatching them.
        /// </summary>
        /// <param name="e">Details of event to raise.</param>
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
                    // Send errors to StdErr
                    //
                    Console.Error.WriteLine(buildEventArgs.Message);
                }
                else
                {
                    BuildMessageEventArgs buildMessageEventArgs = buildEventArgs as BuildMessageEventArgs;

                    // Only send high importance messages to the console
                    //
                    if (buildMessageEventArgs?.Importance == MessageImportance.High)
                    {
                        Console.Out.WriteLine(buildMessageEventArgs.Message);
                    }
                    else
                    {
                        Trace.TraceInformation(buildEventArgs.Message);
                    }
                }
            }
            else
            {
                if (buildEventArgs.BuildEventContext == null)
                {
                    // This property is required to be set
                    //
                    buildEventArgs.BuildEventContext = BuildEventContext.Invalid;
                }

                _logBuildEventHandler(buildEventArgs);
            }
        }
    }
}