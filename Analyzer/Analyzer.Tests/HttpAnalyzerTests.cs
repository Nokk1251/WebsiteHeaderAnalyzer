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

        [Fact]
        public async Task Analyze_RedirectResponse_IsReportedAsReceivedAndNotFollowed()
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri("https://example.com/other");
                return Task.FromResult(response);
            });
            var analyzer = new HttpAnalyzer(handler);
            var uri = new Uri("https://example.com");

            var result = await analyzer.Analyze(uri);

            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.Found, result.StatusCode);
            Assert.Equal(uri, result.FinalUri);
            Assert.False(result.Redirected);
            Assert.Equal(0, result.RedirectCount);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public void Constructor_WithoutHandler_CreatesAnalyzer()
        {
            var analyzer = new HttpAnalyzer();

            Assert.NotNull(analyzer);
        }
    }
}
