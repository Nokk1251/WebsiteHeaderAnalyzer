using System.Net;

namespace Analyzer.Tests
{
    public class HttpAnalyzerTests
    {
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _send;

            public int CallCount { get; private set; }
            public bool IsDisposed { get; private set; }

            public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
            {
                _send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return _send(request);
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        private static FakeHttpMessageHandler CreateFailingHandler() =>
            new FakeHttpMessageHandler(_ => throw new HttpRequestException("Simulated network failure"));

        [Fact]
        public async Task Analyze_UsesInjectedHandler_WhenProvided()
        {
            var handler = CreateFailingHandler();
            var analyzer = new HttpAnalyzer(handler);
            var uri = new Uri("https://example.com");

            var result = await analyzer.Analyze(uri);

            Assert.Equal(1, handler.CallCount);
            Assert.False(result.Success);
            Assert.Equal(uri, result.InitialUri);
            Assert.Null(result.StatusCode);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
            Assert.Equal("Simulated network failure", result.ErrorMessage);
        }

        [Fact]
        public async Task Analyze_DoesNotDisposeInjectedHandler_AndAllowsReuse()
        {
            var handler = CreateFailingHandler();
            var analyzer = new HttpAnalyzer(handler);
            var uri = new Uri("https://example.com");

            await analyzer.Analyze(uri);
            await analyzer.Analyze(uri);

            Assert.False(handler.IsDisposed);
            Assert.Equal(2, handler.CallCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task Analyze_ReceivedResponse_ReportsSuccessAndStatusCode(HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(statusCode)));
            var analyzer = new HttpAnalyzer(handler);
            var uri = new Uri("https://example.com/page");

            var result = await analyzer.Analyze(uri);

            Assert.True(result.Success);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(uri, result.InitialUri);
            Assert.Equal(uri, result.FinalUri);
            Assert.NotNull(result.Headers);
            Assert.False(result.Redirected);
            Assert.Equal(0, result.RedirectCount);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Analyze_ResponseWithoutLocationHeader_DoesNotThrow()
        {
            var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            Assert.True(result.Success);
        }

        private static HttpResponseMessage Redirect(HttpStatusCode status, string? location)
        {
            var response = new HttpResponseMessage(status);
            if (location is not null)
            {
                response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
            }
            return response;
        }

        [Theory]
        [InlineData(HttpStatusCode.MovedPermanently)]
        [InlineData(HttpStatusCode.Found)]
        [InlineData(HttpStatusCode.SeeOther)]
        [InlineData(HttpStatusCode.TemporaryRedirect)]
        [InlineData(HttpStatusCode.PermanentRedirect)]
        public async Task Analyze_SingleRedirect_IsFollowedAndFinalUriReported(HttpStatusCode redirectStatus)
        {
            var handler = new FakeHttpMessageHandler(request =>
                Task.FromResult(request.RequestUri!.AbsolutePath == "/"
                    ? Redirect(redirectStatus, "https://example.com/other")
                    : new HttpResponseMessage(HttpStatusCode.OK)));
            var analyzer = new HttpAnalyzer(handler);
            var uri = new Uri("https://example.com/");

            var result = await analyzer.Analyze(uri);

            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(uri, result.InitialUri);
            Assert.Equal(new Uri("https://example.com/other"), result.FinalUri);
            Assert.True(result.Redirected);
            Assert.Equal(1, result.RedirectCount);
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Analyze_MultipleRedirects_AreCountedAndFinalUriReported()
        {
            var handler = new FakeHttpMessageHandler(request => Task.FromResult(request.RequestUri!.AbsolutePath switch
            {
                "/a" => Redirect(HttpStatusCode.MovedPermanently, "/b"),
                "/b" => Redirect(HttpStatusCode.Found, "https://other.example.com/c"),
                _ => new HttpResponseMessage(HttpStatusCode.OK)
            }));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com/a"));

            Assert.True(result.Success);
            Assert.Equal(new Uri("https://example.com/a"), result.InitialUri);
            Assert.Equal(new Uri("https://other.example.com/c"), result.FinalUri);
            Assert.True(result.Redirected);
            Assert.Equal(2, result.RedirectCount);
        }

        [Fact]
        public async Task Analyze_RelativeLocation_IsResolvedAgainstCurrentUri()
        {
            var handler = new FakeHttpMessageHandler(request => Task.FromResult(request.RequestUri!.AbsolutePath == "/dir/start"
                ? Redirect(HttpStatusCode.Found, "next")
                : new HttpResponseMessage(HttpStatusCode.OK)));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com/dir/start"));

            Assert.True(result.Success);
            Assert.Equal(new Uri("https://example.com/dir/next"), result.FinalUri);
        }

        [Fact]
        public async Task Analyze_RedirectFromHttpToHttps_ReportsHttpsFinalUri()
        {
            var handler = new FakeHttpMessageHandler(request => Task.FromResult(request.RequestUri!.Scheme == "http"
                ? Redirect(HttpStatusCode.MovedPermanently, "https://example.com/")
                : new HttpResponseMessage(HttpStatusCode.OK)));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("http://example.com/"));

            Assert.Equal("http", result.InitialUri.Scheme);
            Assert.Equal("https", result.FinalUri!.Scheme);
        }

        [Fact]
        public async Task Analyze_RedirectResponseWithoutLocation_ReturnsFailure()
        {
            var handler = new FakeHttpMessageHandler(_ => Task.FromResult(Redirect(HttpStatusCode.Found, null)));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            Assert.False(result.Success);
            Assert.Null(result.StatusCode);
            Assert.Null(result.FinalUri);
            Assert.Empty(result.Headers);
            Assert.False(result.Redirected);
            Assert.Equal(0, result.RedirectCount);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
            Assert.Equal(1, handler.CallCount);
        }

        [Theory]
        [InlineData("ftp://example.com/file")]
        [InlineData("mailto:user@example.com")]
        public async Task Analyze_RedirectToUnsupportedScheme_ReturnsFailureWithoutFollowing(string location)
        {
            var handler = new FakeHttpMessageHandler(_ => Task.FromResult(Redirect(HttpStatusCode.Found, location)));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            Assert.False(result.Success);
            Assert.False(result.Redirected);
            Assert.Equal(0, result.RedirectCount);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Analyze_RedirectLoop_StopsAtLimitAndReturnsFailure()
        {
            var handler = new FakeHttpMessageHandler(_ => Task.FromResult(Redirect(HttpStatusCode.Found, "https://example.com/loop")));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com/loop"));

            Assert.False(result.Success);
            Assert.Null(result.FinalUri);
            Assert.Equal(10, result.RedirectCount);
            Assert.Equal(11, handler.CallCount);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Fact]
        public async Task Analyze_TenRedirects_AreAllowed()
        {
            var result = await AnalyzeChainOfRedirects(10);

            Assert.True(result.Success);
            Assert.Equal(10, result.RedirectCount);
            Assert.Equal(new Uri("https://example.com/10"), result.FinalUri);
        }

        [Fact]
        public async Task Analyze_ElevenRedirects_ExceedLimitAndReturnFailure()
        {
            var result = await AnalyzeChainOfRedirects(11);

            Assert.False(result.Success);
            Assert.Equal(10, result.RedirectCount);
        }

        // /0 redirects to /1, ..., /(n-1) redirects to /n, and /n returns 200.
        private static Task<HttpAnalysisResult> AnalyzeChainOfRedirects(int redirects)
        {
            var handler = new FakeHttpMessageHandler(request =>
            {
                int step = int.Parse(request.RequestUri!.AbsolutePath.TrimStart('/'));
                return Task.FromResult(step < redirects
                    ? Redirect(HttpStatusCode.Found, $"/{step + 1}")
                    : new HttpResponseMessage(HttpStatusCode.OK));
            });
            return new HttpAnalyzer(handler).Analyze(new Uri("https://example.com/0"));
        }

        private static HttpAnalyzer CreateRedirectingAnalyzer(string fromScheme, string toUrl) =>
            new HttpAnalyzer(new FakeHttpMessageHandler(request => Task.FromResult(request.RequestUri!.Scheme == fromScheme
                ? Redirect(HttpStatusCode.MovedPermanently, toUrl)
                : new HttpResponseMessage(HttpStatusCode.OK))));

        [Fact]
        public async Task Analyze_HttpsWithoutRedirect_UsesHttpsAndIsNotDowngraded()
        {
            var analyzer = new HttpAnalyzer(new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

            var result = await analyzer.Analyze(new Uri("https://example.com/"));

            Assert.True(result.UsesHttps);
            Assert.False(result.HttpsDowngraded);
        }

        [Fact]
        public async Task Analyze_PlainHttpWithoutRedirect_DoesNotUseHttpsAndIsNotDowngraded()
        {
            var analyzer = new HttpAnalyzer(new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

            var result = await analyzer.Analyze(new Uri("http://example.com/"));

            Assert.False(result.UsesHttps);
            Assert.False(result.HttpsDowngraded);
        }

        [Fact]
        public async Task Analyze_RedirectFromHttpToHttps_UsesHttpsAndIsNotDowngraded()
        {
            var result = await CreateRedirectingAnalyzer("http", "https://example.com/").Analyze(new Uri("http://example.com/"));

            Assert.True(result.UsesHttps);
            Assert.False(result.HttpsDowngraded);
        }

        [Fact]
        public async Task Analyze_RedirectFromHttpsToHttp_DoesNotUseHttpsAndIsDowngraded()
        {
            var result = await CreateRedirectingAnalyzer("https", "http://example.com/").Analyze(new Uri("https://example.com/"));

            Assert.False(result.UsesHttps);
            Assert.True(result.HttpsDowngraded);
        }

        [Fact]
        public async Task Analyze_FailureWithoutFinalUri_MakesNoHttpsClaim()
        {
            var result = await new HttpAnalyzer(CreateFailingHandler()).Analyze(new Uri("https://example.com/"));

            Assert.False(result.Success);
            Assert.Null(result.UsesHttps);
            Assert.False(result.HttpsDowngraded);
        }

        [Fact]
        public void Constructor_WithoutHandler_CreatesAnalyzer()
        {
            var analyzer = new HttpAnalyzer();

            Assert.NotNull(analyzer);
        }
    }
}
