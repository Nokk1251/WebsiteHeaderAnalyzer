using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Analyzer
{
    // Minimal local web interface on HttpListener (part of .NET, no extra packages). Localhost only.
    public static class WebServer
    {
        public const int DefaultPort = 5080;

        // Request logic kept free of sockets so it can be unit tested.
        public static async Task<(int StatusCode, string Html)> HandleAsync(string path, string? urlParameter,
            HttpAnalyzer httpAnalyzer, CancellationToken cancellationToken = default)
        {
            if (path != "/")
            {
                return (404, WebPage.RenderForm("Page not found."));
            }

            if (urlParameter is null)
            {
                return (200, WebPage.RenderForm());
            }

            ValidationResult validation = new UrlValidator().Validate(urlParameter);
            if (!validation.IsValid)
            {
                return (400, WebPage.RenderForm(validation.Message, urlParameter));
            }

            try
            {
                HttpAnalysisResult result = await httpAnalyzer.Analyze(new Uri(validation.TrimmedUrl), cancellationToken);
                IReadOnlyList<HeaderObservation> observations = new SecurityHeaderAnalyzer().Analyze(result.Headers);
                return (200, WebPage.RenderReport(result, observations));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Same last-resort boundary as the console flow: no internal details in the page.
                return (500, WebPage.RenderForm("An unexpected error occurred during the analysis.", urlParameter));
            }
        }

        public static async Task<int> RunAsync(int port, HttpAnalyzer httpAnalyzer, TextWriter output,
            CancellationToken cancellationToken, bool openBrowser = false)
        {
            using HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                listener.Start();
            }
            catch (HttpListenerException)
            {
                output.WriteLine($"Could not start the web interface on port {port}. The port may be in use.");
                return 1;
            }

            output.WriteLine($"Web interface running at http://localhost:{port}/ (press Ctrl+C to stop).");

            if (openBrowser)
            {
                TryOpenBrowser($"http://localhost:{port}/", output);
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(listener.Stop);

            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    break;
                }

                await RespondAsync(context, httpAnalyzer, cancellationToken);
            }

            return 0;
        }

        // A missing or failing browser must not stop the server, so the failure is reported and ignored.
        private static void TryOpenBrowser(string url, TextWriter output)
        {
            try
            {
                using Process? process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
            {
                output.WriteLine("Could not open the browser automatically. Open the address above manually.");
            }
        }

        private static async Task RespondAsync(HttpListenerContext context, HttpAnalyzer httpAnalyzer,
            CancellationToken cancellationToken)
        {
            try
            {
                int status;
                string html;

                if (context.Request.HttpMethod != "GET")
                {
                    status = 405;
                    html = WebPage.RenderForm("Only GET requests are supported.");
                }
                else
                {
                    string? url = context.Request.QueryString["url"];
                    (status, html) = await HandleAsync(context.Request.Url?.AbsolutePath ?? "/", url, httpAnalyzer, cancellationToken);
                }

                byte[] bytes = Encoding.UTF8.GetBytes(html);
                context.Response.StatusCode = status;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers["Cache-Control"] = "no-store";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'";
                await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpListenerException or OperationCanceledException or ObjectDisposedException)
            {
                // The client disconnected or the server is stopping; nothing more to send.
            }
            finally
            {
                context.Response.Close();
            }
        }
    }
}
