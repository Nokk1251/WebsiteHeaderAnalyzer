# CLAUDE.md

This file gives guidance to Claude Code when working in this repository.

## Project

Website Security Header Analyzer is an educational C# console tool. It does passive checks of HTTPS usage and selected HTTP security headers. The target user is a beginner web developer or cybersecurity student. It is not a professional security audit tool. See `docs/project-charter.md` for the scope and MVP.

MVP flow: accept one URL → validate it → send an HTTP request → detect the final URL after redirects → check HTTPS → check selected security headers → print a readable console report. Errors must be handled without crashing.

## Layout

- `Analyzer/Analyzer.slnx`: solution file
- `Analyzer/Analyzer/`: console app (`net10.0`, nullable and implicit usings enabled)
  - `Program.cs`: entry point (top-level statements)
  - `UrlValidator.cs`, `ValidationResult.cs`: URL validation
  - `HttpAnalyzer.cs`, `HttpAnalysisResult.cs`: HTTP fetching (in progress)
- `Analyzer/Analyzer.Tests/`: xUnit tests (`net10.0`)
- `docs/`: project documentation

## Commands

Run these from the `Analyzer/` directory:

```
dotnet build
dotnet test
dotnet run --project Analyzer
```

## Conventions

- Namespace is `Analyzer`, with one public class per file.
- Result objects are small immutable classes with get-only properties and a constructor, like `ValidationResult`.
- Every new behavior gets xUnit tests in `Analyzer.Tests`, in a file named `<ClassName>Tests.cs`.
- Keep the tool passive: only send ordinary requests to the URL the user supplies. No scanning, fuzzing or exploit probing.
- Handle network and parsing errors by returning a result object with a message. Don't let exceptions reach the user.

## Workflow

- Work is tracked in GitHub issues:
  - #3 Fetch an HTTP response safely
  - #4 Analyze HTTPS usage
  - #5 Analyze selected security headers
  - #6 Generate the console security report
- Branches are named `feature/<issue-number>-<short-name>`. Changes go through pull requests into `main`.
- Commit messages use the `feat:`, `chore:` style.
