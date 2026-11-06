using System.Net;
using System.Text;

namespace Analyzer
{
    // Pure HTML rendering. Every dynamic value is HTML-encoded. No scripts and no external resources.
    public static class WebPage
    {
        private const string Style = """
            :root{color-scheme:light dark;--bg:#f4f6fb;--card:#fff;--text:#1b2433;--muted:#5d6b82;--border:#e1e6ef;
              --accent:#3b5bdb;--accent-text:#fff;--ok-bg:#e3f6ea;--ok:#12703a;--miss-bg:#fff1dc;--miss:#9a5b00;
              --err-bg:#fde8e8;--err:#a31d1d;--code-bg:#eef1f7;--shadow:0 1px 3px rgba(20,30,60,.08),0 8px 24px rgba(20,30,60,.06)}
            @media (prefers-color-scheme:dark){:root{--bg:#10141c;--card:#1a2030;--text:#e6eaf3;--muted:#9aa7bf;--border:#2a3347;
              --accent:#7c93f5;--accent-text:#10141c;--ok-bg:#12331f;--ok:#6fdc9b;--miss-bg:#38280e;--miss:#f2b45c;
              --err-bg:#3a1717;--err:#ff9b9b;--code-bg:#232b3e;--shadow:none}}
            *{box-sizing:border-box}
            body{margin:0;background:var(--bg);color:var(--text);font:16px/1.55 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
            .wrap{max-width:52rem;margin:0 auto;padding:2rem 1rem 3rem}
            header.top{margin-bottom:1.5rem}
            header.top h1{margin:0;font-size:1.7rem;letter-spacing:-.01em}
            header.top p{margin:.25rem 0 0;color:var(--muted)}
            .card{background:var(--card);border:1px solid var(--border);border-radius:14px;padding:1.25rem;margin-bottom:1.25rem;box-shadow:var(--shadow)}
            .card h2{margin:0 0 .9rem;font-size:1.1rem}
            form.lookup{display:flex;gap:.6rem;flex-wrap:wrap}
            form.lookup input{flex:1 1 16rem;min-width:0;padding:.7rem .9rem;font:inherit;color:var(--text);background:var(--bg);
              border:1px solid var(--border);border-radius:10px}
            form.lookup input:focus{outline:2px solid var(--accent);outline-offset:1px}
            form.lookup button{padding:.7rem 1.4rem;font:inherit;font-weight:600;color:var(--accent-text);background:var(--accent);
              border:0;border-radius:10px;cursor:pointer}
            form.lookup button:hover{filter:brightness(1.08)}
            .alert{margin:.9rem 0 0;padding:.7rem .9rem;border-radius:10px;background:var(--err-bg);color:var(--err)}
            .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(13rem,1fr));gap:.8rem}
            .stat{padding:.7rem .9rem;border:1px solid var(--border);border-radius:10px;background:var(--bg)}
            .stat .label{font-size:.78rem;text-transform:uppercase;letter-spacing:.05em;color:var(--muted)}
            .stat .value{font-weight:600;word-break:break-all}
            .hdr{padding:.9rem 0;border-top:1px solid var(--border)}
            .hdr:first-of-type{border-top:0;padding-top:0}
            .hdr .name{font-weight:600;margin-right:.5rem;word-break:break-word}
            .badge{display:inline-block;padding:.1rem .6rem;border-radius:999px;font-size:.8rem;font-weight:600}
            .badge.ok{background:var(--ok-bg);color:var(--ok)}
            .badge.miss{background:var(--miss-bg);color:var(--miss)}
            code{display:block;margin:.5rem 0 0;padding:.5rem .7rem;border-radius:8px;background:var(--code-bg);
              font:.85rem/1.4 ui-monospace,SFMono-Regular,Consolas,monospace;word-break:break-all;white-space:pre-wrap}
            .note{margin:.5rem 0 0;font-size:.9rem;color:var(--muted)}
            .note b{color:var(--text);font-weight:600}
            footer.disclaimer{padding:.9rem 1rem;border-left:4px solid var(--accent);border-radius:6px;background:var(--card);
              color:var(--muted);font-size:.9rem}
            """;

