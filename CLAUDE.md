# RSSFeedReader-claude Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-27

## Active Technologies

- C# 12 / .NET 8 LTS — .NET MAUI desktop app (001-mvp-rss-reader)
- SQLite + EF Core 8 — local-first persistence, migrations on startup
- HtmlSanitizer (Ganss NuGet) — feed content sanitization
- xUnit + Moq/NSubstitute — testing

## Project Structure

```text
src/
├── RSSFeedReader.Domain/        # Entities, interfaces (no external deps)
├── RSSFeedReader.Application/   # Use cases, DTOs
├── RSSFeedReader.Infrastructure/# EF Core, HttpClient, HtmlSanitizer
└── RSSFeedReader.Presentation/  # MAUI app, MauiProgram.cs (DI root)
tests/
├── RSSFeedReader.Domain.Tests/
├── RSSFeedReader.Application.Tests/
└── RSSFeedReader.Infrastructure.Tests/
```

## Commands

```sh
dotnet restore                        # Restore NuGet packages
dotnet build                          # Build all projects
dotnet test                           # Run all tests
dotnet format --verify-no-changes     # Lint check (CI gate)
dotnet build -warnaserror             # Build with analyzer errors

# Run on macOS
dotnet run --project src/RSSFeedReader.Presentation --framework net8.0-maccatalyst

# Run on Windows
dotnet run --project src/RSSFeedReader.Presentation --framework net8.0-windows10.0.19041.0

# Add EF Core migration
dotnet ef migrations add <Name> --project src/RSSFeedReader.Infrastructure --startup-project src/RSSFeedReader.Presentation
```

## Code Style

- Microsoft C# coding conventions throughout
- Nullable reference types enabled globally (`<Nullable>enable</Nullable>`)
- XML `/// <summary>` doc comments on all public APIs in Domain and Application
- Named constants for all magic values (timeouts, limits, max lengths)
- `dotnet-format` + Roslyn analyzers + SonarAnalyzer.CSharp enforced in CI
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Clean Architecture dependency rule: Domain ← Application ← Infrastructure ← Presentation

## Recent Changes

- 001-mvp-rss-reader: Added C# 12 / .NET 8 LTS + MAUI + EF Core 8 + SQLite + HtmlSanitizer

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
