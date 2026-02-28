# Research: MVP RSS Feed Reader

**Branch**: `001-mvp-rss-reader`
**Phase**: 0 — Outline & Research
**Date**: 2026-02-27
**Input**: Feature spec + TechStack.md + StakeholderDocuments/ProjectGoals.md

---

## Decision 1: UI Framework — .NET MAUI vs Blazor WebAssembly

**Decision**: .NET MAUI (pure native XAML/MVVM)

**Rationale**:
- Direct, unrestricted access to the local file system and SQLite database file — no browser sandbox
- EF Core 8 with SQLite provider works natively without workarounds
- Native MAUI controls render immediately; no WASM compilation overhead
- Single `.csproj` targets both Windows 10+ and macOS 12+ from one codebase
- Async/await + `ObservableCollection<T>` MVVM bindings automatically marshal UI updates to main thread

**Alternatives considered**:
- **Blazor WebAssembly**: Rejected — runs inside browser sandbox; local SQLite file access requires
  complex OPFS/IndexedDB workarounds; unsuitable for a desktop-native, fully-offline app.
- **MAUI + BlazorWebView (hybrid)**: Rejected — adds WebView2 runtime dependency on Windows,
  WKWebView on macOS; unnecessary complexity for a single-user desktop app with no web-sharing needs.

**macOS caveat**: Distribution requires Xcode 13.3+ and code signing/notarization via the Apple
Developer program. Plan for this in the CI/CD pipeline for the packaging phase (Phase 5 of the
rollout plan).

---

## Decision 2: Async Feed Fetching (Non-Blocking UI)

**Decision**: `async/await` throughout all layers; named `HttpClient` via `IHttpClientFactory`;
`HttpClient.Timeout = TimeSpan.FromSeconds(10)` enforced at the named client level.

**Pattern**:
- ViewModels call use-case handlers via `async Task` commands
- Use-case handlers call `IFeedFetcherService.FetchAsync()` — fully awaitable
- For multiple feeds, `Task.WhenAll()` over per-feed fetch tasks enables parallel fetching
  bounded by a `SemaphoreSlim` to limit concurrent outbound connections
- MVVM data bindings automatically dispatch property updates to the UI thread via MAUI's dispatcher
- `BeginInvokeOnMainThread()` only needed outside the MVVM binding context

**Rationale**: Constitution § IV — "feed-fetching operations MUST run asynchronously and MUST NOT
block the UI thread." The MAUI MVVM pattern satisfies this without manual thread management.

---

## Decision 3: SSRF Prevention

**Decision**: Pre-request URL validation function (not a `DelegatingHandler`) — validates scheme,
length, and resolved IP range before any `HttpClient` call.

**Pattern**:
1. Reject non-HTTP/HTTPS schemes immediately (string check, no DNS call needed)
2. Reject URLs longer than 2048 characters
3. Resolve hostname via `Dns.GetHostAddressesAsync()` with a short timeout (≤ 100ms)
4. Regex-match each resolved IP against the private range blocklist:
   - `^10\.` (10.x)
   - `^172\.(1[6-9]|2[0-9]|3[01])\.` (172.16–172.31.x)
   - `^192\.168\.` (192.168.x)
   - `^127\.` (127.x loopback)
   - `^::1$` (IPv6 loopback)
5. Apply same validation to the final URL after redirect resolution

**Rationale**: A `DelegatingHandler` runs after DNS resolution, creating a TOCTOU window.
Pre-request validation + post-redirect re-validation eliminates this window.

---

## Decision 4: HTML Sanitization

**Decision**: `HtmlSanitizer` NuGet package (by Ganss), wrapped in `IContentSanitizerService`
adapter in the Infrastructure layer.

**Allowlist** (from Constitution § Security Constraints):
- Tags: `p`, `a`, `img`, `ul`, `ol`, `li`, `h1`–`h6`, `blockquote`, `code`, `pre`
- Attributes: `href` (on `a`), `src` (on `img`), `alt` (on `img`), `title` (on all)
- Stripped: all event attributes (`on*`), `javascript:` hrefs, `data:` image URIs from
  external tracking pixels, all `<script>` and `<iframe>` tags
- `<a href>` and `<img src>` values are further validated to be absolute HTTPS URLs or
  relative paths (re-using SSRF guard where applicable)

**Rationale**: Wrapping in an interface (not using `HtmlSanitizer` directly) keeps the
Infrastructure concern swappable and keeps Application/Domain layers free of the NuGet dependency.

---

## Decision 5: EF Core SQLite — Migrations, Concurrency, Deduplication

**Decision**: `Database.MigrateAsync()` on startup; WAL journal mode (default in EF Core SQLite);
`SemaphoreSlim(1,1)` to serialize write operations during parallel feed refresh.

**Migration pattern**:
- `MauiProgram.cs` resolves `AppDbContext` from the DI container and calls
  `context.Database.MigrateAsync()` before the UI renders
- Idempotent — safe to call on every app launch; creates the database file if absent
- EF Core migrations tracked in `RSSFeedReader.Infrastructure/Persistence/Migrations/`

**Concurrency pattern**:
- SQLite WAL mode (enabled by default in `Microsoft.EntityFrameworkCore.Sqlite`) allows
  concurrent reads during writes
- A `SemaphoreSlim(1,1)` in `RefreshFeedsHandler` serializes all batch write operations
  to prevent SQLite "database is locked" errors during parallel feed refresh

**Deduplication pattern**:
- `Article` entity has a unique database index on `FeedGuid` (the feed's own `<guid>` or `<id>`
  element; falls back to `OriginalUrl` if absent)
- Batch upsert: fetch all `FeedGuid` values for existing articles in one query, then
  `INSERT` only new ones — avoids per-article round-trips
- Unique constraint at the database level is the final safety net against race conditions

**Testing pattern** (from TechStack.md):
- `SqliteConnection("DataSource=:memory:")` held open per test class via `IAsyncLifetime`
- `Database.EnsureCreatedAsync()` (not `MigrateAsync`) for test schema — faster and
  equivalent for unit/integration tests without migration history

---

## Decision 6: Repository Pattern

**Decision**: Lightweight repository interfaces in `Domain`, implemented in `Infrastructure` with
`AppDbContext`. No generic repository — purpose-specific interfaces only.

**Rationale**:
- Repository interfaces in Domain keep Application use-cases independent of EF Core
- Concrete implementations in Infrastructure are the only layer that references `DbContext`
- Returns `Task<IReadOnlyList<T>>` (not `IQueryable<T>`) to prevent query composition leakage
  across layer boundaries
- `AsNoTracking()` on all read queries for performance (no change tracking overhead for
  display-only data)

---

## Resolved NEEDS CLARIFICATION Items

| Item | Resolution |
|------|-----------|
| UI Framework (Blazor vs MAUI) | .NET MAUI native (Blazor WASM and Hybrid rejected) |
| Async pattern | `async/await` + named HttpClient + `Task.WhenAll` for parallel feeds |
| SSRF approach | Pre-request DNS resolution + IP regex check (not DelegatingHandler) |
| Migration timing | `MigrateAsync()` in `MauiProgram.cs` before UI renders |
| Deduplication key | `Article.FeedGuid` (unique index) with `OriginalUrl` as fallback |
| Write concurrency | WAL mode (default) + `SemaphoreSlim(1,1)` per refresh operation |
