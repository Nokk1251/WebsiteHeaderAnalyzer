using Analyzer;

Console.WriteLine("Enter a URL:");
string? input = Console.ReadLine();

UrlValidator validator = new UrlValidator();
ValidationResult validation = validator.Validate(input);

if (!validation.IsValid)
{
    Console.WriteLine("URL is invalid.");
    Console.WriteLine($"Reason: {validation.Message}");
    return 1;
}

try
{
    HttpAnalyzer httpAnalyzer = new HttpAnalyzer();
    HttpAnalysisResult result = await httpAnalyzer.Analyze(new Uri(validation.TrimmedUrl));

    IReadOnlyList<HeaderObservation> observations = new SecurityHeaderAnalyzer().Analyze(result.Headers);
    Console.WriteLine(ConsoleReport.Build(result, observations));

    return result.Success ? 0 : 1;
}
catch (Exception ex)
{
    // Last-resort boundary: report the problem instead of crashing with an unhandled exception.
    Console.Error.WriteLine("An unexpected error occurred during the analysis.");
    Console.Error.WriteLine(ex.GetType().Name);
    return 1;
}