        public static string RenderForm(string? message = null, string? currentUrl = null) =>
            Wrap(Header() + FormCard(message, currentUrl) + DisclaimerHtml());

        public static string RenderReport(HttpAnalysisResult result, IReadOnlyList<HeaderObservation> observations)
        {
            StringBuilder body = new StringBuilder();
            body.Append(Header());
            body.Append(FormCard(null, result.InitialUri.ToString()));

            if (!result.Success || result.FinalUri is null || result.StatusCode is null)
            {
                body.Append("<section class=\"card\"><h2>Report</h2><div class=\"grid\">");
                AppendStat(body, "Initial URL", result.InitialUri.ToString());
                body.Append("</div>");
                body.Append($"<p class=\"alert\">Analysis failed: {Encode(result.ErrorMessage ?? "An unknown error occurred.")}</p>");
                body.Append("</section>");
                body.Append(DisclaimerHtml());
                return Wrap(body.ToString());
            }

            body.Append("<section class=\"card\"><h2>Report</h2><div class=\"grid\">");
            AppendStat(body, "Initial URL", result.InitialUri.ToString());
            AppendStat(body, "Final URL", result.FinalUri.ToString());
            AppendStat(body, "Status code", $"{(int)result.StatusCode} {result.StatusCode}");
            AppendStat(body, "Redirects followed", result.RedirectCount.ToString());
            AppendStat(body, "HTTPS (final URL)", ConsoleReport.DescribeHttps(result));
            body.Append("</div></section>");

            body.Append("<section class=\"card\"><h2>Security headers (observations)</h2>");
            foreach (HeaderObservation observation in observations)
            {
                string badge = observation.IsPresent
                    ? "<span class=\"badge ok\">present</span>"
                    : "<span class=\"badge miss\">missing</span>";

                body.Append("<div class=\"hdr\">");
                body.Append($"<span class=\"name\">{Encode(observation.Name)}</span>{badge}");
                if (observation.IsPresent)
                {
                    string value = observation.Values.Count == 0 ? "(empty value)" : string.Join(" | ", observation.Values);
                    body.Append($"<code>{Encode(value)}</code>");
                }
                body.Append($"<p class=\"note\"><b>Purpose:</b> {Encode(observation.Purpose)}</p>");
                body.Append($"<p class=\"note\"><b>Limitation:</b> {Encode(observation.Limitation)}</p>");
                body.Append("</div>");
            }
            body.Append("</section>");

            body.Append(DisclaimerHtml());
            return Wrap(body.ToString());
        }

        private static string Header() =>
            "<header class=\"top\"><h1>Website Security Header Analyzer</h1>" +
            "<p>A passive check of HTTPS usage and selected HTTP security headers.</p></header>";

        private static string FormCard(string? message, string? currentUrl)
        {
            string messageHtml = message is null ? "" : $"<p class=\"alert\">{Encode(message)}</p>";
            return "<section class=\"card\"><form class=\"lookup\" method=\"get\" action=\"/\">" +
                   $"<input type=\"text\" name=\"url\" placeholder=\"https://example.com\" aria-label=\"Website URL\" value=\"{Encode(currentUrl ?? "")}\">" +
                   "<button type=\"submit\">Analyze</button></form>" + messageHtml + "</section>";
        }

        private static string DisclaimerHtml() =>
            $"<footer class=\"disclaimer\">{Encode(ConsoleReport.Disclaimer)}</footer>";

        private static void AppendStat(StringBuilder body, string label, string value) =>
            body.Append($"<div class=\"stat\"><div class=\"label\">{Encode(label)}</div><div class=\"value\">{Encode(value)}</div></div>");

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private static string Wrap(string body) =>
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
            $"<title>Website Security Header Analyzer</title><style>{Style}</style></head><body><div class=\"wrap\">{body}</div></body></html>";
    }
}
