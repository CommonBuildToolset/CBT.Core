using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CBT.Core.Internal
{
    /// <summary>
    /// A class that will download the latest version of NuGet from NuGet.org.
    /// </summary>
    internal static class DefaultNuGetDownloader
    {
        private static readonly Uri NuGetUrl = new Uri("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");

        public static bool Execute(string path, IBuildEngine buildEngine, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (buildEngine == null)
            {
                throw new ArgumentNullException("buildEngine");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            LogMessage(buildEngine, "Downloading NuGet from '{0}'", NuGetUrl);

            string filePath = Path.Combine(path, Path.GetFileName(NuGetUrl.LocalPath));

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFileTaskAsync(NuGetUrl, filePath).Wait(cancellationToken);
                    
                    return true;
                }
                catch (Exception)
                {
                    
                    webClient.CancelAsync();

                    if (File.Exists(filePath))
                    {
                        // Delete any file that was partially downloaded when canceling
                        //
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                File.Delete(filePath);
                                break;
                            }
                            catch (Exception)
                            {
                                // Ignored because in some cases the file is in use still
                                //
                            }

                            // ReSharper disable once MethodSupportsCancellation
                            Thread.Sleep(TimeSpan.FromMilliseconds(200));
                        }
                    }
                }
            }

            return false;
        }

        private static void LogMessage(IBuildEngine buildEngine, string format, params object[] args)
        {
            buildEngine.LogMessageEvent(new BuildMessageEventArgs(format, null, "DownloadNuGet", MessageImportance.Normal, DateTime.UtcNow, args));
        }
    }
}
