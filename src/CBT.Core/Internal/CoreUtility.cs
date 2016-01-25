using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.Core.Internal
{
    /// <summary>
    /// A class containing utility methods.
    /// </summary>
    internal static class CoreUtility
    {
        /// <summary>
        /// Determines if a file is out-of-date in relation to the specified paths.
        /// </summary>
        /// <param name="path">The file to check if it is out-of-date.</param>
        /// <param name="paths">The list of files to check against.</param>
        /// <returns><code>true</code> if the file does not exist or it is older than any of the other files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <code>null</code>.</exception>
        public static bool IsFileOutOfDate(string path, params string[] paths)
        {
            return IsFileOutOfDate(path, paths.ToList());
        }

        /// <summary>
        /// Determines if a file is out-of-date in relation to the specified paths.
        /// </summary>
        /// <param name="path">The file to check if it is out-of-date.</param>
        /// <param name="paths">The list of files to check against.</param>
        /// <returns><code>true</code> if the file does not exist or it is older than any of the other files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <code>null</code>.</exception>
        public static bool IsFileOutOfDate(string path, ICollection<string> paths)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }
            
            if (!File.Exists(path) || paths == null || paths.Count == 0)
            {
                return true;
            }

            long lastWriteTime = File.GetLastWriteTimeUtc(path).Ticks;

            return paths.Where(File.Exists).Any(item => File.GetLastWriteTimeUtc(item).Ticks > lastWriteTime);
        }
    }
}