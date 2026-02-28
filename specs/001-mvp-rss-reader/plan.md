# Implementation Plan: MVP RSS Feed Reader

**Branch**: `001-mvp-rss-reader` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-mvp-rss-reader/spec.md`

---

## Summary

Build a local-first, offline-capable desktop RSS/Atom feed reader for Windows 10+ and
macOS 12+ using .NET 8 + .NET MAUI. The application allows users to subscribe to feeds
by URL, manually refresh all feeds to fetch new articles, browse articles sorted
newest-first, and track read/unread status — with all data persisted in a local SQLite
database via EF Core 8. No cloud services, no user accounts.

Architecture follows Clean Architecture with four layers (Domain, Application,
Infrastructure, Presentation). Security controls are enforced at the Infrastructure
boundary: SSRF validation before every HTTP call, HtmlSanitizer for all feed content,
TLS 1.2+ with self-signed cert rejection.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS
**UI Framework**: .NET MAUI (native XAML + MVVM)
**Primary Dependencies**:
- `System.ServiceModel.Syndication` (built-in) — RSS 2.0 / Atom 1.0 parsing
- `Microsoft.EntityFrameworkCore.Sqlite` 8.x — persistence
- `HtmlSanitizer` (Ganss NuGet) — feed content sanitization
- `Microsoft.Extensions.DependencyInjection` (built-in via MAUI) — DI container
- `xUnit` + `Moq` or `NSubstitute` — testing
- `Microsoft.Data.Sqlite` — in-memory SQLite for integration tests

**Storage**: SQLite (local file, EF Core 8 migrations applied on startup)
**Testing**: xUnit, Moq/NSubstitute, in-memory SQLite (`DataSource=:memory:`)
**Target Platform**: Windows 10+ (`net8.0-windows10.0.19041.0`), macOS 12+ (`net8.0-maccatalyst`)
**Project Type**: desktop-app (local-first, offline-capable)
**Performance Goals**:
- Feed fetch timeout: hard 10 seconds per feed (Constitution § IV)
- Startup with cached content: ≤ 2 seconds (Constitution § IV / ProjectGoals.md)
**Constraints**:
- No cloud services, no external APIs, no user accounts in MVP
- Feed fetching MUST NOT block the UI thread
- All article HTML MUST be sanitized before storage
- TLS 1.2+ enforced; self-signed certs rejected
**Scale/Scope**: Single-user, ~6 MVP features, ~5 MAUI pages, ~4 service contracts

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### I. Security by Design ✅ PASS

- `SsrfGuard` validates every feed URL pre-request: rejects non-HTTP/HTTPS schemes,
  private IP ranges (10.x, 172.16–31.x, 192.168.x, 127.x, ::1), URLs > 2048 chars
- `SsrfGuard` re-applied to canonical URL after redirect resolution
- `HtmlSanitizerAdapter` wraps `HtmlSanitizer` NuGet with 13-tag allowlist; all article
  HTML sanitized before storage — raw content never reaches the database
- TLS: .NET 8 defaults enforce TLS 1.2+; `ServerCertificateCustomValidationCallback`
  MUST NOT be overridden (self-signed rejection is .NET default behavior)
- HTTPS-only enforced; HTTP feeds surface a user warning (FR-003)
- No hardcoded credentials or API keys — no external services required in MVP

### II. Maintainability & Clean Architecture ✅ PASS

- Four-layer structure enforced: Domain → Application → Infrastructure → Presentation
- Dependency rule: outer layers depend on inner; inner layers NEVER reference outer
- All cross-layer communication via interfaces defined in Domain
  (`IFeedRepository`, `IArticleRepository`, `IFeedFetcherService`, `IContentSanitizerService`)
- DI wired exclusively in `MauiProgram.cs` (Presentation layer) via
  `Microsoft.Extensions.DependencyInjection`
- No service locator pattern anywhere

### III. Code Quality Standards ✅ PASS

- `dotnet-format` enforced in GitHub Actions on every PR (build fails if reformatting needed)
- Roslyn analyzers + `SonarAnalyzer.CSharp` — warnings treated as errors in CI
- `<Nullable>enable</Nullable>` set globally in all `.csproj` files
- XML `/// <summary>` doc comments required on all public interfaces, classes, and methods
  in Domain and Application layers
- Microsoft C# coding conventions; named constants for all magic values (timeouts, limits)
- PR review required before merge to `main`

### IV. Reliability & Resilience ✅ PASS

