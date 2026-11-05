using System.Net;
namespace Analyzer
{
    public class HttpAnalyzer
    {
        private const int MaxRedirects = 10;

        private readonly HttpMessageHandler? _handler;

        // An injected handler is owned by the caller and is responsible for its own redirect configuration.
        public HttpAnalyzer(HttpMessageHandler? handler = null)
        {
            _handler = handler;
        }

        public async Task<HttpAnalysisResult> Analyze(Uri input)
        {
            bool ownsHandler = _handler is null;
            HttpMessageHandler handler = _handler ?? new HttpClientHandler { AllowAutoRedirect = false };

            using HttpClient client = new HttpClient(handler, disposeHandler: ownsHandler);
            client.Timeout = TimeSpan.FromSeconds(5);

            int redirectCount = 0;

            try
            {
                Uri current = input;

                while (true)
                {
                    using HttpResponseMessage response = await client.GetAsync(current);

                    if (!IsRedirectStatus(response.StatusCode))
                    {
                        Dictionary<string, string[]> headers = new Dictionary<string, string[]>();

                        foreach (var header in response.Headers)
                        {
                            headers.Add(header.Key, header.Value.ToArray());
                        }

                        return new HttpAnalysisResult(true, input, current, response.StatusCode, headers, redirectCount > 0, redirectCount, null);
                    }

                    if (redirectCount >= MaxRedirects)
                    {
                        return Failure(input, redirectCount, $"Too many redirects (more than {MaxRedirects}).");
                    }

                    Uri? location = response.Headers.Location;
                    if (location is null)
                    {
                        return Failure(input, redirectCount, "The server sent a redirect without a valid Location header.");
                    }

                    if (!Uri.TryCreate(current, location, out Uri? next)
                        || (next.Scheme != Uri.UriSchemeHttp && next.Scheme != Uri.UriSchemeHttps))
                    {
                        return Failure(input, redirectCount, "The server redirected to an unsupported or invalid address.");
                    }

                    current = next;
                    redirectCount++;
                }
            }
            catch(HttpRequestException ex)
            {
                return new HttpAnalysisResult(false, input, null, null, new Dictionary<string, string[]>(), redirectCount > 0, redirectCount, ex.Message);
            }
            catch(OperationCanceledException ex2)
            {
                return new HttpAnalysisResult(false, input, null, null, new Dictionary<string, string[]>(), redirectCount > 0, redirectCount, $"{ex2.Message}. The operation took more than 5 seconds");
            }
        }

        private static bool IsRedirectStatus(HttpStatusCode statusCode) =>
            statusCode is HttpStatusCode.MovedPermanently
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect;

        private static HttpAnalysisResult Failure(Uri input, int redirectCount, string message) =>
            new HttpAnalysisResult(false, input, null, null, new Dictionary<string, string[]>(), redirectCount > 0, redirectCount, message);
    }
}
 