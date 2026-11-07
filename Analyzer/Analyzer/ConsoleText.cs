using System.Text;

namespace Analyzer
{
    // Remote data (header values, URLs, error text) must not be able to inject terminal escape sequences
    // or fake extra report lines, so control characters are replaced with visible escapes before printing.
    public static class ConsoleText
    {
        public static string Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? "";
            }

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsControl(c))
                {
                    builder.Append("\\x").Append(((int)c).ToString("X2"));
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
