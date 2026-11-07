namespace Analyzer.Tests
{
    public class HeaderEvaluatorTests
    {
        private static IReadOnlyList<string> Eval(string name, params string[] values) =>
            HeaderEvaluator.EvaluateValues(name, values);

        private static bool Has(IReadOnlyList<string> findings, string text) =>
            findings.Any(f => f.Contains(text, StringComparison.Ordinal));

        [Fact]
        public void Hsts_ShortMaxAge_IsDescribedAsShort()
        {
            var findings = Eval("Strict-Transport-Security", "max-age=300");

            Assert.True(Has(findings, "short lifetime"));
        }

        [Fact]
        public void Hsts_LongMaxAgeWithAllDirectives_HasNoWeaknessFindings()
        {
            var findings = Eval("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            Assert.False(Has(findings, "short"));
            Assert.True(Has(findings, "max-age is 31536000 seconds"));
            Assert.True(Has(findings, "includeSubDomains is set"));
            Assert.True(Has(findings, "preload is set"));
        }

        [Fact]
        public void Hsts_MissingDirectives_AreReported()
        {
            var findings = Eval("Strict-Transport-Security", "max-age=31536000");

            Assert.True(Has(findings, "includeSubDomains is not set"));
            Assert.True(Has(findings, "preload is not set"));
        }

        [Fact]
        public void Hsts_WithoutMaxAge_IsReported()
        {
            Assert.True(Has(Eval("Strict-Transport-Security", "includeSubDomains"), "No max-age"));
        }

        [Fact]
        public void Hsts_InvalidAndZeroMaxAge_AreReported()
        {
            Assert.True(Has(Eval("Strict-Transport-Security", "max-age=abc"), "not a valid number"));
            Assert.True(Has(Eval("Strict-Transport-Security", "max-age=0"), "max-age is 0"));
        }

        [Fact]
        public void Hsts_DirectiveNamesAreCaseInsensitive()
        {
            var findings = Eval("strict-transport-security", "MAX-AGE=31536000; INCLUDESUBDOMAINS");

            Assert.True(Has(findings, "includeSubDomains is set"));
        }

        [Fact]
        public void Csp_UnsafeInlineUnsafeEvalAndWildcard_AreReported()
        {
            var findings = Eval("Content-Security-Policy", "default-src 'self'; script-src 'unsafe-inline' 'unsafe-eval' *");

            Assert.True(Has(findings, "'unsafe-inline'"));
            Assert.True(Has(findings, "'unsafe-eval'"));
            Assert.True(Has(findings, "wildcard"));
            Assert.False(Has(findings, "No default-src"));
        }

        [Fact]
        public void Csp_WithoutDefaultSrc_IsReported()
        {
            var findings = Eval("Content-Security-Policy", "script-src 'self'");

            Assert.True(Has(findings, "No default-src"));
            Assert.False(Has(findings, "unsafe"));
        }

        [Fact]
        public void Csp_WildcardSubdomainSourceIsNotTreatedAsBareWildcard()
        {
            var findings = Eval("Content-Security-Policy", "default-src 'self' *.example.com");

            Assert.False(Has(findings, "wildcard"));
        }

        [Fact]
        public void ContentTypeOptions_NosniffAndOtherValues()
        {
            Assert.True(Has(Eval("X-Content-Type-Options", "nosniff"), "The value is nosniff"));
            Assert.True(Has(Eval("X-Content-Type-Options", "sniff"), "not nosniff"));
        }

        [Theory]
        [InlineData("DENY", "denied")]
        [InlineData("sameorigin", "same origin")]
        [InlineData("ALLOW-FROM https://a.example", "obsolete")]
        [InlineData("whatever", "not DENY or SAMEORIGIN")]
        public void FrameOptions_Values(string value, string expected)
        {
            Assert.True(Has(Eval("X-Frame-Options", value), expected));
        }

        [Theory]
        [InlineData("unsafe-url", "full page address")]
        [InlineData("no-referrer-when-downgrade", "no-referrer-when-downgrade")]
        [InlineData("strict-origin-when-cross-origin", "The policy is strict-origin-when-cross-origin.")]
        [InlineData("no-referrer", "The policy is no-referrer.")]
        [InlineData("bogus", "not a recognized")]
        [InlineData("no-referrer, unsafe-url", "unsafe-url")]
        public void ReferrerPolicy_Values(string value, string expected)
        {
            Assert.True(Has(Eval("Referrer-Policy", value), expected));
        }

        [Fact]
        public void PermissionsPolicy_WildcardFeatures_AreListed()
        {
            var findings = Eval("Permissions-Policy", "camera=*, microphone=(self), geolocation=(*)");

            Assert.True(Has(findings, "camera, geolocation"));
            Assert.False(Has(findings, "microphone"));
        }

        [Fact]
        public void PermissionsPolicy_WithoutWildcards_SaysSo()
        {
            Assert.True(Has(Eval("Permissions-Policy", "camera=()"), "No feature is allowed for all origins"));
        }

        [Theory]
        [InlineData("Strict-Transport-Security")]
        [InlineData("Content-Security-Policy")]
        [InlineData("X-Frame-Options")]
        public void EmptyValue_IsReported(string header)
        {
            Assert.True(Has(HeaderEvaluator.EvaluateValues(header, Array.Empty<string>()), "empty"));
            Assert.True(Has(Eval(header, "  "), "empty"));
        }

        [Theory]
        [InlineData("Strict-Transport-Security", "max-age=31536000; includeSubDomains")]
        [InlineData("Content-Security-Policy", "default-src 'self'")]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "DENY")]
        [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
        [InlineData("Permissions-Policy", "camera=(), microphone=(), geolocation=()")]
        public void Recommend_GivesExampleValueForEveryCheckedHeader(string header, string example)
        {
            string? recommendation = HeaderEvaluator.Recommend(header);

            Assert.NotNull(recommendation);
            Assert.Contains($"{header}: {example}", recommendation);
        }

        [Fact]
        public void Recommend_UnknownHeader_ReturnsNull()
        {
            Assert.Null(HeaderEvaluator.Recommend("X-Unknown"));
        }

        [Fact]
        public void Analyze_PresentHeaderHasFindingsAndNoRecommendation_MissingHeaderHasRecommendationOnly()
        {
            var headers = new Dictionary<string, string[]>
            {
                { "x-frame-options", new[] { "DENY" } }
            };

            var observations = new SecurityHeaderAnalyzer().Analyze(headers);
            var present = observations.Single(o => o.Name == "X-Frame-Options");
            var missing = observations.Single(o => o.Name == "Content-Security-Policy");

            Assert.NotEmpty(present.Findings);
            Assert.Null(present.Recommendation);
            Assert.Empty(missing.Findings);
            Assert.NotNull(missing.Recommendation);
        }

        [Fact]
        public void CoverageSummary_CountsPresentHeaders_WithoutVerdict()
        {
            var headers = new Dictionary<string, string[]>
            {
                { "X-Frame-Options", new[] { "DENY" } },
                { "Referrer-Policy", new[] { "no-referrer" } }
            };
            var observations = new SecurityHeaderAnalyzer().Analyze(headers);

            string summary = ConsoleReport.CoverageSummary(observations);

            Assert.Equal("Coverage: 2 of 6 checked headers are present.", summary);
        }

        [Fact]
        public void ConsoleReport_ShowsFindingsRecommendationsAndCoverage_AndEscapesControlCharacters()
        {
            var headers = new Dictionary<string, string[]>
            {
                { "X-Frame-Options", new[] { "bad\u001bvalue" } }
            };
            var result = new HttpAnalysisResult(true, new Uri("https://example.com"), new Uri("https://example.com"),
                System.Net.HttpStatusCode.OK, headers, false, 0, null);

            string report = ConsoleReport.Build(result, new SecurityHeaderAnalyzer().Analyze(headers));

            Assert.Contains("Finding: ", report);
            Assert.Contains("Recommendation: Consider adding this header, for example: Strict-Transport-Security", report);
            Assert.Contains("Coverage: 1 of 6 checked headers are present.", report);
            Assert.DoesNotContain('\u001b', report);
        }

        [Fact]
        public void WebReport_ShowsEncodedFindingsRecommendationsAndCoverage()
        {
            var headers = new Dictionary<string, string[]>
            {
                { "X-Frame-Options", new[] { "<script>x</script>" } }
            };
            var result = new HttpAnalysisResult(true, new Uri("https://example.com"), new Uri("https://example.com"),
                System.Net.HttpStatusCode.OK, headers, false, 0, null);

            string html = WebPage.RenderReport(result, new SecurityHeaderAnalyzer().Analyze(headers));

            Assert.Contains("<b>Finding:</b>", html);
            Assert.Contains("<b>Recommendation:</b>", html);
            Assert.Contains("Coverage: 1 of 6 checked headers are present.", html);
            Assert.DoesNotContain("<script>x</script>", html);
        }
    }
}
