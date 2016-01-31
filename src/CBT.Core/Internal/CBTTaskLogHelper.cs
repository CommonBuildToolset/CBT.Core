using System;
using Microsoft.Build.Framework;

namespace CBT.Core.Internal
{
    internal sealed class CBTTaskLogHelper
    {
        private readonly ITask _task;
        private readonly string _taskName;

        public CBTTaskLogHelper(ITask task)
            : this(task, task.GetType().Name)
        {
        }

        public CBTTaskLogHelper(ITask task, string taskName)
        {
            if(task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (String.IsNullOrWhiteSpace(taskName))
            {
                throw new ArgumentNullException("task");
            }

            _task = task;
            _taskName = taskName;
        }

        /// <summary>
        /// Logs a critical message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogCritical(string message)
        {
            LogCritical(message, null);
        }

        /// <summary>
        /// Logs a critical message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogCritical(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogMessageEvent(new CriticalBuildMessageEventArgs(null, null, null, 0, 0, 0, 0, message, null, null, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogError(string message)
        {
            LogError(message, null);
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogError(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(null, null, null, 0, 0, 0, 0, message, null, _taskName, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogMessage(string message, params object[] args)
        {
            LogMessage(MessageImportance.Normal, message, args);
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="importance">The importance level of the message.</param>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogMessage(MessageImportance importance, string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, _taskName, importance, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogWarning(string message)
        {
            LogWarning(message, null);
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void LogWarning(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(null, null, null, 0, 0, 0, 0, message, null, _taskName, DateTime.UtcNow, args));
        }
    }
}
