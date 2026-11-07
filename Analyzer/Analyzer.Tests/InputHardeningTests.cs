namespace Analyzer.Tests
{
    public class InputHardeningTests
    {
        private static ValidationResult Validate(string input) => new UrlValidator().Validate(input);

        [Theory]
        [InlineData("example.com", "https://example.com")]
        [InlineData(" example.com/path?q=1 ", "https://example.com/path?q=1")]
        [InlineData("localhost:8080", "https://localhost:8080")]
        [InlineData("example.com:443/x", "https://example.com:443/x")]
        public void InputWithoutScheme_IsNormalizedToHttps(string input, string expected)
        {
            ValidationResult result = Validate(input);

            Assert.True(result.IsValid);
            Assert.Equal(expected, result.TrimmedUrl);
        }

        [Theory]
        [InlineData("ftp://example.com")]
        [InlineData("mailto:user@example.com")]
        [InlineData("javascript:alert(1)")]
        [InlineData("file:///etc/passwd")]
        public void OtherSchemes_AreStillRejected(string input)
        {
            ValidationResult result = Validate(input);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void RootedPath_IsNotTurnedIntoHttpsUrl()
        {
            ValidationResult result = Validate("/account/login");

            Assert.False(result.IsValid);
            Assert.Equal("/account/login", result.TrimmedUrl);
        }

        [Theory]
        [InlineData("https://user:secret@example.com/")]
        [InlineData("http://user@example.com")]
        [InlineData("https://:secret@example.com")]
        public void EmbeddedCredentials_AreRejectedWithoutEchoingThem(string input)
        {
            ValidationResult result = Validate(input);

            Assert.False(result.IsValid);
            Assert.Equal("", result.TrimmedUrl);
            Assert.Contains("credentials", result.Message);
            Assert.DoesNotContain("secret", result.Message);
        }

        [Fact]
        public void AtSignInPath_IsNotTreatedAsCredentials()
        {
            ValidationResult result = Validate("https://example.com/@user");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void InputAtLengthLimit_IsAccepted()
        {
            string url = "https://example.com/" + new string('a', UrlValidator.MaxInputLength - "https://example.com/".Length);

            Assert.True(Validate(url).IsValid);
        }

        [Fact]
        public void InputOverLengthLimit_IsRejectedWithoutEchoing()
        {
            string url = "https://example.com/" + new string('a', UrlValidator.MaxInputLength);

            ValidationResult result = Validate(url);

            Assert.False(result.IsValid);
            Assert.Equal("", result.TrimmedUrl);
            Assert.Contains("too long", result.Message);
        }

        [Fact]
        public void Sanitize_ReplacesControlCharactersWithVisibleEscapes()
        {
            string result = ConsoleText.Sanitize("a\u001b[31mred\r\nb\u0007\u009b");

            Assert.Equal("a\\x1B[31mred\\x0D\\x0Ab\\x07\\x9B", result);
        }

        [Fact]
        public void Sanitize_LeavesOrdinaryAndNonAsciiTextUnchanged()
        {
            Assert.Equal("max-age=31536000; ключ ✓", ConsoleText.Sanitize("max-age=31536000; ключ ✓"));
            Assert.Equal("", ConsoleText.Sanitize(null));
        }

        [Fact]
        public void Report_EscapesControlCharactersInHeaderValuesAndErrors()
        {
            var headers = new Dictionary<string, string[]>
            {
                { "Content-Security-Policy", new[] { "default-src 'self'\u001b[2J\nFake: line" } }
            };
            var ok = new HttpAnalysisResult(true, new Uri("https://example.com"), new Uri("https://example.com"),
                System.Net.HttpStatusCode.OK, headers, false, 0, null);

            string report = ConsoleReport.Build(ok, new SecurityHeaderAnalyzer().Analyze(ok.Headers));

            Assert.DoesNotContain('\u001b', report);
            Assert.Contains("\\x1B[2J\\x0AFake: line", report);

            var failed = new HttpAnalysisResult(false, new Uri("https://example.com"), null, null,
                new Dictionary<string, string[]>(), false, 0, "bad\u001bmessage");

            Assert.DoesNotContain('\u001b', ConsoleReport.Build(failed, Array.Empty<HeaderObservation>()));
        }
    }
}
