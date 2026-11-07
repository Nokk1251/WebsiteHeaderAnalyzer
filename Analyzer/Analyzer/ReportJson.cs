using System.Text.Json;

namespace Analyzer
{
    // Machine-readable form of a report for the web export. Contains the same observations as the HTML report.
    public static class ReportJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        public static string Build(HttpAnalysisResult result, IReadOnlyList<HeaderObservation> observations)
        {
            var report = new
            {
                success = result.Success,
                initialUrl = result.InitialUri.ToString(),
                finalUrl = result.FinalUri?.ToString(),
                statusCode = result.StatusCode is null ? (int?)null : (int)result.StatusCode,
                redirectsFollowed = result.RedirectCount,
                usesHttps = result.UsesHttps,
                httpsDowngraded = result.HttpsDowngraded,
                error = result.ErrorMessage,
                coverage = new
                {
                    present = observations.Count(o => o.IsPresent),
                    checkedHeaders = observations.Count
                },
                headers = observations.Select(o => new
                {
                    name = o.Name,
                    present = o.IsPresent,
                    values = o.Values,
                    findings = o.FindingDetails.Select(f => new { text = f.Text, needsAttention = f.NeedsAttention }),
                    recommendation = o.Recommendation,
                    purpose = o.Purpose,
                    limitation = o.Limitation
                }),
                note = ConsoleReport.Disclaimer
            };

            return JsonSerializer.Serialize(report, Options);
        }
    }
}
