using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.Core
{
    public static class CoreUtility
    {
        public static bool IsFileOutOfDate(string path, params string[] paths)
        {
            return IsFileOutOfDate(path, paths.ToList());

        }

        public static bool IsFileOutOfDate(string path, ICollection<string> paths)
        {
            if (!File.Exists(path) || paths == null || paths.Count == 0)
            {
                return true;
            }

            DateTime lastWriteTime = File.GetLastWriteTimeUtc(path);



            return paths.FirstOrDefault(i => File.Exists(i) && File.GetLastWriteTimeUtc(i) < lastWriteTime) == null;
        }
    }
}