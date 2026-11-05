
using System.Net;

namespace Analyzer
{
    public class HttpAnalysisResult
    {
        public bool Success { get; }
        public Uri InitialUri { get; }
        public Uri? FinalUri { get; }
        public HttpStatusCode? StatusCode { get; }
        public Dictionary<string, string[]> Headers { get; }
        public bool Redirected { get; }
        public int RedirectCount { get; }
        public string? ErrorMessage { get; }

        // Based on the final URL only; null when there is no final URL, so no HTTPS claim is made.
        public bool? UsesHttps => FinalUri is null ? null : FinalUri.Scheme == Uri.UriSchemeHttps;

        // True when the request started on https but the final destination is plain http.
        public bool HttpsDowngraded =>
            FinalUri is not null
            && InitialUri.Scheme == Uri.UriSchemeHttps
            && FinalUri.Scheme == Uri.UriSchemeHttp;

        public HttpAnalysisResult(bool success, Uri initialUri, Uri? finalUri, HttpStatusCode? statusCode,
            Dictionary<string, string[]> headers, bool redirected, int redirectCount, string? errorMessage)
        {
            Success = success;
            InitialUri = initialUri;
            FinalUri = finalUri;
            StatusCode = statusCode;
            Headers = headers;
            Redirected = redirected;
            RedirectCount = redirectCount;
            ErrorMessage = errorMessage;
        }

        //public override string ToString()
        //{
        //    return $"{Success} - {InitialUri} - {FinalUri} - {StatusCode} - {Headers} - {Redirected} - {RedirectCount} - {ErrorMessage}";
        //}
    }
}
