using Analyzer;

Console.WriteLine("Enter a URL:");
string? input = Console.ReadLine();

UrlValidator validator = new UrlValidator();
var result = validator.Validate(input);

if(result.IsValid)
{
    Console.WriteLine("URL is valid.");
}
else
{
    Console.WriteLine("URL is invalid.");
    Console.WriteLine($"Reason: {result.Message}");
}
