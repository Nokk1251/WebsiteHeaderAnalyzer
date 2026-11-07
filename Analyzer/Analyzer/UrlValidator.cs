using System.Text.RegularExpressions;

namespace Analyzer
{
    public class UrlValidator
    {
        public const int MaxInputLength = 2048;

        // "scheme:" prefix that is not just "host:port" (for example "mailto:a@b" or "javascript:x").
        private static readonly Regex SchemePrefix =
            new Regex(@"^[A-Za-z][A-Za-z0-9+.\-]*:(?!\d+([/?#]|$))", RegexOptions.Compiled);

        public ValidationResult Validate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "", "The URL cannot be empty.");
            }

            var trimmedInput = input.Trim();

            if (trimmedInput.Length > MaxInputLength)
            {
                // The oversized input is not echoed back.
                return new ValidationResult(false, "", $"The URL is too long (maximum {MaxInputLength} characters).");
            }

            trimmedInput = AddDefaultScheme(trimmedInput);

            // On Unix-like systems, a rooted path such as "/account/login" is parsed as an implicit
            // file URI. Treat it as a non-absolute URL so behavior matches across platforms.
            if (!Uri.TryCreate(trimmedInput, UriKind.Absolute, out Uri? uri)
                || (uri.IsFile && !trimmedInput.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult(false, trimmedInput, "The URL must be an absolute HTTP or HTTPS URL.");
            }

            if(!(uri.Scheme=="http" || uri.Scheme=="https"))
            {
                return new ValidationResult(false, trimmedInput, "Only HTTP and HTTPS URLs are allowed.");
            }

            if (uri.UserInfo.Length > 0 || HasUserInfo(trimmedInput))
            {
                // The input is not echoed back so that credentials do not end up in reports or the form.
                return new ValidationResult(false, "", "URLs with embedded credentials (user:password@host) are not allowed.");
            }

            return new ValidationResult(true, trimmedInput, "The URL is valid.");

        }

        // Input such as "example.com/path" is treated as HTTPS. Anything that starts like another scheme
        // or like a rooted path is left untouched so it is still rejected by the checks below.
        private static string AddDefaultScheme(string input)
        {
            if (input.Contains("://", StringComparison.Ordinal)
                || input.StartsWith('/')
                || input.StartsWith('\\')
                || SchemePrefix.IsMatch(input))
            {
                return input;
            }

            return "https://" + input;
        }

        // Authority is everything between "://" and the first '/', '?' or '#'; userinfo ends at its last '@'.
        private static bool HasUserInfo(string url)
        {
            int start = url.IndexOf("://", StringComparison.Ordinal);
            if (start < 0)
            {
                return false;
            }

            string rest = url[(start + 3)..];
            int end = rest.IndexOfAny(new[] { '/', '?', '#' });
            string authority = end < 0 ? rest : rest[..end];
            return authority.Contains('@', StringComparison.Ordinal);
        }
    }
}
