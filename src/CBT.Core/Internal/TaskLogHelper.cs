using CBT.Core.Tasks;
using Microsoft.Build.Framework;
using System;

namespace CBT.Core.Internal
{
    /// <summary>
    /// Represents a logging helper for tasks.
    /// </summary>
    internal sealed class TaskLogHelper : ITaskLogHelper
    {
        /// <summary>
        /// Stores the <see cref="TaskBase"/> instance of the task.
        /// </summary>
        private readonly TaskBase _task;

        /// <summary>
        /// Initializes a new instance of the TaskLogHelper class.
        /// </summary>
        /// <param name="task">A <see cref="TaskBase"/> containing an instance of this class.</param>
        /// <exception cref="ArgumentNullException"><paramref name="task"/> is <code>null</code>.</exception>
        public TaskLogHelper(TaskBase task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            _task = task;
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Error(string message)
        {
            Error(message, null);
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Error(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(null, null, null, 0, 0, 0, 0, message, null, _task.TaskName, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Message(string message, params object[] args)
        {
            Message(MessageImportance.Normal, message, args);
        }

        /// <summary>
        /// Logs a message using the specified string.
        /// </summary>
        /// <param name="importance">The importance level of the message.</param>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Message(MessageImportance importance, string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, _task.TaskName, importance, DateTime.UtcNow, args));
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Warning(string message)
        {
            Warning(message, null);
        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="args">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <code>null</code>.</exception>
        public void Warning(string message, params object[] args)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException("message");
            }

            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(null, null, null, 0, 0, 0, 0, message, null, _task.TaskName, DateTime.UtcNow, args));
        }
    }
}