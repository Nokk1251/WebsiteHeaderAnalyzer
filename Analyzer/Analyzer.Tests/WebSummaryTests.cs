using System.Net;
using System.Text.Json;

namespace Analyzer.Tests
{
    public class WebSummaryTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
            {
                _send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_send(request));
        }

        private static HttpAnalysisResult Success(Dictionary<string, string[]> headers) =>
            new HttpAnalysisResult(true, new Uri("https://example.com/"), new Uri("https://example.com/"),
                HttpStatusCode.OK, headers, false, 0, null);

        private static string Render(Dictionary<string, string[]> headers)
        {
            HttpAnalysisResult result = Success(headers);
            return WebPage.RenderReport(result, new SecurityHeaderAnalyzer().Analyze(result.Headers));
        }

        private static readonly Dictionary<string, string[]> MixedHeaders = new Dictionary<string, string[]>
        {
            { "X-Frame-Options", new[] { "DENY" } },
            { "Content-Security-Policy", new[] { "script-src 'unsafe-inline' https://cdn.example.com" } }
        };

        [Fact]
        public void Summary_ShowsCoverageCountAndNoVerdict()
        {
            string html = Render(MixedHeaders);

            Assert.Contains("<div class=\"big\">2<span> / 6</span></div>", html);
            Assert.Contains("4 missing; 1 present with values worth reviewing. This is a count, not a verdict.", html);
            Assert.Contains("width:33%", html);
        }

        [Fact]
        public void Headers_AreGroupedAndColorCoded()
        {
            string html = Render(MixedHeaders);

            Assert.Contains("g-miss", html);
            Assert.Contains("g-warn", html);
            Assert.Contains("g-ok", html);
            Assert.Contains("Missing (4)", html);
            Assert.Contains("Present, values worth reviewing (1)", html);
            Assert.Contains("Present (1)", html);
            Assert.Contains("finding review", html);
            Assert.Contains("finding rec", html);
        }

        [Fact]
        public void EmptyGroups_AreNotRendered()
        {
            string html = Render(new Dictionary<string, string[]>());

            Assert.Contains("g-miss", html);
            Assert.DoesNotContain("class=\"group g-warn\"", html);
            Assert.DoesNotContain("class=\"group g-ok\"", html);
        }

        [Fact]
        public void Filter_IsCssOnlyAndPageContainsNoScript()
        {
            string html = Render(MixedHeaders);

            Assert.Contains("id=\"f-missing\"", html);
            Assert.Contains("Missing only", html);
            Assert.Contains("#f-missing:checked~.hdrs .group:not(.g-miss){display:none}", html);
            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LongCspValue_IsShownCompleteAndSelectableAsOne()
        {
            string policy = "default-src 'self'; " + string.Join("; ", Enumerable.Range(1, 40).Select(i => $"img-src https://host{i}.example.com"));
            string html = Render(new Dictionary<string, string[]> { { "Content-Security-Policy", new[] { policy } } });

            Assert.Contains(WebUtility.HtmlEncode(policy), html);
            Assert.Contains("user-select:all", html);
        }

        [Fact]
        public void DynamicValues_AreHtmlEncodedEverywhere()
        {
            string html = Render(new Dictionary<string, string[]>
            {
                { "Referrer-Policy", new[] { "<img src=x onerror=alert(1)>" } }
            });

            Assert.DoesNotContain("<img src=x", html);
            Assert.Contains("&lt;img src=x", html);
        }

        [Fact]
        public void ExportLink_PointsToJsonWithEncodedUrl()
        {
            string html = Render(MixedHeaders);

            Assert.Contains("href=\"/?format=json&amp;url=https%3A%2F%2Fexample.com%2F\"", html);
            Assert.Contains("Download report as JSON", html);
        }

        [Fact]
        public void FailedAnalysis_StillOffersNoBrokenSummary()
        {
            var failed = new HttpAnalysisResult(false, new Uri("https://example.com"), null, null,
                new Dictionary<string, string[]>(), false, 0, "Timed out.");

            string html = WebPage.RenderReport(failed, new SecurityHeaderAnalyzer().Analyze(failed.Headers));

            Assert.Contains("Analysis failed: Timed out.", html);
            Assert.DoesNotContain("class=\"big\"", html);
        }

        [Fact]
        public void ReportJson_ContainsCoverageHeadersAndFindings()
        {
            HttpAnalysisResult result = Success(MixedHeaders);

            using JsonDocument doc = JsonDocument.Parse(ReportJson.Build(result, new SecurityHeaderAnalyzer().Analyze(result.Headers)));
            JsonElement root = doc.RootElement;

            Assert.Equal(200, root.GetProperty("statusCode").GetInt32());
            Assert.Equal(2, root.GetProperty("coverage").GetProperty("present").GetInt32());
            Assert.Equal(6, root.GetProperty("coverage").GetProperty("checkedHeaders").GetInt32());
            JsonElement csp = root.GetProperty("headers").EnumerateArray().Single(h => h.GetProperty("name").GetString() == "Content-Security-Policy");
            Assert.True(csp.GetProperty("present").GetBoolean());
            Assert.Equal("script-src 'unsafe-inline' https://cdn.example.com", csp.GetProperty("values")[0].GetString());
            Assert.Contains(csp.GetProperty("findings").EnumerateArray(), f => f.GetProperty("needsAttention").GetBoolean());
            JsonElement hsts = root.GetProperty("headers").EnumerateArray().Single(h => h.GetProperty("name").GetString() == "Strict-Transport-Security");
            Assert.False(hsts.GetProperty("present").GetBoolean());
            Assert.NotEqual(JsonValueKind.Null, hsts.GetProperty("recommendation").ValueKind);
        }

        [Fact]
        public void ReportJson_FailedAnalysis_HasNullStatusAndError()
        {
            var failed = new HttpAnalysisResult(false, new Uri("https://example.com"), null, null,
                new Dictionary<string, string[]>(), false, 0, "Timed out.");

            using JsonDocument doc = JsonDocument.Parse(ReportJson.Build(failed, new SecurityHeaderAnalyzer().Analyze(failed.Headers)));

            Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("statusCode").ValueKind);
            Assert.Equal("Timed out.", doc.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        public async Task HandleJson_ValidUrl_ReturnsJsonReport()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var (status, json) = await WebServer.HandleJsonAsync("/", "example.com", new HttpAnalyzer(handler));

            Assert.Equal(200, status);
            using JsonDocument doc = JsonDocument.Parse(json);
            Assert.Equal(404, doc.RootElement.GetProperty("statusCode").GetInt32());
            Assert.Equal("https://example.com/", doc.RootElement.GetProperty("initialUrl").GetString());
        }

        [Theory]
        [InlineData("/", null, 400)]
        [InlineData("/", "ftp://example.com", 400)]
        [InlineData("/", "https://user:secret@example.com", 400)]
        [InlineData("/admin", "https://example.com", 404)]
        public async Task HandleJson_InvalidRequests_ReturnJsonErrors(string path, string? url, int expectedStatus)
        {
            var handler = new StubHandler(_ => throw new InvalidOperationException("must not be called"));

            var (status, json) = await WebServer.HandleJsonAsync(path, url, new HttpAnalyzer(handler));

            Assert.Equal(expectedStatus, status);
            using JsonDocument doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("error", out _));
            Assert.DoesNotContain("secret", json);
        }
    }
}
