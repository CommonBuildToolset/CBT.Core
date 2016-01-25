using Microsoft.Build.Framework;

namespace CBT.Core.Internal
{
    internal interface ITaskLogHeler
    {
        void Error(string message);

        void Error(string message, params object[] args);

        void Message(string message, params object[] args);

        void Message(MessageImportance importance, string message, params object[] args);

        void Warning(string message);

        void Warning(string message, params object[] args);
    }
}