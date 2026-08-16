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
    }
}
