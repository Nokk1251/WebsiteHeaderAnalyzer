-- Project Charter

1. Project

Website Security Header Analyzer

2. Problem

Beginners and small website owners may not know whether their websites use
HTTPS and common HTTP security headers correctly.

3. The Goal

Build an educational C# tool that performs passive checks of HTTPS usage and
selected HTTP response headers.

4. Target User

A beginner web developer or cybersecurity student who wants a quick,
understandable preliminary check.

5. MVP

- Accept one URL
- Validate the URL
- Send an HTTP request
- Detect the final URL after redirects
- Check whether HTTPS is used
- Check a selected set of security headers
- Display a readable console report
- Handle errors without crashing
- Include automated tests and documentation

6. Definition of Done

The application performs the planned checks, displays clear results, passes
its tests, and can be run using the instructions in the README.

-- Disclaimer!

This is an educational passive-analysis tool, not a professional security audit.