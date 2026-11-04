# CLAUDE.md

## 1. Project Overview

`WebsiteSecurityHeaderAnalyzer` is a C#/.NET console application that performs a passive analysis of a user-provided website URL. Its purpose is to inspect the HTTP response, determine whether the final destination uses HTTPS, collect relevant response information, and report the presence and values of selected security-related HTTP headers.

The project is an informational website configuration checker. It is not a penetration-testing tool, vulnerability scanner, or guarantee that a website is safe.

### Technology Stack
- C#
- .NET 10
- .NET console application
- `HttpClient` and `HttpClientHandler` for HTTP communication
- xUnit for unit testing

### Solution Structure
The current solution is located at:

```text
Analyzer/
├── Analyzer.slnx
├── Analyzer/
│   └── Analyzer.csproj
└── Analyzer.Tests/
    └── Analyzer.Tests.csproj
```

The solution and project structure may evolve. Before making changes, inspect the existing solution and follow its established structure unless the task explicitly requires a change.

### Main Responsibilities
- Validate user-provided URLs and accept only supported HTTP and HTTPS addresses.
- Send passive HTTP requests and collect response information.
- Handle HTTP responses, network failures, and request timeouts explicitly.
- Track HTTP redirects and identify the final destination.
- Determine whether the final destination uses HTTPS.
- Inspect selected security-related response headers.
- Present analysis results in a clear and understandable console report.

Do not document planned behavior as implemented behavior. Check the current code before describing a feature as complete.

## 2. Development Workflow

### Understand Before Changing
- Read the relevant source files, result models, URL validation logic, HTTP request logic, and tests before modifying code.
- Trace the existing URL validation and HTTP analysis flow and understand the current behavior.
- Check related code and existing conventions before introducing new patterns.
- Do not assume that existing behavior is incorrect without investigating it.
- Keep changes focused on the requested GitHub Issue.
- Preserve the distinction between receiving an HTTP response and receiving a successful HTTP status. For this project, an HTTP response such as `404 Not Found` is still a received response; do not redefine result semantics without an explicit requirement.
- Keep the analysis passive. Do not introduce exploit attempts, authentication bypasses, brute-force behavior, intrusive crawling, or other active security testing.

### Work Through GitHub Issues
- Every development task must be associated with a GitHub Issue whenever applicable.
- Read the Issue description, acceptance criteria, labels, and dependencies before starting.
- If requirements are ambiguous, investigate the code and ask for clarification when necessary.
- Do not silently expand the scope of an Issue.
- A Pull Request without a specific Issue may be appropriate in some cases, such as a standalone documentation or repository-maintenance change.

### Branch and Pull Request Policy — Mandatory

**Never make project changes directly on `main`. Every change must be developed on a separate branch and merged into `main` only through a Pull Request.**

1. Ensure the working tree is clean before starting a new task. Do not overwrite, discard, or commit unrelated user changes.
2. Start from an up-to-date `main` branch and create a dedicated branch for the Issue.
3. Use descriptive branch names, for example:
   - `feat/manual-redirect-handling`
   - `feat/security-header-analysis`
   - `feat/console-report`
   - `fix/url-validation`
   - `fix/http-timeout-handling`
   - `refactor/http-analysis-result`
   - `test/http-analyzer`
   - `docs/update-readme`
   - `ci/github-actions`
4. Make changes only on the task branch.
5. Build the solution and run the relevant tests before proposing the change as complete.
6. Review the diff and remove unrelated changes before committing.
7. Always ask the user whether they want to see the diff before creating a commit, and wait for their answer.
8. Commit changes with a concise, descriptive message. Never add attribution or watermark lines to commit messages, PR descriptions, issues, comments, or any other written output (for example `Co-Authored-By: Claude ...` or `Generated with Claude Code`).
9. Push the task branch and create a Pull Request targeting `main`.
10. Include the related GitHub Issue in the PR description, preferably using `Closes #ISSUE_NUMBER` when the PR fully resolves the Issue.
11. Summarize the implementation, tests performed, and any known limitations in the PR description.
12. Wait for review and required CI checks. Address requested changes in the same branch and PR.
13. Do not merge the PR unless explicitly authorized by the repository owner or repository policy.

Never:
- Commit directly to `main`.
- Push changes directly to `main`.
- Use `git push --force` unless explicitly authorized and genuinely necessary.
- Bypass branch protection or required review and CI checks.
- Create a PR without checking which changes it contains.

If the current branch is `main`, do not edit project files. Create or switch to an appropriate task branch first, while preserving any existing work. If the working tree contains uncommitted changes, inspect them and preserve them before proceeding. Never reset, clean, stash, or overwrite user changes without explicit permission.

## 3. Coding Conventions

### General C# Guidelines
- Follow standard C# naming conventions:
  - `PascalCase` for classes, methods, properties, and public members.
  - `camelCase` for local variables and parameters.
  - Meaningful names that describe intent.
- Use `async` and `await` for asynchronous I/O operations, including HTTP requests.
- Use `CancellationToken` when it fits the existing method and application design; distinguish caller-requested cancellation from an HTTP timeout when applicable.
- Use nullable reference types and handle nullable values consistently with the existing project configuration.
- Prefer clear, readable code over clever or overly compact implementations.
- Avoid unnecessary comments that merely repeat what the code does. Comment on non-obvious decisions and behavior.
- Dispose of disposable resources appropriately, including HTTP responses and handlers/clients according to their ownership and lifetime.
- Avoid creating unnecessary abstractions or repeatedly creating network-related resources without considering their lifetime and intended usage.

