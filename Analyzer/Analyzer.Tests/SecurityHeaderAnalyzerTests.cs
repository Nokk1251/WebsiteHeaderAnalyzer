namespace Analyzer.Tests
{
    public class SecurityHeaderAnalyzerTests
    {
        private static readonly SecurityHeaderAnalyzer Analyzer = new SecurityHeaderAnalyzer();

        private static HeaderObservation Get(IReadOnlyList<HeaderObservation> observations, string name) =>
            Assert.Single(observations, o => o.Name == name);

        [Fact]
        public void Definitions_EveryCheckedHeaderHasNonEmptyPurposeAndLimitation()
        {
            Assert.Equal(6, SecurityHeaderAnalyzer.Definitions.Count);
            Assert.All(SecurityHeaderAnalyzer.Definitions, d =>
            {
                Assert.False(string.IsNullOrWhiteSpace(d.Name));
                Assert.False(string.IsNullOrWhiteSpace(d.Purpose));
                Assert.False(string.IsNullOrWhiteSpace(d.Limitation));
            });
        }

        [Fact]
        public void Analyze_Observations_CarryPurposeAndLimitationWhetherPresentOrMissing()
        {
            var headers = new Dictionary<string, string[]> { ["X-Frame-Options"] = new[] { "DENY" } };

            var result = Analyzer.Analyze(headers);

            Assert.All(result, o =>
            {
                Assert.False(string.IsNullOrWhiteSpace(o.Purpose));
                Assert.False(string.IsNullOrWhiteSpace(o.Limitation));
            });
        }

        [Fact]
        public void Analyze_AllHeadersPresent_ReportsEachAsPresentWithValue()
        {
            var headers = SecurityHeaderAnalyzer.CheckedHeaders
                .ToDictionary(n => n, n => new[] { n + "-value" });

            var result = Analyzer.Analyze(headers);

            Assert.Equal(6, result.Count);
            Assert.All(result, o =>
            {
                Assert.True(o.IsPresent);
                Assert.Equal(new[] { o.Name + "-value" }, o.Values);
            });
        }

        [Fact]
        public void Analyze_AllHeadersMissing_ReportsEachAsMissing()
        {
            var headers = new Dictionary<string, string[]> { ["Server"] = new[] { "nginx" } };

            var result = Analyzer.Analyze(headers);

            Assert.Equal(6, result.Count);
            Assert.All(result, o =>
            {
                Assert.False(o.IsPresent);
                Assert.NotNull(o.Values);
                Assert.Empty(o.Values);
            });
        }

        [Fact]
        public void Analyze_MixedHeaders_ReportsPresentAndMissingCorrectly()
        {
            var headers = new Dictionary<string, string[]>
            {
                ["Strict-Transport-Security"] = new[] { "max-age=31536000" },
                ["X-Frame-Options"] = new[] { "DENY" }
            };

            var result = Analyzer.Analyze(headers);

            Assert.True(Get(result, "Strict-Transport-Security").IsPresent);
            Assert.Equal(new[] { "max-age=31536000" }, Get(result, "Strict-Transport-Security").Values);
            Assert.True(Get(result, "X-Frame-Options").IsPresent);
            Assert.False(Get(result, "Content-Security-Policy").IsPresent);
            Assert.False(Get(result, "Referrer-Policy").IsPresent);
            Assert.False(Get(result, "Permissions-Policy").IsPresent);
            Assert.False(Get(result, "X-Content-Type-Options").IsPresent);
        }

        [Fact]
        public void Analyze_EmptyCollection_ReportsAllAsMissingWithoutThrowing()
        {
            var result = Analyzer.Analyze(new Dictionary<string, string[]>());

            Assert.Equal(SecurityHeaderAnalyzer.CheckedHeaders, result.Select(o => o.Name));
            Assert.All(result, o =>
            {
                Assert.False(o.IsPresent);
                Assert.Empty(o.Values);
            });
        }

        [Fact]
        public void Analyze_MultiValueHeader_PreservesAllValues()
        {
            var headers = new Dictionary<string, string[]>
            {
                ["Content-Security-Policy"] = new[] { "default-src 'self'", "img-src *" }
            };

            var result = Analyzer.Analyze(headers);

            Assert.Equal(new[] { "default-src 'self'", "img-src *" }, Get(result, "Content-Security-Policy").Values);
        }

        [Fact]
        public void Analyze_DifferentCasing_MatchesCaseInsensitivelyAndKeepsCanonicalName()
        {
            var headers = new Dictionary<string, string[]>
            {
                ["strict-transport-security"] = new[] { "max-age=1" },
                ["X-FRAME-OPTIONS"] = new[] { "SAMEORIGIN" }
            };

            var result = Analyzer.Analyze(headers);

            var hsts = Get(result, "Strict-Transport-Security");
            Assert.True(hsts.IsPresent);
            Assert.Equal(new[] { "max-age=1" }, hsts.Values);
            Assert.True(Get(result, "X-Frame-Options").IsPresent);
        }
    }
}
