using System.Net;

namespace Analyzer.Tests
{
    public class WebInterfaceTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

            public int CallCount { get; private set; }

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
            {
                _send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(_send(request));
            }
        }

        private static HttpAnalysisResult Success(string initial, string final, Dictionary<string, string[]>? headers = null) =>
            new HttpAnalysisResult(true, new Uri(initial), new Uri(final), HttpStatusCode.OK,
                headers ?? new Dictionary<string, string[]>(), initial != final, initial != final ? 1 : 0, null);

        private static string Render(HttpAnalysisResult result) =>
            WebPage.RenderReport(result, new SecurityHeaderAnalyzer().Analyze(result.Headers));

        // --- WebPage (pure rendering) ---

        [Fact]
        public void RenderForm_ShowsUrlFieldAndDisclaimer()
        {
            string html = WebPage.RenderForm();

            Assert.Contains("name=\"url\"", html);
            Assert.Contains(WebUtility.HtmlEncode(ConsoleReport.Disclaimer), html);
        }

        [Fact]
        public void RenderForm_EncodesMessageAndCurrentUrl()
        {
            string html = WebPage.RenderForm("<script>alert(1)</script>", "\"><img src=x>");

            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.DoesNotContain("<img src=x>", html);
            Assert.Contains("&lt;script&gt;", html);
        }

        [Fact]
        public void RenderReport_Success_ShowsSameInformationAsConsoleReport()
        {
            var headers = new Dictionary<string, string[]> { ["X-Frame-Options"] = new[] { "DENY" } };

            string html = Render(Success("http://example.com/", "https://example.com/", headers));

            Assert.Contains("http://example.com/", html);
            Assert.Contains("https://example.com/", html);
            Assert.Contains("200 OK", html);
            Assert.Contains("Redirects followed", html);
            Assert.Contains(">DENY</code>", html);
            Assert.Contains("missing", html);
            foreach (SecurityHeaderDefinition definition in SecurityHeaderAnalyzer.Definitions)
            {
                Assert.Contains(WebUtility.HtmlEncode(definition.Purpose), html);
                Assert.Contains(WebUtility.HtmlEncode(definition.Limitation), html);
            }
            Assert.Contains(WebUtility.HtmlEncode(ConsoleReport.Disclaimer), html);
        }

        [Fact]
        public void RenderReport_HeaderValueWithMarkup_IsEncoded()
        {
            var headers = new Dictionary<string, string[]>
            {
                ["Content-Security-Policy"] = new[] { "<script>alert(1)</script>" }
            };

            string html = Render(Success("https://example.com/", "https://example.com/", headers));

            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        }

        [Fact]
        public void RenderReport_Failure_ShowsFriendlyMessageAndNoHeaderSection()
        {
            var result = new HttpAnalysisResult(false, new Uri("https://example.com/"), null, null,
                new Dictionary<string, string[]>(), false, 0, "Could not connect to the server.");

            string html = Render(result);

            Assert.Contains("Analysis failed: Could not connect to the server.", html);
            Assert.DoesNotContain("Security headers", html);
            Assert.DoesNotContain("Purpose:", html);
            Assert.Contains(WebUtility.HtmlEncode(ConsoleReport.Disclaimer), html);
        }

        [Fact]
        public void Pages_HaveNoScriptsOrExternalResources_AndSupportResponsiveDarkLayout()
        {
            string form = WebPage.RenderForm();
            string report = Render(Success("https://example.com/", "https://example.com/"));

            foreach (string html in new[] { form, report })
            {
                Assert.DoesNotContain("<script", html);
                Assert.DoesNotContain("src=", html);
                Assert.DoesNotContain("http-equiv", html);
                Assert.DoesNotContain("@import", html);
                Assert.DoesNotContain("url(", html);
                Assert.DoesNotContain("<link", html);
                Assert.Contains("name=\"viewport\"", html);
                Assert.Contains("prefers-color-scheme:dark", html);
            }
        }

        [Fact]
        public void RenderReport_ShowsPresentAndMissingAsBadges()
        {
            var headers = new Dictionary<string, string[]> { ["X-Frame-Options"] = new[] { "DENY" } };

            string html = Render(Success("https://example.com/", "https://example.com/", headers));

            Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "badge ok\">present"));
            Assert.Equal(5, System.Text.RegularExpressions.Regex.Matches(html, "badge miss\">missing").Count);
        }

        // --- WebServer.HandleAsync (no sockets) ---

        [Fact]
        public async Task Handle_RootWithoutUrl_ShowsFormAndSendsNoRequest()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var (status, html) = await WebServer.HandleAsync("/", null, new HttpAnalyzer(handler));

            Assert.Equal(200, status);
            Assert.Contains("name=\"url\"", html);
            Assert.Equal(0, handler.CallCount);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a url")]
        [InlineData("ftp://example.com")]
        public async Task Handle_InvalidUrl_ReturnsBadRequestWithReasonAndSendsNoRequest(string url)
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var (status, html) = await WebServer.HandleAsync("/", url, new HttpAnalyzer(handler));

            Assert.Equal(400, status);
            Assert.Contains("class=\"alert\"", html);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task Handle_ValidUrl_ReturnsReport()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var (status, html) = await WebServer.HandleAsync("/", "https://example.com/x", new HttpAnalyzer(handler));

            Assert.Equal(200, status);
            Assert.Contains("404 NotFound", html);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Handle_NetworkFailure_ShowsFriendlyMessage()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("Simulated network failure"));

            var (status, html) = await WebServer.HandleAsync("/", "https://example.com", new HttpAnalyzer(handler));

            Assert.Equal(200, status);
            Assert.Contains("Analysis failed:", html);
            Assert.DoesNotContain("Simulated network failure", html);
        }

        [Fact]
        public async Task Handle_UnexpectedException_ReturnsServerErrorWithoutDetails()
        {
            var handler = new StubHandler(_ => throw new InvalidOperationException("boom with internal details"));

            var (status, html) = await WebServer.HandleAsync("/", "https://example.com", new HttpAnalyzer(handler));

            Assert.Equal(500, status);
            Assert.DoesNotContain("boom with internal details", html);
        }

        [Fact]
        public async Task Handle_UnknownPath_ReturnsNotFoundAndSendsNoRequest()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var (status, _) = await WebServer.HandleAsync("/admin", "https://example.com", new HttpAnalyzer(handler));

            Assert.Equal(404, status);
            Assert.Equal(0, handler.CallCount);
        }
    }
}
