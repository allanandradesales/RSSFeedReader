# Quickstart: MVP RSS Feed Reader

**Branch**: `001-mvp-rss-reader`
**Date**: 2026-02-27
**Platform**: Windows 10+ / macOS 12+

---

## Prerequisites

### All platforms
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (LTS)
- .NET MAUI workload:
  ```sh
  dotnet workload install maui
  ```
- Verify install:
  ```sh
  dotnet --version          # 8.x.x
  dotnet workload list      # should include maui
  ```

### macOS only
- **Xcode 13.3+** (from the Mac App Store) — required for MAUI macOS builds
- After installing Xcode, accept the license:
  ```sh
  sudo xcodebuild -license accept
  ```

### Windows only
- **Visual Studio 2022 17.8+** with the ".NET Multi-platform App UI development" workload
  (optional but recommended for XAML hot reload)

---

## Clone & Build

```sh
git clone <repo-url>
cd RSSFeedReader
git checkout 001-mvp-rss-reader

# Restore all NuGet packages
dotnet restore

# Build the solution (all projects)
dotnet build
```

---

## Run the Application

### macOS
```sh
dotnet run --project src/RSSFeedReader.Presentation \
           --framework net8.0-maccatalyst
```

### Windows
```sh
dotnet run --project src/RSSFeedReader.Presentation ^
           --framework net8.0-windows10.0.19041.0
```

The SQLite database file is created automatically on first launch at:
- **macOS**: `~/Library/Application Support/RSSFeedReader/feeds.db`
- **Windows**: `%LOCALAPPDATA%\RSSFeedReader\feeds.db`

EF Core migrations are applied automatically before the UI renders (idempotent).

---

## Run Tests

```sh
# All test projects
dotnet test

# Specific layer
dotnet test tests/RSSFeedReader.Domain.Tests
dotnet test tests/RSSFeedReader.Application.Tests
dotnet test tests/RSSFeedReader.Infrastructure.Tests

# With coverage report (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

Tests use an in-memory SQLite database (`DataSource=:memory:`) — no real database file
is created or required.

Coverage target: **≥ 80%** for `Application` and `Domain` layers.

---

## Add a New EF Core Migration

After modifying an entity in `RSSFeedReader.Domain` or `AppDbContext`:

```sh
dotnet ef migrations add <MigrationName> \
  --project src/RSSFeedReader.Infrastructure \
  --startup-project src/RSSFeedReader.Presentation

# Verify the generated migration looks correct, then apply:
dotnet ef database update \
  --project src/RSSFeedReader.Infrastructure \
  --startup-project src/RSSFeedReader.Presentation
```

Migrations are applied automatically on the next app launch via `MigrateAsync()`.

---

## Code Quality Gates

Run locally before opening a PR:

```sh
# Format check (must pass — enforced in CI)
dotnet format --verify-no-changes

# Roslyn analyzers (build warnings treated as errors in CI)
dotnet build -warnaserror
```

---

## Project Structure

```
RSSFeedReader/
├── src/
│   ├── RSSFeedReader.Domain/           # Entities, interfaces, value objects
│   │   ├── Entities/
│   │   │   ├── Feed.cs
│   │   │   └── Article.cs
│   │   └── Interfaces/
│   │       ├── Repositories/
│   │       │   ├── IFeedRepository.cs
│   │       │   └── IArticleRepository.cs
│   │       └── Services/
│   │           ├── IFeedFetcherService.cs
│   │           └── IContentSanitizerService.cs
│   │
│   ├── RSSFeedReader.Application/      # Use cases, DTOs
│   │   ├── UseCases/
│   │   │   ├── AddFeedSubscription/
│   │   │   ├── RefreshFeeds/
│   │   │   ├── GetArticles/
│   │   │   ├── MarkArticleRead/
│   │   │   └── RemoveFeedSubscription/
│   │   └── DTOs/
│   │
│   ├── RSSFeedReader.Infrastructure/   # EF Core, HttpClient, HtmlSanitizer
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── FeedFetcher/
│   │   │   ├── FeedFetcherService.cs
│   │   │   └── SsrfGuard.cs
│   │   └── ContentSanitizer/
│   │       └── HtmlSanitizerAdapter.cs
│   │
│   └── RSSFeedReader.Presentation/    # MAUI app — pages, viewmodels, DI root
│       ├── MauiProgram.cs
│       ├── App.xaml
│       ├── AppShell.xaml
│       ├── Pages/
│       └── ViewModels/
│
├── tests/
│   ├── RSSFeedReader.Domain.Tests/
│   ├── RSSFeedReader.Application.Tests/
│   └── RSSFeedReader.Infrastructure.Tests/
│
├── StakeholderDocuments/
├── specs/
└── RSSFeedReader.sln
```

---

## Key Architecture Rules (Constitution v1.1.0)

| Rule | Where enforced |
|------|---------------|
| Domain/Application MUST NOT reference Infrastructure | Roslyn analyzers in CI |
| All HTML stored in `Article.Content` is pre-sanitized | `FeedFetcherService` → `IContentSanitizerService` |
| Feed URLs validated for SSRF before any HTTP call | `SsrfGuard.Validate()` in `FeedFetcherService` |
| 10s timeout per feed fetch | Named HttpClient `Timeout` property |
| EF Core migrations applied on startup | `MauiProgram.cs` → `MigrateAsync()` |
| ≥ 80% test coverage on Domain + Application | CI coverage gate |

---

## Troubleshooting

**"MAUI workload not found"**
```sh
dotnet workload update
dotnet workload install maui
```

**"Unable to open database file" on macOS**
Ensure the `~/Library/Application Support/RSSFeedReader/` directory exists and is
writable. The app creates it automatically, but sandboxing may prevent it on first
launch without entitlements — check `Platforms/MacCatalyst/Entitlements.plist`.

**"Migration pending" warning on startup**
The app applies migrations automatically. If you see this in tests, ensure the test
fixture calls `Database.EnsureCreatedAsync()` (not `MigrateAsync()`).
