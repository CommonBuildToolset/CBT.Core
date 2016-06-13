using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace CBT.Core.Internal
{
    /// <summary>
    /// A class that will download the latest version of NuGet from NuGet.org.
    /// </summary>
    internal static class DefaultNuGetDownloader
    {
        private static readonly Uri DefaultNuGetUrl = new Uri("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");

        public static bool Execute(string path, string arguments, Action<string> logInfo, Action<string> logError, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            bool success = true;

            Uri downloadUri = DefaultNuGetUrl;

            // Attempt to parse the arguments as a URL to NuGet.exe
            //
            if (!String.IsNullOrWhiteSpace(arguments))
            {
                Uri uri;

                if (!Uri.TryCreate(arguments, UriKind.Absolute, out uri) || !Path.GetFileName(uri.LocalPath).Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"The specified NuGet downloader arguments '{arguments}' are invalid.  The value must be a valid URL that points to NuGet.exe.");
                }

                downloadUri = uri;
            }

            logInfo($"Downloading NuGet from '{downloadUri}'");

            string filePath = Path.Combine(path, Path.GetFileName(downloadUri.LocalPath));

            Retry(() =>
            {
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadFileTaskAsync(downloadUri, filePath).Wait(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException)
                        {
                            e = ((AggregateException) e).Flatten().InnerExceptions.Last();
                        }

                        logInfo($"{e.Message}");

                        webClient.CancelAsync();

                        try
                        {
                            Retry(() =>
                            {
                                // Clean up any partially downloaded file
                                //
                                if (File.Exists(filePath))
                                {
                                    File.Delete(filePath);
                                }
                            }, TimeSpan.FromMilliseconds(200));
                        }
                        catch (Exception)
                        {
                            // Ignored
                        }

                        if (e is OperationCanceledException)
                        {
                            success = false;
                            return;
                        }

                        throw;
                    }
                }
            }, TimeSpan.FromSeconds(3));

            return success;
        }

        private static void Retry(Action action, TimeSpan retryInterval, int retryCount = 3)
        {
            List<Exception> exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    action();

                    return;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
