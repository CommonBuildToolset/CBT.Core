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
        private const string NuGetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";

        public static async Task<bool> ExecuteAsync(string path, IBuildEngine buildEngine, CancellationToken cancellationToken)
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

            string filePath = Path.Combine(path, Path.GetFileName(NuGetUrl));

            try
            {
                Task downloadTask = Task.Run(() =>
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(new Uri(NuGetUrl), filePath);
                    }
                }, cancellationToken);

                Task timeoutTask = Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);

                Task completedTask = await Task.WhenAny(downloadTask, timeoutTask).ConfigureAwait(continueOnCapturedContext: false);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException(String.Format("Timed out downloading NuGet from '{0}'", NuGetUrl));
                }

                return true;
            }
            catch (Exception)
            {
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

                        Thread.Sleep(TimeSpan.FromMilliseconds(200));
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
