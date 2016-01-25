using CBT.Core.Internal;
using Microsoft.Build.Framework;

namespace CBT.Core.Tasks
{
    public abstract class TaskBase : ITask
    {
        private readonly string _taskName;

        protected TaskBase()
        {
            _taskName = GetType().Name;

            Log = new TaskLogHelper(this);
        }

        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public virtual string TaskName
        {
            get { return _taskName; }
        }

        internal ITaskLogHeler Log { get; set; }

        public abstract bool Execute();
    }
}