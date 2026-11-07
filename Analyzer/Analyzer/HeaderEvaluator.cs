using System.Globalization;

namespace Analyzer
{
    // Produces neutral, descriptive findings about header values and example values for missing headers.
    // Findings describe what was observed; they never state that a site is secure or insecure.
    public static class HeaderEvaluator
    {
        // 180 days; shorter HSTS lifetimes are called out as short.
        public const long ShortHstsMaxAgeSeconds = 15552000;

        private static readonly IReadOnlyDictionary<string, string> ExampleValues =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains",
                ["Content-Security-Policy"] = "default-src 'self'",
                ["X-Content-Type-Options"] = "nosniff",
                ["X-Frame-Options"] = "DENY",
                ["Referrer-Policy"] = "strict-origin-when-cross-origin",
                ["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()"
            };

        public static IReadOnlyList<string> EvaluateValues(string headerName, IReadOnlyList<string> values)
        {
            if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
            {
                return new[] { "The header is present but its value is empty." };
            }

            return headerName.ToLowerInvariant() switch
            {
                "strict-transport-security" => EvaluateHsts(values),
                "content-security-policy" => EvaluateCsp(values),
                "x-content-type-options" => EvaluateContentTypeOptions(values),
                "x-frame-options" => EvaluateFrameOptions(values),
                "referrer-policy" => EvaluateReferrerPolicy(values),
                "permissions-policy" => EvaluatePermissionsPolicy(values),
                _ => Array.Empty<string>()
            };
        }

        public static string? Recommend(string headerName)
        {
            return ExampleValues.TryGetValue(headerName, out string? example)
                ? $"Consider adding this header, for example: {headerName}: {example} (adjust the value to the needs of the site)."
                : null;
        }

        private static IReadOnlyList<string> EvaluateHsts(IReadOnlyList<string> values)
        {
            List<string> findings = new List<string>();
            string[] directives = SplitDirectives(values, ';');

            string? maxAge = directives
                .Select(d => d.Split('=', 2))
                .Where(p => p[0].Trim().Equals("max-age", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Length > 1 ? p[1].Trim().Trim('"') : "")
                .FirstOrDefault();

            if (maxAge is null)
            {
                findings.Add("No max-age directive was found.");
            }
            else if (!long.TryParse(maxAge, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds))
            {
                findings.Add($"The max-age value \"{maxAge}\" is not a valid number of seconds.");
            }
            else if (seconds == 0)
            {
                findings.Add("max-age is 0, which tells browsers to forget the HSTS policy.");
            }
            else if (seconds < ShortHstsMaxAgeSeconds)
            {
                findings.Add($"max-age is {seconds} seconds (about {seconds / 86400} days), which is a short lifetime.");
            }
            else
            {
                findings.Add($"max-age is {seconds} seconds (about {seconds / 86400} days).");
            }

            findings.Add(HasDirective(directives, "includeSubDomains")
                ? "includeSubDomains is set."
                : "includeSubDomains is not set, so subdomains are not covered by this policy.");
            findings.Add(HasDirective(directives, "preload")
                ? "preload is set."
                : "preload is not set.");

            return findings;
        }

        private static IReadOnlyList<string> EvaluateCsp(IReadOnlyList<string> values)
        {
            List<string> findings = new List<string>();
            string[] directives = SplitDirectives(values, ';');
            string[] tokens = directives.SelectMany(d => d.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();

            if (tokens.Contains("'unsafe-inline'", StringComparer.OrdinalIgnoreCase))
            {
                findings.Add("'unsafe-inline' is present, which allows inline scripts or styles.");
            }

            if (tokens.Contains("'unsafe-eval'", StringComparer.OrdinalIgnoreCase))
            {
                findings.Add("'unsafe-eval' is present, which allows dynamic code evaluation.");
            }

            if (tokens.Contains("*"))
            {
                findings.Add("A wildcard (*) source is present, which allows content from any host.");
            }

            bool hasDefaultSrc = directives.Any(d =>
                d.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                    ?.Equals("default-src", StringComparison.OrdinalIgnoreCase) == true);
            if (!hasDefaultSrc)
            {
                findings.Add("No default-src directive was found, so resource types without their own directive are not restricted by it.");
            }

            return findings;
        }

        private static IReadOnlyList<string> EvaluateContentTypeOptions(IReadOnlyList<string> values)
        {
            bool nosniff = values.SelectMany(v => v.Split(',')).Any(v => v.Trim().Equals("nosniff", StringComparison.OrdinalIgnoreCase));
            return new[]
            {
                nosniff
                    ? "The value is nosniff."
                    : $"The value \"{string.Join(", ", values)}\" is not nosniff, which is the only value browsers act on."
            };
        }

        private static IReadOnlyList<string> EvaluateFrameOptions(IReadOnlyList<string> values)
        {
            string value = string.Join(", ", values).Trim();

            if (value.Equals("DENY", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Framing is denied (DENY)." };
            }

            if (value.Equals("SAMEORIGIN", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Framing is limited to the same origin (SAMEORIGIN)." };
            }

            if (value.StartsWith("ALLOW-FROM", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "ALLOW-FROM is obsolete and ignored by modern browsers." };
            }

            return new[] { $"The value \"{value}\" is not DENY or SAMEORIGIN, so browsers may ignore it." };
        }

        private static IReadOnlyList<string> EvaluateReferrerPolicy(IReadOnlyList<string> values)
        {
            // Browsers use the last recognized token of a comma-separated list; the last one is described here.
            string[] policies = SplitDirectives(values, ',');
            string effective = policies.Length > 0 ? policies[^1] : "";

            return effective.ToLowerInvariant() switch
            {
                "unsafe-url" => new[] { "The policy is unsafe-url, which sends the full page address (including the query) to every destination." },
                "no-referrer-when-downgrade" => new[] { "The policy is no-referrer-when-downgrade, which sends the full page address to destinations that are not less secure." },
                "origin-when-cross-origin" => new[] { "The policy is origin-when-cross-origin, which sends the full address to the same origin and only the origin elsewhere." },
                "no-referrer" or "same-origin" or "origin" or "strict-origin" or "strict-origin-when-cross-origin"
                    => new[] { $"The policy is {effective.ToLowerInvariant()}." },
                _ => new[] { $"The policy \"{effective}\" is not a recognized Referrer-Policy value." }
            };
        }

        private static IReadOnlyList<string> EvaluatePermissionsPolicy(IReadOnlyList<string> values)
        {
            List<string> findings = new List<string>();
            List<string> allowAll = new List<string>();

            foreach (string directive in SplitDirectives(values, ','))
            {
                string[] parts = directive.Split('=', 2);
                if (parts.Length == 2 && parts[1].Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries).Contains("*"))
                {
                    allowAll.Add(parts[0].Trim());
                }
            }

            if (allowAll.Count > 0)
            {
                findings.Add($"These features are allowed for all origins (*): {string.Join(", ", allowAll)}.");
            }
            else
            {
                findings.Add("No feature is allowed for all origins (*).");
            }

            return findings;
        }

        private static string[] SplitDirectives(IReadOnlyList<string> values, char separator) =>
            values
                .SelectMany(v => v.Split(separator))
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .ToArray();

        private static bool HasDirective(string[] directives, string name) =>
            directives.Any(d => d.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
