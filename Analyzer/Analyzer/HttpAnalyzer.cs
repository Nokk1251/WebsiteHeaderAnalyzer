using System.Net;
namespace Analyzer
{
    public class HttpAnalyzer
    {
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

            try
            {
                // Redirects are not followed yet; any received response is reported as-is.
                using HttpResponseMessage response = await client.GetAsync(input);
                Dictionary<string, string[]> headers = new Dictionary<string, string[]>();

                foreach (var header in response.Headers)
                {
                    headers.Add(header.Key, header.Value.ToArray());
                }

                return new HttpAnalysisResult(true, input, input, response.StatusCode, headers, false, 0, null);
            }
            catch(HttpRequestException ex)
            {
                return new HttpAnalysisResult(false, input, null, null, new Dictionary<string, string[]>(), false, 0, ex.Message);
            }
            catch(OperationCanceledException ex2)
            {
                return new HttpAnalysisResult(false, input, null, null, new Dictionary<string, string[]>(), false, 0, $"{ex2.Message}. The operation took more than 5 seconds");
            }



        }
    }
}
 