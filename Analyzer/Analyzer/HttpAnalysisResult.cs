
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
