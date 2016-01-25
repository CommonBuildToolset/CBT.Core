using CBT.Core.Internal;
using Microsoft.Build.Framework;

namespace CBT.Core.Tasks
{
    /// <summary>
    /// Represents a base class for CBT tasks.
    /// </summary>
    /// <remarks>
    /// This is not using the Microsoft.Build.Tasks.Task base class because that assembly has been renamed from MSBuild 12.0 to 14.0.
    /// So far, CBT core tasks don't really need that much functionality anyway so this small base class is used instead.
    /// </remarks>
    public abstract class TaskBase : ITask
    {
        /// <summary>
        /// Stores the name of the task.
        /// </summary>
        private readonly string _taskName;

        /// <summary>
        /// Initializes a new instance of the TaskBase class.
        /// </summary>
        protected TaskBase()
        {
            // Default task name is the full class name
            //
            _taskName = GetType().Name;

            Log = new TaskLogHelper(this);
        }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        public virtual string TaskName
        {
            get { return _taskName; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ITaskLogHelper"/> to use for logging.
        /// </summary>
        internal ITaskLogHelper Log { get; set; }

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <returns><code>true</code> if the task succeeded, otherwise <code>false</code>.</returns>
        public abstract bool Execute();
    }
}