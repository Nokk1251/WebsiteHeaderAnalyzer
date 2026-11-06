using System.Net;

namespace Analyzer.Tests
{
    public class ConsoleAppTests
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

        private static async Task<(int ExitCode, string Output, string Error)> Run(string? input, StubHandler handler)
        {
            var output = new StringWriter();
            var error = new StringWriter();

            int exitCode = await ConsoleApp.RunAsync(new StringReader(input ?? ""), output, error, new HttpAnalyzer(handler));

            return (exitCode, output.ToString(), error.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a url")]
        [InlineData("ftp://example.com")]
        public async Task Run_InvalidUrl_PrintsReasonReturnsOneAndSendsNoRequest(string input)
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var (exitCode, output, _) = await Run(input, handler);

            Assert.Equal(1, exitCode);
            Assert.Contains("URL is invalid.", output);
            Assert.Contains("Reason:", output);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task Run_ValidUrl_PrintsReportAndReturnsZero()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var (exitCode, output, error) = await Run("https://example.com", handler);

            Assert.Equal(0, exitCode);
            Assert.Contains("=== Website analysis report ===", output);
            Assert.Contains("Status code: 200 OK", output);
            Assert.Equal(1, handler.CallCount);
            Assert.Equal("", error);
        }

        [Fact]
        public async Task Run_UrlWithSurroundingSpaces_IsTrimmedBeforeRequest()
        {
            Uri? requested = null;
            var handler = new StubHandler(request =>
            {
                requested = request.RequestUri;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var (exitCode, _, _) = await Run("  https://example.com/page  ", handler);

            Assert.Equal(0, exitCode);
            Assert.Equal(new Uri("https://example.com/page"), requested);
        }

        [Fact]
        public async Task Run_NotFoundResponse_IsAReceivedResponseAndReturnsZero()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var (exitCode, output, _) = await Run("https://example.com/missing", handler);

            Assert.Equal(0, exitCode);
            Assert.Contains("Status code: 404 NotFound", output);
            Assert.DoesNotContain("Analysis failed", output);
        }

        [Fact]
        public async Task Run_NetworkFailure_PrintsFriendlyFailureReportAndReturnsOne()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("Simulated network failure"));

            var (exitCode, output, _) = await Run("https://example.com", handler);

            Assert.Equal(1, exitCode);
            Assert.Contains("Analysis failed:", output);
            Assert.DoesNotContain("Simulated network failure", output);
            Assert.DoesNotContain("Security headers", output);
        }

        [Fact]
        public async Task Run_UnexpectedException_IsReportedWithoutCrashingAndReturnsOne()
        {
            var handler = new StubHandler(_ => throw new InvalidOperationException("boom with internal details"));

            var (exitCode, output, error) = await Run("https://example.com", handler);

            Assert.Equal(1, exitCode);
            Assert.Contains("An unexpected error occurred", error);
            Assert.DoesNotContain("boom with internal details", error);
            Assert.DoesNotContain("boom with internal details", output);
        }
    }
}
