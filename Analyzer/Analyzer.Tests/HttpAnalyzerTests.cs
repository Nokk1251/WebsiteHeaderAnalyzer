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
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
            Assert.DoesNotContain("Simulated network failure", result.ErrorMessage);
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

        private static void AssertFailureShape(HttpAnalysisResult result)
        {
            Assert.False(result.Success);
            Assert.Null(result.StatusCode);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Theory]
        [InlineData(HttpRequestError.NameResolutionError, "could not be found")]
        [InlineData(HttpRequestError.ConnectionError, "Could not connect")]
        [InlineData(HttpRequestError.SecureConnectionError, "TLS")]
        [InlineData(HttpRequestError.Unknown, "network error")]
        public async Task Analyze_NetworkFailure_ReturnsFriendlyMessageWithoutInternalDetails(HttpRequestError error, string expectedFragment)
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new HttpRequestException(error, "internal diagnostic details"));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            AssertFailureShape(result);
            Assert.Contains(expectedFragment, result.ErrorMessage);
            Assert.DoesNotContain("internal diagnostic details", result.ErrorMessage);
        }

        [Fact]
        public async Task Analyze_Timeout_ReturnsTimeoutFailure()
        {
            var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("simulated timeout"));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            AssertFailureShape(result);
            Assert.Contains("timed out", result.ErrorMessage);
            Assert.DoesNotContain("simulated timeout", result.ErrorMessage);
        }

        [Fact]
        public async Task Analyze_CallerCancellation_ReturnsCancelledFailureDistinctFromTimeout()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException());
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"), cts.Token);

            AssertFailureShape(result);
            Assert.Contains("cancelled", result.ErrorMessage);
            Assert.DoesNotContain("timed out", result.ErrorMessage);
        }

        [Fact]
        public async Task Analyze_CancelledWhileRequestInFlight_ReturnsCancelledFailure()
        {
            using var cts = new CancellationTokenSource();
            var handler = new FakeHttpMessageHandler(_ =>
            {
                cts.Cancel();
                throw new TaskCanceledException();
            });
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com"), cts.Token);

            AssertFailureShape(result);
            Assert.Contains("cancelled", result.ErrorMessage);
        }

        [Fact]
        public async Task Analyze_NetworkFailureAfterRedirect_KeepsRedirectInformation()
        {
            var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath == "/"
                ? Task.FromResult(Redirect(HttpStatusCode.Found, "/next"))
                : throw new HttpRequestException(HttpRequestError.ConnectionError));
            var analyzer = new HttpAnalyzer(handler);

            var result = await analyzer.Analyze(new Uri("https://example.com/"));

            AssertFailureShape(result);
            Assert.True(result.Redirected);
            Assert.Equal(1, result.RedirectCount);
        }

        [Fact]
        public async Task Analyze_IncludesResponseAndContentHeaders()
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("hi") };
                response.Headers.TryAddWithoutValidation("X-Frame-Options", "DENY");
                return Task.FromResult(response);
            });

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            Assert.Equal(new[] { "DENY" }, result.Headers["X-Frame-Options"]);
            Assert.Contains("text/plain", result.Headers["Content-Type"][0]);
        }

        [Fact]
        public async Task Analyze_HeaderNames_AreCaseInsensitive()
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.TryAddWithoutValidation("Strict-Transport-Security", "max-age=1");
                return Task.FromResult(response);
            });

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            Assert.True(result.Headers.ContainsKey("strict-transport-security"));
            Assert.Equal(new[] { "max-age=1" }, result.Headers["STRICT-TRANSPORT-SECURITY"]);
        }

        [Fact]
        public async Task Analyze_MultiValueHeader_PreservesAllValues()
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.TryAddWithoutValidation("Via", new[] { "1.1 a", "1.1 b" });
                return Task.FromResult(response);
            });

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            Assert.Equal(new[] { "1.1 a", "1.1 b" }, result.Headers["Via"]);
        }

        [Fact]
        public async Task Analyze_ResponseWithoutContentOrHeaders_ReturnsEmptyNonNullHeaders()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = null! }));

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            Assert.True(result.Success);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
        }

        [Fact]
        public async Task Analyze_Headers_AreThoseOfFinalResponseNotRedirects()
        {
            var handler = new FakeHttpMessageHandler(request =>
            {
                HttpResponseMessage response = request.RequestUri!.AbsolutePath == "/"
                    ? Redirect(HttpStatusCode.Found, "/final")
                    : new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.TryAddWithoutValidation(
                    request.RequestUri.AbsolutePath == "/" ? "X-Redirect-Only" : "X-Final-Only", "1");
                return Task.FromResult(response);
            });

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com/"));

            Assert.True(result.Headers.ContainsKey("X-Final-Only"));
            Assert.False(result.Headers.ContainsKey("X-Redirect-Only"));
        }

        [Fact]
        public void Constructor_WithoutHandler_CreatesAnalyzer()
        {
            var analyzer = new HttpAnalyzer();

            Assert.NotNull(analyzer);
        }
    }
}
