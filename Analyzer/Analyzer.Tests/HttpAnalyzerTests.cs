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

        [Fact]
        public void Constructor_WithoutHandler_CreatesAnalyzer()
        {
            var analyzer = new HttpAnalyzer();

            Assert.NotNull(analyzer);
        }
    }
}
