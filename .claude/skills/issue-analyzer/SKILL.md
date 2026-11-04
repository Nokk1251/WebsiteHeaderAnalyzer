---
name: issue-analyzer
description: Analyze a GitHub Issue in Nokk1251/WebsiteHeaderAnalyzer and prepare a project-specific implementation plan. Always use Plan Mode. Never implement changes.
---

# Issue Analyzer

## Mandatory Mode

**You MUST work exclusively in Plan Mode.**

- Enter or remain in Plan Mode before analyzing the issue.
- Do not switch to normal execution mode.
- Do not implement any part of the solution.
- Do not create, edit, or delete files.
- Do not run commands that modify the repository or its state.
- Do not create branches, commits, or pull requests.
- Do not install packages or modify project configuration.
- If Plan Mode is unavailable, stop and explain that the skill requires Plan Mode. Do not continue in another mode.

## Objective

Analyze a GitHub Issue from `Nokk1251/WebsiteHeaderAnalyzer` and produce a clear, practical, project-specific implementation plan that another developer or Claude Code session can follow.

The goal is to understand the requirement before writing code, inspect the existing C# implementation, identify the relevant parts of the solution, and define how the change can be implemented and verified without expanding the Issue's scope.

## Project Context

### Repository

- GitHub repository: `Nokk1251/WebsiteHeaderAnalyzer`
- Repository URL: `https://github.com/Nokk1251/WebsiteHeaderAnalyzer`
- Project: an educational C# console application for passive analysis of HTTPS usage and selected HTTP security headers.
- Main technology: C# and .NET 10.
- Tests: xUnit.
- The solution is currently located at `Analyzer/Analyzer.slnx` relative to the repository root. Verify the actual path in the checked-out working tree before relying on it.

### Project Scope

The application is intended to accept a user-provided website URL, validate it, make a passive HTTP request, inspect the response and report relevant information, including HTTPS usage and selected security headers.

Keep plans within this scope. The application is a passive HTTP/security-header analyzer, not a penetration-testing tool, vulnerability scanner, exploitation tool, or guarantee that a website is secure.

Do not propose active attacks, intrusive scanning, exploitation, or unrelated security-testing functionality unless the user explicitly changes the project requirements.

### Relevant Areas of the Application

Inspect the actual solution before deciding which files need changes. Depending on the Issue and the current implementation, relevant areas may include:

- URL input and validation, including trimming, accepted schemes (`http` and `https`), and host validation.
- HTTP request execution using `HttpClient` and `HttpClientHandler`.
- HTTP status-code handling.
- Manual redirect handling, redirect limits, relative or absolute `Location` values, and final URL detection.
- Response headers and content headers.
- Network errors, timeouts, cancellation, and resource disposal.
- The `HttpAnalysisResult` model and the meaning of its properties.
- Analysis of selected security headers, their values, explanations, and limitations.
- Console input, orchestration, and report output.
- xUnit tests for validation, HTTP behavior, header analysis, and reporting.

Names such as `UrlValidator`, `HttpAnalyzer`, `HttpAnalysisResult`, and `Program` may exist in the current implementation, but verify the actual source files and namespaces. Do not assume that planned or discussed functionality has already been implemented.

A likely execution flow is:

`Console input / Program` → `URL validation` → `HTTP analysis` → `analysis result` → `security-header analysis / console report`.

Treat this only as a guide. Trace the real execution flow in the current branch and clearly distinguish implemented behavior from planned behavior.

### Existing Repository Conventions

The current GitHub Issues use labels for categories such as:

- `type: bug`, `type: feature`, `type: refactor`, `type: test`, `type: chore`, `type: docs`.
- `area: url`, `area: http`, `area: headers`, `area: cli`, and `area: ci` where applicable.
- Priority labels such as `priority: high`, `priority: medium`, and `priority: low`.
- Milestones such as `MVP`, when assigned.

Always inspect the actual labels and milestones on the selected Issue. Do not assume this list is exhaustive or that every Issue uses all of these labels.

### Project Priority Guidance

Use the following project conventions when assessing whether an Issue's priority label is appropriate:

| Issue type / nature | Expected priority | Guidance |
|---|---|---|
| Bug or confirmed functional defect | `priority: high` | Incorrect behavior should be fixed before adding non-essential functionality. |
| Security defect or serious reliability defect | `priority: high` | Prioritize risks that can cause unsafe behavior, misleading analysis, or failure of core functionality. |
| Feature | `priority: medium` | New capability that extends the analyzer without fixing a defect. |
| Refactor | `priority: medium` | Structural improvement that supports correctness, testability, or maintainability. |
| Tests / testability improvement | `priority: medium` | Important when it protects core behavior or makes HTTP logic reliably testable. |
| Chore / maintenance | `priority: low` | Routine maintenance that does not block the current MVP. |
| Documentation | `priority: low` | Documentation improvements that do not block implementation or release. |

These are default conventions, not an excuse to ignore impact. A task that blocks the MVP or is a prerequisite for a critical feature may justify a different priority; explain the reason explicitly.

Distinguish the Issue's actual GitHub priority label from the priority recommended by this guidance. If they differ, report the mismatch and explain the suggested priority. Do not edit labels or otherwise modify the Issue.

## Workflow

### 1. Identify the Issue

- If the user provides a GitHub Issue number, use that Issue.
- If the user provides an issue title or ID, identify the corresponding GitHub Issue.
- If no Issue is specified, ask which Issue should be analyzed. Do not silently select another Issue.
- The default repository is `Nokk1251/WebsiteHeaderAnalyzer`. Verify the current repository with Git/GitHub context when possible; do not analyze a different repository just because its working directory is open.
- Use GitHub CLI (`gh`) to retrieve the Issue when available.
- Read the actual Issue title, number, description, acceptance criteria, labels, milestone, state, assignees, and linked or blocking Issues when available.
- Check the Issue's comments when they contain clarifications, decisions, or updated requirements.
- If the Issue cannot be retrieved or identified unambiguously, stop and explain the problem. Never invent missing Issue details.

