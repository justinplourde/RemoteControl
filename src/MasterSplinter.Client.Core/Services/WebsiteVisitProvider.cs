using System;
using System.Diagnostics;
using System.Net.Http;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class WebsiteVisitProvider : IWebsiteVisitProvider
    {
        public void Visit(string url, bool hidden)
        {
            Uri uri = NormalizeUrl(url);
            if (hidden)
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.75.14 (KHTML, like Gecko) Version/7.0.3 Safari/7046A194A");
                    using (client.GetAsync(uri).GetAwaiter().GetResult())
                    {
                    }
                }

                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }

        private static Uri NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            if (url.Contains("://") &&
                !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", nameof(url));
            }

            string normalized = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? url
                : "http://" + url;

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", nameof(url));
            }

            return uri;
        }
    }
}
