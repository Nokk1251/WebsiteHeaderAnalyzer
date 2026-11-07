using System.Net;
using System.Net.Sockets;

namespace Analyzer.Tests
{
    public class HttpHardeningTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

            public List<HttpRequestMessage> Requests { get; } = new();

            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
            {
                _send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return _send(request, cancellationToken);
            }
        }

        private sealed class ThrowingContent : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
                throw new InvalidOperationException("The body must not be read.");

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }

        // A real local HTTP server, so the default handler (with its address guard) can be tested without the internet.
        private sealed class LocalServer : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();
            private readonly Task _loop;

            public int Port { get; }
            public List<string?> CookieHeaders { get; } = new();

            public LocalServer(Func<HttpListenerRequest, HttpListenerResponse, bool> handle)
            {
                TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                Port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();

                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Start();
                _loop = Task.Run(async () =>
                {
                    while (_listener.IsListening)
                    {
                        HttpListenerContext context;
                        try
                        {
                            context = await _listener.GetContextAsync();
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        CookieHeaders.Add(context.Request.Headers["Cookie"]);
                        handle(context.Request, context.Response);
                        context.Response.Close();
                    }
                });
            }

            public Uri Url(string path = "/") => new Uri($"http://localhost:{Port}{path}");

            public void Dispose()
            {
                _listener.Close();
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
        }

        // --- AddressGuard ---

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.5.5.5")]
        [InlineData("10.1.2.3")]
        [InlineData("172.16.0.1")]
        [InlineData("172.31.255.255")]
        [InlineData("192.168.1.1")]
        [InlineData("169.254.169.254")]
        [InlineData("100.64.0.1")]
        [InlineData("0.0.0.0")]
        [InlineData("224.0.0.1")]
        [InlineData("::1")]
        [InlineData("::")]
        [InlineData("fe80::1")]
        [InlineData("fc00::1")]
        [InlineData("fd12:3456::1")]
        [InlineData("ff02::1")]
        [InlineData("::ffff:127.0.0.1")]
        [InlineData("::ffff:10.0.0.1")]
        public void IsRestricted_LocalAndPrivateAddresses_AreRestricted(string address)
        {
            Assert.True(AddressGuard.IsRestricted(IPAddress.Parse(address)));
        }

        [Theory]
        [InlineData("93.184.216.34")]
        [InlineData("8.8.8.8")]
        [InlineData("172.15.0.1")]
        [InlineData("172.32.0.1")]
        [InlineData("100.63.0.1")]
        [InlineData("2606:4700:4700::1111")]
        [InlineData("::ffff:8.8.8.8")]
        public void IsRestricted_PublicAddresses_AreAllowed(string address)
        {
            Assert.False(AddressGuard.IsRestricted(IPAddress.Parse(address)));
        }

        [Theory]
        [InlineData("http://127.0.0.1:1/")]
        [InlineData("http://localhost:1/")]
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://[::1]:1/")]
        public async Task Analyze_DefaultHandler_BlocksLocalAndPrivateTargets(string url)
        {
            var result = await new HttpAnalyzer().Analyze(new Uri(url));

            Assert.False(result.Success);
            Assert.Contains("--allow-private", result.ErrorMessage);
            Assert.Empty(result.Headers);
        }

        [Fact]
        public async Task Analyze_DefaultHandler_AllowsLocalTargetWhenExplicitlyEnabled()
        {
            using var server = new LocalServer((_, response) =>
            {
                response.StatusCode = 200;
                response.Headers["X-Test"] = "yes";
                return true;
            });

            var result = await new HttpAnalyzer(allowPrivateAddresses: true).Analyze(server.Url());

            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(new[] { "yes" }, result.Headers["X-Test"]);
        }

        // --- Cookies ---

        [Fact]
        public async Task Analyze_DefaultHandler_DoesNotSendCookiesBetweenRequests()
        {
            using var server = new LocalServer((request, response) =>
            {
                if (request.Url!.AbsolutePath == "/")
                {
                    response.StatusCode = 302;
                    response.Headers["Location"] = "/next";
                    response.Headers.Add("Set-Cookie", "session=abc123; Path=/");
                }
                else
                {
                    response.StatusCode = 200;
                }
                return true;
            });

            var result = await new HttpAnalyzer(allowPrivateAddresses: true).Analyze(server.Url());

            Assert.True(result.Success);
            Assert.Equal(1, result.RedirectCount);
            Assert.Equal(2, server.CookieHeaders.Count);
            Assert.All(server.CookieHeaders, cookie => Assert.Null(cookie));
        }

        // --- Headers only, User-Agent, total deadline ---

        [Fact]
        public async Task Analyze_DoesNotReadTheResponseBody()
        {
            var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ThrowingContent()
            }));

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task Analyze_SendsDescriptiveUserAgent()
        {
            var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"));

            var request = Assert.Single(handler.Requests);
            Assert.NotEmpty(request.Headers.UserAgent);
            Assert.Contains("WebsiteSecurityHeaderAnalyzer", request.Headers.UserAgent.ToString());
        }

        [Fact]
        public async Task Analyze_TotalDeadline_StopsSlowAnalysisWithDistinctMessage()
        {
            var handler = new StubHandler(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var analyzer = new HttpAnalyzer(handler, totalTimeout: TimeSpan.FromMilliseconds(150));

            var result = await analyzer.Analyze(new Uri("https://example.com"));

            Assert.False(result.Success);
            Assert.Contains("took longer than", result.ErrorMessage);
            Assert.DoesNotContain("cancelled", result.ErrorMessage);
            Assert.Empty(result.Headers);
        }

        [Fact]
        public async Task Analyze_CallerCancellation_IsNotReportedAsTotalDeadline()
        {
            using var cts = new CancellationTokenSource();
            var handler = new StubHandler(async (_, token) =>
            {
                cts.Cancel();
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var result = await new HttpAnalyzer(handler).Analyze(new Uri("https://example.com"), cts.Token);

            Assert.False(result.Success);
            Assert.Contains("cancelled", result.ErrorMessage);
        }
    }
}
