using System.Net;
using System.Text;

namespace Analyzer
{
    // Pure HTML rendering. Every dynamic value is HTML-encoded.
    public static class WebPage
    {
        private const string Style =
            "body{font-family:system-ui,sans-serif;max-width:50rem;margin:2rem auto;padding:0 1rem;line-height:1.5;color:#222}" +
            "input[type=text]{width:70%;padding:.4rem}button{padding:.4rem .8rem}" +
            ".error{color:#a00}.ok{color:#064}.missing{color:#a60}" +
            "table{border-collapse:collapse}td,th{text-align:left;padding:.2rem .8rem .2rem 0;vertical-align:top}" +
            ".header{margin:1rem 0}.small{color:#555;font-size:.9rem}code{word-break:break-all}";

        public static string RenderForm(string? message = null, string? currentUrl = null) =>
            Wrap(FormHtml(message, currentUrl) + DisclaimerHtml());

        public static string RenderReport(HttpAnalysisResult result, IReadOnlyList<HeaderObservation> observations)
        {
            StringBuilder body = new StringBuilder();
            body.Append(FormHtml(null, result.InitialUri.ToString()));

            body.Append("<h2>Report</h2><table>");
            AppendRow(body, "Initial URL", result.InitialUri.ToString());

            if (!result.Success || result.FinalUri is null || result.StatusCode is null)
            {
                body.Append("</table>");
                body.Append($"<p class=\"error\">Analysis failed: {Encode(result.ErrorMessage ?? "An unknown error occurred.")}</p>");
                body.Append(DisclaimerHtml());
                return Wrap(body.ToString());
            }

            AppendRow(body, "Final URL", result.FinalUri.ToString());
            AppendRow(body, "Status code", $"{(int)result.StatusCode} {result.StatusCode}");
            AppendRow(body, "Redirects followed", result.RedirectCount.ToString());
            AppendRow(body, "HTTPS (final URL)", ConsoleReport.DescribeHttps(result));
            body.Append("</table>");

            body.Append("<h2>Security headers (observations)</h2>");
            foreach (HeaderObservation observation in observations)
            {
                string state = observation.IsPresent
                    ? "<span class=\"ok\">present</span>"
                    : "<span class=\"missing\">missing</span>";

                body.Append("<div class=\"header\">");
                body.Append($"<strong>{Encode(observation.Name)}</strong> - {state}");
                if (observation.IsPresent)
                {
                    string value = observation.Values.Count == 0 ? "(empty value)" : string.Join(" | ", observation.Values);
                    body.Append($"<br><code>{Encode(value)}</code>");
                }
                body.Append($"<br><span class=\"small\">Purpose: {Encode(observation.Purpose)}</span>");
                body.Append($"<br><span class=\"small\">Limitation: {Encode(observation.Limitation)}</span>");
                body.Append("</div>");
            }

            body.Append(DisclaimerHtml());
            return Wrap(body.ToString());
        }

        private static string FormHtml(string? message, string? currentUrl)
        {
            string messageHtml = message is null ? "" : $"<p class=\"error\">{Encode(message)}</p>";
            return "<h1>Website Security Header Analyzer</h1>" +
                   "<form method=\"get\" action=\"/\">" +
                   $"<input type=\"text\" name=\"url\" placeholder=\"https://example.com\" value=\"{Encode(currentUrl ?? "")}\"> " +
                   "<button type=\"submit\">Analyze</button></form>" + messageHtml;
        }

        private static string DisclaimerHtml() => $"<p class=\"small\">{Encode(ConsoleReport.Disclaimer)}</p>";

        private static void AppendRow(StringBuilder body, string label, string value) =>
            body.Append($"<tr><th>{Encode(label)}</th><td>{Encode(value)}</td></tr>");

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private static string Wrap(string body) =>
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
            $"<title>Website Security Header Analyzer</title><style>{Style}</style></head><body>{body}</body></html>";
    }
}
