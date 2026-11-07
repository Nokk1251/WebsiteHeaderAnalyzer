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
              --warn-bg:#fdecc8;--warn:#8a4b00;--err-bg:#fde8e8;--err:#a31d1d;--code-bg:#eef1f7;--shadow:0 1px 3px rgba(20,30,60,.08),0 8px 24px rgba(20,30,60,.06)}
            @media (prefers-color-scheme:dark){:root{--bg:#10141c;--card:#1a2030;--text:#e6eaf3;--muted:#9aa7bf;--border:#2a3347;
              --accent:#7c93f5;--accent-text:#10141c;--ok-bg:#12331f;--ok:#6fdc9b;--miss-bg:#38280e;--miss:#f2b45c;
              --warn-bg:#3d2c06;--warn:#ffc666;--err-bg:#3a1717;--err:#ff9b9b;--code-bg:#232b3e;--shadow:none}}
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
            .hero .coverage{display:flex;align-items:center;gap:1.2rem;margin-bottom:1rem;flex-wrap:wrap}
            .big{font-size:2.8rem;font-weight:700;line-height:1;letter-spacing:-.02em}
            .big span{font-size:1.2rem;font-weight:500;color:var(--muted)}
            .cov-text{flex:1 1 14rem}
            .bar{height:.6rem;margin:.4rem 0;border-radius:999px;background:var(--code-bg);overflow:hidden}
            .bar span{display:block;height:100%;background:var(--accent)}
            a.export{color:var(--accent);font-weight:600}
            #f-all,#f-missing{position:absolute;opacity:0;pointer-events:none}
            .filters{display:flex;gap:.5rem;margin-bottom:.8rem}
            .filters label{padding:.3rem .9rem;border:1px solid var(--border);border-radius:999px;cursor:pointer;font-size:.9rem}
            #f-all:checked~.filters label[for=f-all],#f-missing:checked~.filters label[for=f-missing]{background:var(--accent);color:var(--accent-text);border-color:var(--accent)}
            #f-all:focus-visible~.filters label[for=f-all],#f-missing:focus-visible~.filters label[for=f-missing]{outline:2px solid var(--accent);outline-offset:2px}
            #f-missing:checked~.hdrs .group:not(.g-miss){display:none}
            .group h3{margin:1rem 0 .2rem;font-size:.85rem;text-transform:uppercase;letter-spacing:.05em;color:var(--muted)}
            .hdr{border-left:4px solid var(--border);padding-left:.8rem}
            .hdr.miss{border-left-color:var(--miss)}
            .hdr.warn{border-left-color:var(--warn)}
            .hdr.ok{border-left-color:var(--ok)}
            .badge.warn{background:var(--warn-bg);color:var(--warn)}
            .finding{margin:.4rem 0 0;font-size:.9rem}
            .finding.review{color:var(--warn)}
            .finding.rec{color:var(--miss)}
            code{cursor:copy;-webkit-user-select:all;user-select:all}
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
                body.Append(ExportLink(result));
                body.Append("</section>");
                body.Append(DisclaimerHtml());
                return Wrap(body.ToString());
            }

            AppendSummary(body, result, observations);
            AppendHeaders(body, observations);
            body.Append(DisclaimerHtml());
            return Wrap(body.ToString());
        }

        private static void AppendSummary(StringBuilder body, HttpAnalysisResult result, IReadOnlyList<HeaderObservation> observations)
        {
            int present = observations.Count(o => o.IsPresent);
            int total = observations.Count;
            int percent = total == 0 ? 0 : present * 100 / total;
            int attention = observations.Count(o => o.IsPresent && o.NeedsAttention);

            body.Append("<section class=\"card hero\"><h2>Summary</h2>");
            body.Append("<div class=\"coverage\">");
            body.Append($"<div class=\"big\">{present}<span> / {total}</span></div>");
            body.Append("<div class=\"cov-text\"><b>checked headers present</b>");
            body.Append($"<div class=\"bar\" role=\"img\" aria-label=\"{present} of {total} checked headers present\"><span style=\"width:{percent}%\"></span></div>");
            body.Append($"<div class=\"note\">{total - present} missing; {attention} present with values worth reviewing. This is a count, not a verdict.</div></div>");
            body.Append("</div><div class=\"grid\">");
            AppendStat(body, "Initial URL", result.InitialUri.ToString());
            AppendStat(body, "Final URL", result.FinalUri!.ToString());
            AppendStat(body, "Status code", $"{(int)result.StatusCode!} {result.StatusCode}");
            AppendStat(body, "Redirects followed", result.RedirectCount.ToString());
            AppendStat(body, "HTTPS (final URL)", ConsoleReport.DescribeHttps(result));
            body.Append("</div>");
            body.Append(ExportLink(result));
            body.Append("</section>");
        }

        private static string ExportLink(HttpAnalysisResult result) =>
            $"<p class=\"note\"><a class=\"export\" href=\"/?format=json&amp;url={Encode(Uri.EscapeDataString(result.InitialUri.ToString()))}\" download=\"report.json\">Download report as JSON</a></p>";

        // The filter is CSS-only (radio inputs), so the page needs no script and the strict Content-Security-Policy stays as is.
        private static void AppendHeaders(StringBuilder body, IReadOnlyList<HeaderObservation> observations)
        {
            body.Append("<section class=\"card\"><h2>Security headers (observations)</h2>");
            body.Append("<input type=\"radio\" name=\"filter\" id=\"f-all\" checked><input type=\"radio\" name=\"filter\" id=\"f-missing\">");
            body.Append("<div class=\"filters\"><label for=\"f-all\">All</label><label for=\"f-missing\">Missing only</label></div>");
            body.Append("<div class=\"hdrs\">");

            AppendGroup(body, "Missing", "miss", observations.Where(o => !o.IsPresent));
            AppendGroup(body, "Present, values worth reviewing", "warn", observations.Where(o => o.IsPresent && o.NeedsAttention));
            AppendGroup(body, "Present", "ok", observations.Where(o => o.IsPresent && !o.NeedsAttention));

            body.Append("</div></section>");
        }

        private static void AppendGroup(StringBuilder body, string title, string kind, IEnumerable<HeaderObservation> group)
        {
            List<HeaderObservation> items = group.ToList();
            if (items.Count == 0)
            {
                return;
            }

            body.Append($"<div class=\"group g-{kind}\"><h3>{Encode(title)} ({items.Count})</h3>");
            foreach (HeaderObservation observation in items)
            {
                string badgeText = kind switch { "miss" => "missing", "warn" => "review", _ => "present" };
                body.Append($"<div class=\"hdr {kind}\">");
                body.Append($"<span class=\"name\">{Encode(observation.Name)}</span><span class=\"badge {kind}\">{badgeText}</span>");
                if (observation.IsPresent)
                {
                    string value = observation.Values.Count == 0 ? "(empty value)" : string.Join(" | ", observation.Values);
                    // user-select:all makes one click select the complete value, so it can be copied without losing part of a long policy.
                    body.Append($"<code title=\"Click to select the whole value, then copy\">{Encode(value)}</code>");
                }
                foreach (HeaderFinding finding in observation.FindingDetails)
                {
                    body.Append($"<p class=\"finding{(finding.NeedsAttention ? " review" : "")}\"><b>Finding:</b> {Encode(finding.Text)}</p>");
                }
                if (observation.Recommendation is not null)
                {
                    body.Append($"<p class=\"finding rec\"><b>Recommendation:</b> {Encode(observation.Recommendation)}</p>");
                }
                body.Append($"<p class=\"note\"><b>Purpose:</b> {Encode(observation.Purpose)}</p>");
                body.Append($"<p class=\"note\"><b>Limitation:</b> {Encode(observation.Limitation)}</p>");
                body.Append("</div>");
            }
            body.Append("</div>");
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
