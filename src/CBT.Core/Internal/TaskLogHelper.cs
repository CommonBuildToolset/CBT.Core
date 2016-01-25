using CBT.Core.Tasks;
using Microsoft.Build.Framework;
using System;

namespace CBT.Core.Internal
{
    internal sealed class TaskLogHelper : ITaskLogHeler
    {
        private readonly TaskBase _task;

        public TaskLogHelper(TaskBase task)
        {
            _task = task;
        }

        public void Error(string message)
        {
            Error(message, null);
        }

        public void Error(string message, params object[] args)
        {
            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(null, null, null, 0, 0, 0, 0, message, null, _task.TaskName, DateTime.UtcNow, args));
        }

        public void Message(string message, params object[] args)
        {
            Message(MessageImportance.Normal, message, args);
        }

        public void Message(MessageImportance importance, string message, params object[] args)
        {
            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, _task.TaskName, importance, DateTime.UtcNow, args));
        }

        public void Warning(string message)
        {
            Warning(message, null);
        }

        public void Warning(string message, params object[] args)
        {
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(null, null, null, 0, 0, 0, 0, message, null, _task.TaskName, DateTime.UtcNow, args));
        }
    }
}