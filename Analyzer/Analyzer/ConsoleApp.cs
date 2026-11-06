namespace Analyzer
{
    // The console flow, kept out of Program.cs so input, output and the HTTP layer can be replaced in tests.
    public static class ConsoleApp
    {
        public static async Task<int> RunAsync(TextReader input, TextWriter output, TextWriter error,
            HttpAnalyzer httpAnalyzer, CancellationToken cancellationToken = default)
        {
            output.WriteLine("Enter a URL:");
            string? line = input.ReadLine();

            ValidationResult validation = new UrlValidator().Validate(line);

            if (!validation.IsValid)
            {
                output.WriteLine("URL is invalid.");
                output.WriteLine($"Reason: {validation.Message}");
                return 1;
            }

            try
            {
                HttpAnalysisResult result = await httpAnalyzer.Analyze(new Uri(validation.TrimmedUrl), cancellationToken);

                IReadOnlyList<HeaderObservation> observations = new SecurityHeaderAnalyzer().Analyze(result.Headers);
                output.WriteLine(ConsoleReport.Build(result, observations));

                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                // Last-resort boundary: report the problem instead of crashing with an unhandled exception.
                error.WriteLine("An unexpected error occurred during the analysis.");
                error.WriteLine(ex.GetType().Name);
                return 1;
            }
        }
    }
}
