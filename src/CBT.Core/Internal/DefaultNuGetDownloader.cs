using System;
using System.IO;
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
                throw new ArgumentNullException("path");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            Uri downloadUri = DefaultNuGetUrl;

            // Attempt to parse the arguments as a URL to NuGet.exe
            //
            if (!String.IsNullOrWhiteSpace(arguments))
            {
                Uri uri;

                if (!Uri.TryCreate(arguments, UriKind.Absolute, out uri) || !Path.GetFileName(uri.LocalPath).Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(String.Format("The specified NuGet downloader arguments '{0}' are invalid.  The value must be a valid URL that points to NuGet.exe.", arguments));
                }

                downloadUri = uri;
            }

            logInfo(String.Format("Downloading NuGet from '{0}'", downloadUri));
            
            string filePath = Path.Combine(path, Path.GetFileName(downloadUri.LocalPath));

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFileTaskAsync(downloadUri, filePath).Wait(cancellationToken);
                    
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
    }
}