- `HttpClient.Timeout = TimeSpan.FromSeconds(10)` on named client "RssFeedClient"
- All feed fetching via `async/await`; `Task.WhenAll` for parallel multi-feed refresh
- `SemaphoreSlim(1,1)` in `RefreshFeedsHandler` serializes write operations to SQLite
- Per-feed error isolation: `FeedFetchResult.IsSuccess = false` returned (no throw);
  remaining feeds continue; per-feed error shown in UI
- App startup renders cached SQLite content before any network call
- WAL journal mode (SQLite EF Core default) enables concurrent reads during writes
- Deduplication by `Article.FeedGuid` unique index prevents duplicate articles on refresh

### V. Testable by Default ✅ PASS

- Domain and Application classes depend on interfaces only; all injectable via
  Moq/NSubstitute in unit tests — no live network or database needed
- Infrastructure tests use `SqliteConnection("DataSource=:memory:")` + `IAsyncLifetime`
  (real SQLite engine, no production database file)
- Coverage target ≥ 80% for Domain and Application layers enforced in CI
- Test naming convention: `MethodName_StateUnderTest_ExpectedBehavior`
- `FeedFetcherService`, `HtmlSanitizerAdapter`, and `SsrfGuard` each have dedicated
  test classes covering all error branches

**→ All 5 gates PASS. No violations. Phase 0 research may begin.**

---

## Project Structure

### Documentation (this feature)

```text
specs/001-mvp-rss-reader/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── IFeedRepository.md
│   ├── IArticleRepository.md
│   ├── IFeedFetcherService.md
│   └── IContentSanitizerService.md
└── tasks.md             # Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (repository root)

```text
src/
├── RSSFeedReader.Domain/
│   ├── Entities/
│   │   ├── Feed.cs
│   │   └── Article.cs
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IFeedRepository.cs
│   │   │   └── IArticleRepository.cs
│   │   └── Services/
│   │       ├── IFeedFetcherService.cs
│   │       └── IContentSanitizerService.cs
│   └── ValueObjects/
│       └── FeedUrl.cs
│
├── RSSFeedReader.Application/
│   ├── UseCases/
│   │   ├── AddFeedSubscription/
│   │   │   ├── AddFeedSubscriptionCommand.cs
│   │   │   └── AddFeedSubscriptionHandler.cs
│   │   ├── RefreshFeeds/
│   │   │   ├── RefreshFeedsCommand.cs
│   │   │   └── RefreshFeedsHandler.cs
│   │   ├── GetArticles/
│   │   │   ├── GetArticlesQuery.cs
│   │   │   └── GetArticlesHandler.cs
│   │   ├── MarkArticleRead/
│   │   │   ├── MarkArticleReadCommand.cs
│   │   │   └── MarkArticleReadHandler.cs
│   │   └── RemoveFeedSubscription/
│   │       ├── RemoveFeedSubscriptionCommand.cs
│   │       └── RemoveFeedSubscriptionHandler.cs
│   └── DTOs/
│       ├── FeedDto.cs
│       └── ArticleDto.cs
│
├── RSSFeedReader.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Migrations/
│   │   └── Repositories/
│   │       ├── FeedRepository.cs
│   │       └── ArticleRepository.cs
│   ├── FeedFetcher/
│   │   ├── FeedFetcherService.cs
│   │   └── SsrfGuard.cs
│   └── ContentSanitizer/
│       └── HtmlSanitizerAdapter.cs
│
└── RSSFeedReader.Presentation/
    ├── MauiProgram.cs        # DI root, migrations on startup
    ├── App.xaml
    ├── AppShell.xaml
    ├── Pages/
    │   ├── FeedListPage.xaml
    │   ├── ArticleListPage.xaml
    │   └── ArticleDetailPage.xaml
    └── ViewModels/
        ├── FeedListViewModel.cs
        ├── ArticleListViewModel.cs
        └── ArticleDetailViewModel.cs

tests/
├── RSSFeedReader.Domain.Tests/
├── RSSFeedReader.Application.Tests/
└── RSSFeedReader.Infrastructure.Tests/
```

**Structure Decision**: Single solution with 4 Clean Architecture projects + 3 test
projects (one per testable layer). .NET MAUI Presentation project is the DI composition
root and app entry point. No web backend — purely local desktop app.

---

## Constitution Check (Post-Design Re-evaluation)

**→ All 5 gates still PASS after Phase 1 design.**

No new violations introduced. The data model, contracts, and project structure all
reinforce rather than compromise any principle.

---

## Complexity Tracking

> No violations to justify. All constitution gates pass without exception.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
