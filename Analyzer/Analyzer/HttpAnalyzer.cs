using System.Net;
namespace Analyzer
{
    public class HttpAnalyzer
    {
        private const int MaxRedirects = 10;
        private const int TimeoutSeconds = 5;

        private readonly HttpMessageHandler? _handler;

        // An injected handler is owned by the caller and is responsible for its own redirect configuration.
        public HttpAnalyzer(HttpMessageHandler? handler = null)
        {
            _handler = handler;
        }

        public async Task<HttpAnalysisResult> Analyze(Uri input, CancellationToken cancellationToken = default)
        {
            bool ownsHandler = _handler is null;
            HttpMessageHandler handler = _handler ?? new HttpClientHandler { AllowAutoRedirect = false };

            using HttpClient client = new HttpClient(handler, disposeHandler: ownsHandler);
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            int redirectCount = 0;

            try
            {
                Uri current = input;

                while (true)
                {
                    using HttpResponseMessage response = await client.GetAsync(current, cancellationToken);

                    if (!IsRedirectStatus(response.StatusCode))
                    {
                        Dictionary<string, string[]> headers = CollectHeaders(response);

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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Failure(input, redirectCount, "The analysis was cancelled.");
            }
            catch (OperationCanceledException)
            {
                // The caller did not cancel, so HttpClient.Timeout expired.
                return Failure(input, redirectCount, $"The request timed out after {TimeoutSeconds} seconds.");
            }
            catch (HttpRequestException ex)
            {
                return Failure(input, redirectCount, DescribeNetworkError(ex.HttpRequestError));
            }
        }

        private static string DescribeNetworkError(HttpRequestError error) => error switch
        {
            HttpRequestError.NameResolutionError => "The host could not be found. Check the address and your internet connection.",
            HttpRequestError.ConnectionError => "Could not connect to the server.",
            HttpRequestError.SecureConnectionError => "A secure (TLS) connection to the server could not be established.",
            _ => "A network error occurred while contacting the server."
        };

        // Response and content headers (e.g. Content-Type) are separate collections; names are case-insensitive.
        private static Dictionary<string, string[]> CollectHeaders(HttpResponseMessage response)
        {
            Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<KeyValuePair<string, IEnumerable<string>>> all = response.Content is null
                ? response.Headers
                : response.Headers.Concat(response.Content.Headers);

            foreach (var header in all)
            {
                string[] values = header.Value.ToArray();
                headers[header.Key] = headers.TryGetValue(header.Key, out string[]? existing)
                    ? existing.Concat(values).ToArray()
                    : values;
            }

            return headers;
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
 