### HTTP Communication and Redirect Handling
- Use `HttpClient` for HTTP communication and configure its handler deliberately.
- Keep automatic redirect behavior explicit. When redirects are followed manually, inspect the response status and `Location` header before issuing the next request.
- Support the relevant redirect status codes and handle missing, invalid, or unsupported `Location` values safely.
- Enforce a finite redirect limit to prevent redirect loops.
- Preserve the original URL separately from the current/final URL so the result can report both accurately.
- Update the redirect flag and redirect count only when a redirect is actually followed.
- Do not assume that `response.RequestMessage.RequestUri` represents the final destination when automatic redirects are disabled.
- Treat HTTP status codes as response information. A non-2xx status does not by itself mean that the HTTP request failed to obtain a response.
- Collect response headers without assuming that every header is present or that every response contains content.
- Consider both response headers and content headers when the required analysis depends on them.
- Do not claim that a website is secure merely because one or more security headers are present. Report observations accurately and explain limitations where appropriate.

### Core Components and Responsibilities
- Keep URL validation responsible for checking and normalizing user-provided addresses before an HTTP request is attempted.
- Keep HTTP communication and response collection in the HTTP analysis component.
- Keep the analysis result model focused on representing the outcome, including the initial URL, final URL, status code, headers, redirect information, and any error information required by the current design.
- Keep security-header evaluation focused on inspecting the selected headers and describing their presence or values.
- Keep console presentation separate from the underlying URL validation and HTTP analysis logic where the existing architecture allows it.
- Follow the architecture that exists in the current branch.
- Do not add unnecessary interfaces, factories, services, repositories, or other abstractions.
- When refactoring, preserve existing externally observable behavior unless the Issue explicitly requires a behavior change.

### Validation and Error Handling
- Validate user-provided URLs at the appropriate boundary before sending network requests.
- Accept only the protocols supported by the project, currently HTTP and HTTPS, unless an Issue explicitly changes this requirement.
- Handle expected failure cases explicitly, including invalid input, HTTP request failures, timeouts, redirect-limit exhaustion, and unusable redirect locations where applicable.
- Avoid broad exception handling that hides errors or returns misleading success responses.
- Do not swallow exceptions silently.
- Do not return `null` for result properties that are designed to contain non-null collections; use an empty collection when that is the intended failure-state representation.
- Keep error messages understandable and consistent. Do not expose unnecessary internal diagnostic details in user-facing output.
- Ensure that failure results accurately distinguish an unsuccessful request from an HTTP response with a non-success status code.

## 4. Testing Conventions

- Add or update tests when changing URL validation, HTTP analysis, redirect handling, security-header evaluation, or result behavior.
- Prefer focused unit tests for isolated logic.
- Use descriptive test names that explain the scenario and expected outcome.
- Cover successful behavior, failure cases, and relevant boundary conditions.
- Keep tests independent and deterministic.
- Avoid relying on public websites or external network availability for ordinary unit tests. Prefer controlled test doubles or a local test HTTP server when testing HTTP behavior.
- Use the existing xUnit test project and its established conventions.
- Do not modify or remove tests merely to make the build pass.
- If a test cannot be executed, explain why rather than claiming it passed.

When applicable, cover scenarios such as:
- Empty, whitespace-only, malformed, and unsupported-protocol URL input.
- Valid HTTP and HTTPS URLs, including normalized or trimmed input where supported.
- Successful receipt of an HTTP response, including non-2xx statuses such as `404`.
- Network errors and request timeouts.
- Redirect responses with absolute and relative `Location` values.
- Multiple redirects, redirect-limit exhaustion, and missing or invalid `Location` values.
- Correct initial URL, final URL, redirect flag, and redirect count.
- Present and missing security-related response headers.
- Correct handling of empty header collections and nullable result fields.

## 5. Build and Verification

Run commands from the repository root unless the project structure requires otherwise.

Restore dependencies:

```powershell
dotnet restore .\Analyzer\Analyzer.slnx
```

Build the solution:

```powershell
dotnet build .\Analyzer\Analyzer.slnx --no-restore
```

Run tests:

```powershell
dotnet test .\Analyzer\Analyzer.slnx
```

Run the console application:

```powershell
dotnet run --project .\Analyzer\Analyzer\Analyzer.csproj
```

Before completing a task:
- Run the build.
- Run relevant tests.
- Inspect the final diff.
- Confirm that only intended files have changed.
- Report the actual results of commands executed.

If a command fails, investigate and report the failure. Do not claim successful verification without evidence.

## 6. Documentation

- Update the README when setup, run, or test instructions change.
- Keep architecture and usage documentation consistent with the actual implementation.
- Update relevant documentation when a change materially affects analysis behavior, supported URL formats, redirect handling, security-header checks, configuration, or development workflow.
- Do not document planned behavior as implemented behavior.
- Explain that the tool performs passive checks and that header presence alone does not establish that a website is secure.
- Keep documentation changes focused on the task.

## 7. Dependencies and Scope Control

- Prefer the existing .NET and project capabilities.
- Add a NuGet dependency only when it provides clear value for the requested task.
- Avoid unrelated refactoring, formatting changes, and feature additions.
- Do not introduce new architectural patterns simply because they are popular.
- Do not turn the project into an active vulnerability scanner or penetration-testing tool unless the project scope is explicitly and deliberately changed.
- If a larger change appears necessary, explain why before expanding the scope.

## 8. Definition of Done

A task is complete only when:
- The requested behavior is implemented.
- Relevant tests have been added or updated where appropriate.
- The solution builds successfully, or any unresolved build failure is clearly reported.
- Relevant tests pass, or failures are clearly reported.
- The diff contains no unrelated changes.
- Relevant documentation has been updated.
- The changes are committed on the task branch and proposed through a Pull Request targeting `main`.
- The PR description explains the changes and verification performed.

**The Pull Request is the required path for every change to `main`. Do not bypass this workflow.**