### 2. Inspect the Existing Project

Read the relevant source code, tests, README, and project documentation to understand the current implementation.

Identify:

- The relevant projects, classes, methods, properties, and data models.
- The current URL-validation and/or HTTP-analysis flow affected by the Issue.
- How the current behavior works, including existing limitations.
- Existing C# and .NET conventions that the solution should follow.
- Relevant xUnit tests and how the behavior is currently verified.
- Dependencies or previously completed Issues that affect the implementation.
- Whether the required behavior already exists, partially exists, or is not implemented.

Use read-only inspection only. Do not modify the repository.

Do not assume generic web-application components such as controllers, API endpoints, database contexts, repositories, Entity Framework, or ASP.NET services exist in this console application. Mention them only if the inspected solution actually contains them.

### 3. Analyze the Requirements

Determine:

- What defect must be corrected or what capability must be added.
- What the expected behavior should be for the analyzer.
- Which acceptance criteria must be satisfied.
- Which normal, invalid-input, network-failure, redirect, timeout, and boundary scenarios are relevant.
- Whether the Issue depends on another Issue or existing functionality.
- What risks, ambiguities, or compatibility concerns exist.
- Whether the requested change remains within the project's passive-analysis scope.
- Whether the current priority label matches the project's priority guidance.

For HTTP-related Issues, consider only the cases relevant to the selected Issue. Examples include:

- HTTP versus HTTPS input and final destination.
- Redirect status codes and missing or invalid `Location` headers.
- Relative versus absolute redirect destinations.
- Redirect limits and redirect loops.
- HTTP response headers versus content headers.
- Non-success HTTP status codes such as 404 or 500 that still represent received HTTP responses.
- DNS, connection, TLS, timeout, and cancellation failures.
- Disposal and lifetime of HTTP-related resources.
- Avoiding misleading claims that the presence of a security header guarantees that a site is safe.

Do not turn this list into extra requirements when they are unrelated to the selected Issue.

If important requirements are unclear, list the questions that need answers instead of making unsupported assumptions.

### 4. Prepare the Implementation Plan

Create an ordered, actionable plan that describes:

1. **Files and components:** Which existing files are likely to require changes and why. Mention new files only when justified.
2. **Implementation steps:** The logical sequence of changes, following the existing solution structure.
3. **Design decisions:** How the solution should fit the current C#/.NET architecture and conventions.
4. **Testing strategy:** Which xUnit tests and, where necessary, manual checks should verify the behavior.
5. **Validation:** Which build and test commands should be run after implementation.
6. **Completion criteria:** How to determine whether the Issue is fully resolved based on its actual acceptance criteria.

For HTTP/network behavior, prefer a deterministic test strategy. Consider an injected or fake `HttpMessageHandler` or a controlled local test server where appropriate, rather than depending on a live external website. Do not require this approach for every Issue; explain why it is appropriate when recommending it.

Mention exact file paths only after checking the current tree. Treat paths and implementation details as recommendations based on inspected code, not guaranteed facts when evidence is incomplete.

Do not introduce unnecessary abstractions, NuGet packages, unrelated refactoring, or features outside the Issue.

### 5. Build and Test Commands

When describing verification steps, use the solution and project paths confirmed in the current working tree.

The current known solution path is:

```powershell
dotnet test .\Analyzer\Analyzer.slnx
```

A separate build check may be recommended as:

```powershell
dotnet build .\Analyzer\Analyzer.slnx
```

If the solution path differs in the checked-out repository, use the actual path instead. Do not claim that any command was executed or passed unless it was actually run. This Skill is for planning only; the implementation session must perform the verification.

## Output Format

Respond in Bulgarian using the following structure:

### Issue

- GitHub Issue number and exact title.
- Current state.
- Actual priority label and type/area labels.
- Recommended priority according to the project conventions, if different.
- Milestone and dependencies, if available.
- Short explanation of the requirement based on the Issue's actual description.

### Current Implementation

- Relevant projects, files, classes, and methods.
- How the current behavior works.
- What is already implemented, partially implemented, or missing.
- What needs to change and why.

### Proposed Implementation Plan

Provide a numbered sequence of clear, actionable steps. Keep each step tied to the selected Issue's acceptance criteria.

### Testing Strategy

List the expected test scenarios, including relevant success cases, validation failures, HTTP/network failures, and edge cases. State which tests are unit tests and which require integration or controlled HTTP testing.

### Risks and Dependencies

List relevant dependencies, potential regressions, passive-analysis scope considerations, and unanswered questions. Distinguish confirmed facts from assumptions.

### Definition of Done

Provide a checklist derived from the Issue's actual acceptance criteria. Do not add unsupported acceptance criteria.

### Recommendation

Briefly explain the suggested implementation approach, the most important design consideration, and whether the Issue should be addressed before dependent work.

## Strict Constraints

- Plan Mode only, without exception.
- Analysis and planning only; never implement the plan.
- Never edit files, even if a small change appears obvious.
- Never commit, push, create branches, or open pull requests.
- Never modify GitHub Issues, labels, milestones, or comments.
- Never run commands that change repository state.
- Never claim that tests or builds passed unless they were actually run.
- Do not expand the scope beyond the selected Issue.
- Do not assume acceptance criteria that are not present in the Issue.
- Do not treat planned functionality as already implemented.
- Do not recommend intrusive scanning, exploitation, or unrelated offensive-security features for this passive analyzer.
- After presenting the plan, stop and wait for the user's instructions.
