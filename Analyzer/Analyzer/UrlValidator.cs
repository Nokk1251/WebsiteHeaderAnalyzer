namespace Analyzer
{
    public class UrlValidator
    {
        public ValidationResult Validate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "", "The URL cannot be empty.");
            }

            var trimmedInput = input.Trim();
            if (!Uri.TryCreate(trimmedInput, UriKind.Absolute, out Uri? uri))
            {
                return new ValidationResult(false, trimmedInput, "The URL must be an absolute HTTP or HTTPS URL.");
            }

            if(!(uri.Scheme=="http" || uri.Scheme=="https"))
            {
                return new ValidationResult(false, trimmedInput, "Only HTTP and HTTPS URLs are allowed.");
            }
            return new ValidationResult(true, trimmedInput, "The URL is valid.");

        }


    }
}
