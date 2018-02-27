using Microsoft.Build.Framework;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CBT.Core.Internal
{
    internal static class ExtensionMethods
    {
        private static Lazy<SHA256> _hasherLazy = new Lazy<SHA256>(SHA256.Create, isThreadSafe: true);

        /// <summary>
        /// Gets a case-insensitive MD5 hash of the current string.
        /// </summary>
        public static string GetHash(this string input, string prefix = null)
        {
            if (prefix != null)
            {
                return $"{prefix}{Convert.ToBase64String(_hasherLazy.Value.ComputeHash(Encoding.UTF8.GetBytes(input.ToUpperInvariant())))}";
            }
            else
            {
                return Convert.ToBase64String(_hasherLazy.Value.ComputeHash(Encoding.UTF8.GetBytes(input.ToUpperInvariant())));
            }
        }
    }
}