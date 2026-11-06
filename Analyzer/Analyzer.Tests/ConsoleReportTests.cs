using System.Net;

namespace Analyzer.Tests
{
    public class ConsoleReportTests
    {
        private static HttpAnalysisResult Success(string initial, string final, HttpStatusCode status = HttpStatusCode.OK,
            int redirects = 0, Dictionary<string, string[]>? headers = null) =>
            new HttpAnalysisResult(true, new Uri(initial), new Uri(final), status,
                headers ?? new Dictionary<string, string[]>(), redirects > 0, redirects, null);

        private static string BuildReport(HttpAnalysisResult result) =>
            ConsoleReport.Build(result, new SecurityHeaderAnalyzer().Analyze(result.Headers));

        [Fact]
        public void Build_Success_ShowsUrlsStatusRedirectsHttpsAndHeaders()
        {
            var headers = new Dictionary<string, string[]> { ["X-Frame-Options"] = new[] { "DENY" } };
            var result = Success("https://example.com/", "https://example.com/", headers: headers);

            string report = BuildReport(result);

            Assert.Contains("Initial URL: https://example.com/", report);
            Assert.Contains("Final URL: https://example.com/", report);
            Assert.Contains("Status code: 200 OK", report);
            Assert.Contains("Redirects followed: 0", report);
            Assert.Contains("HTTPS (final URL): Yes", report);
            Assert.Contains("[present] X-Frame-Options: DENY", report);
            Assert.Contains("[missing] Content-Security-Policy", report);
            Assert.Contains(ConsoleReport.Disclaimer, report);
        }

        [Fact]
        public void Build_Redirected_ShowsBothUrlsAndRedirectCount()
        {
            var result = Success("http://example.com/", "https://www.example.com/home", redirects: 2);

            string report = BuildReport(result);

            Assert.Contains("Initial URL: http://example.com/", report);
            Assert.Contains("Final URL: https://www.example.com/home", report);
            Assert.Contains("Redirects followed: 2", report);
            Assert.Contains("HTTPS (final URL): Yes", report);
        }

        [Fact]
        public void Build_HttpOnly_ReportsNoHttps()
        {
            var result = Success("http://example.com/", "http://example.com/");

            string report = BuildReport(result);

            Assert.Contains("HTTPS (final URL): No - the final URL uses plain HTTP", report);
        }

        [Fact]
        public void Build_HttpsToHttpDowngrade_ReportsDowngrade()
        {
            var result = Success("https://example.com/", "http://example.com/", redirects: 1);

            string report = BuildReport(result);

            Assert.Contains("started on HTTPS but ended on plain HTTP", report);
        }

        [Fact]
        public void Build_NonSuccessStatus_IsStillReportedAsReceivedResponse()
        {
            var result = Success("https://example.com/x", "https://example.com/x", HttpStatusCode.NotFound);

            string report = BuildReport(result);

            Assert.Contains("Status code: 404 NotFound", report);
            Assert.DoesNotContain("Analysis failed", report);
        }

        [Fact]
        public void Build_Failure_ShowsFriendlyMessageAndNoHeaderSection()
        {
            var result = new HttpAnalysisResult(false, new Uri("https://example.com/"), null, null,
                new Dictionary<string, string[]>(), false, 0, "Could not connect to the server.");

            string report = BuildReport(result);

            Assert.Contains("Initial URL: https://example.com/", report);
            Assert.Contains("Analysis failed: Could not connect to the server.", report);
            Assert.DoesNotContain("Security headers", report);
            Assert.DoesNotContain("[missing]", report);
            Assert.DoesNotContain("HTTPS (final URL)", report);
            Assert.Contains(ConsoleReport.Disclaimer, report);
        }

        [Fact]
        public void Build_MultiValueHeader_JoinsAllValues()
        {
            var headers = new Dictionary<string, string[]>
            {
                ["Content-Security-Policy"] = new[] { "default-src 'self'", "img-src *" }
            };
            var result = Success("https://example.com/", "https://example.com/", headers: headers);

            string report = BuildReport(result);

            Assert.Contains("[present] Content-Security-Policy: default-src 'self' | img-src *", report);
        }
    }
}
