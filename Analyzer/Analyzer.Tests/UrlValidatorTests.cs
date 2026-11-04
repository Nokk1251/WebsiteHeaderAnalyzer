using System.Net;

namespace Analyzer.Tests
{
    public class UrlValidatorTests
    {
        [Fact]
        public void WhitespaceOnlyInput_ReturnsInvalidEmptyResult()
        {
            var input = "    ";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.False(result.IsValid);
            Assert.Equal("", result.TrimmedUrl);
            Assert.Equal("The URL cannot be empty.", result.Message);
        }

        [Fact]
        public void ValidHttpsUrl_WithSpaces_ReturnsValidTrimmedResult()
        {
            var input = " https://example.com/products ";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.True(result.IsValid);
            Assert.Equal("https://example.com/products", result.TrimmedUrl);
            Assert.Equal("The URL is valid.", result.Message);
        }

        [Fact]
        public void RelativeUrl_ReturnsInvalidAbsoluteUrlResult()
        {
            var input = " /account/login ";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.False(result.IsValid);
            Assert.Equal("/account/login", result.TrimmedUrl);
            Assert.Equal("The URL must be an absolute HTTP or HTTPS URL.", result.Message);
        }

        [Fact]
        public void UnsupportedScheme_ReturnsInvalidResult()
        {
            var input = " ftp://files.example.com/archive.zip ";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.False(result.IsValid);
            Assert.Equal("ftp://files.example.com/archive.zip", result.TrimmedUrl);
            Assert.Equal("Only HTTP and HTTPS URLs are allowed.", result.Message);
        }

        [Fact]
        public void IncompleteUrl_ReturnsInvalidResult()
        {
            var input = " https:// ";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.False(result.IsValid);
            Assert.Equal("https://", result.TrimmedUrl);
            Assert.Equal("The URL must be an absolute HTTP or HTTPS URL.", result.Message);
        }

        [Fact]
        public void ValidHttpUrl_ReturnsValidResult()
        {
            var input = "http://example.com";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.True(result.IsValid);
            Assert.Equal("http://example.com", result.TrimmedUrl);
            Assert.Equal("The URL is valid.", result.Message);
        }

        [Fact]
        public void UppercaseHttpsScheme_ReturnsValidResult()
        {
            var input = "HTTPS://EXAMPLE.COM";
            var validator = new UrlValidator();

            var result = validator.Validate(input);

            Assert.True(result.IsValid);
            Assert.Equal("HTTPS://EXAMPLE.COM", result.TrimmedUrl);
            Assert.Equal("The URL is valid.", result.Message);
        }

        [Fact]
        public void HttpAnalysisResult_StoresProvidedValues()
        {
            bool success = true;
            var initialUri = new Uri("https://example.com");
            var finalUri = new Uri("https://example.com");
            HttpStatusCode statusCode = HttpStatusCode.OK;
            var headers = new Dictionary<string, string[]>
            {
                {"Server", new[]{ "example" } }
            };
            bool redirected = false;
            int redirectCount = 0;
            string? errorMessage = null;

            var analysis = new HttpAnalysisResult(success, initialUri, finalUri, statusCode, headers, redirected, redirectCount, errorMessage);

            Assert.Equal(success, analysis.Success);
            Assert.Equal(initialUri, analysis.InitialUri);
            Assert.Equal(finalUri, analysis.FinalUri);
            Assert.Equal(statusCode, analysis.StatusCode);
            Assert.Equal(headers, analysis.Headers);
            Assert.Equal(redirected, analysis.Redirected);
            Assert.Equal(redirectCount, analysis.RedirectCount);
            Assert.Equal(errorMessage, analysis.ErrorMessage);
        }
    }
}
