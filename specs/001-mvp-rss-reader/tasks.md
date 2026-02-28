# Tasks: MVP RSS Feed Reader

**Input**: Design documents from `/specs/001-mvp-rss-reader/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: Not requested in spec — test tasks omitted. Add TDD tasks if desired via `/speckit.tasks --tdd`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description — file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Paths assume the project structure from `plan.md`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the solution, projects, and shared tooling before any feature work begins.

- [ ] T001 Create .NET 8 solution with 4 source projects (Domain, Application, Infrastructure, Presentation) and 3 test projects (Domain.Tests, Application.Tests, Infrastructure.Tests) — `RSSFeedReader.sln`
- [ ] T002 Add `Directory.Build.props` at repo root to enforce `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, and Roslyn/SonarAnalyzer.CSharp analyzer references across all projects — `Directory.Build.props`
- [ ] T003 [P] Add NuGet package references: `Microsoft.EntityFrameworkCore.Sqlite` 8.x and `Microsoft.EntityFrameworkCore.Design` to Infrastructure; `HtmlSanitizer` to Infrastructure; `xUnit`, `Moq`, `Microsoft.Data.Sqlite` to test projects — `src/RSSFeedReader.Infrastructure/RSSFeedReader.Infrastructure.csproj`
- [ ] T004 [P] Create GitHub Actions CI workflow: on PR run `dotnet restore`, `dotnet build -warnaserror`, `dotnet format --verify-no-changes`, `dotnet test --collect:"XPlat Code Coverage"`; on merge to main run build + test + publish — `.github/workflows/ci.yml`

**Checkpoint**: `dotnet build` succeeds across the whole solution.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared infrastructure that every user story depends on. No story work starts until this phase is complete.

⚠️ **CRITICAL**: All user story phases depend on this phase completing first.

### Domain Layer

- [ ] T005 Create `Feed` entity with properties: `Id (Guid)`, `Url (string)`, `Title (string)`, `LastRefreshedAt (DateTimeOffset?)`, `CreatedAt (DateTimeOffset)` — `src/RSSFeedReader.Domain/Entities/Feed.cs`
- [ ] T006 Create `Article` entity with properties: `Id (Guid)`, `FeedId (Guid)`, `FeedGuid (string)`, `Title (string)`, `Summary (string?)`, `Content (string?)`, `OriginalUrl (string)`, `PublishedAt (DateTimeOffset)`, `FetchedAt (DateTimeOffset)`, `IsRead (bool, default false)` — `src/RSSFeedReader.Domain/Entities/Article.cs`
- [ ] T007 [P] Create `IFeedRepository` interface with methods: `GetAllAsync`, `GetByUrlAsync`, `AddAsync`, `DeleteAsync`, `UpdateLastRefreshedAtAsync` (all with `CancellationToken`) — `src/RSSFeedReader.Domain/Interfaces/Repositories/IFeedRepository.cs`
- [ ] T008 [P] Create `IArticleRepository` interface with methods: `GetAllSortedAsync`, `GetByFeedIdAsync`, `GetUnreadCountByFeedIdAsync`, `UpsertManyAsync`, `MarkAsReadAsync`, `ToggleReadStatusAsync`, `DeleteByFeedIdAsync` (all with `CancellationToken`) — `src/RSSFeedReader.Domain/Interfaces/Repositories/IArticleRepository.cs`
- [ ] T009 [P] Create `IFeedFetcherService` interface, `FeedFetchResult` sealed record, `ParsedArticle` sealed record, and `FeedFetchError` enum (InvalidUrl, SsrfBlocked, SelfSignedCertificate, HttpError, Timeout, ParseError, NotAFeed) — `src/RSSFeedReader.Domain/Interfaces/Services/IFeedFetcherService.cs`
- [ ] T010 [P] Create `IContentSanitizerService` interface with `string? Sanitize(string? rawHtml)` — `src/RSSFeedReader.Domain/Interfaces/Services/IContentSanitizerService.cs`

### Infrastructure Layer

- [ ] T011 Create `AppDbContext` with `DbSet<Feed>` and `DbSet<Article>`; configure entity mappings: unique index on `Feed.Url`; unique index on `Article.FeedGuid`; composite indexes on `(Article.FeedId, Article.PublishedAt)`, `Article.PublishedAt`, `(Article.FeedId, Article.IsRead)`; cascade delete Feed → Articles; `IsRead` default false — `src/RSSFeedReader.Infrastructure/Persistence/AppDbContext.cs`
- [ ] T012 Generate initial EF Core migration creating `Feeds` and `Articles` tables with all indexes from T011; verify migration SQL is correct before proceeding — `src/RSSFeedReader.Infrastructure/Persistence/Migrations/`
- [ ] T013 Implement `FeedRepository`: `GetAllAsync` (alphabetical by Title, AsNoTracking), `GetByUrlAsync` (AsNoTracking), `AddAsync`, `DeleteAsync` (no-op if not found), `UpdateLastRefreshedAtAsync` — `src/RSSFeedReader.Infrastructure/Persistence/Repositories/FeedRepository.cs`
- [ ] T014 Implement `ArticleRepository`: `GetAllSortedAsync` (PublishedAt DESC, AsNoTracking), `GetByFeedIdAsync`, `GetUnreadCountByFeedIdAsync`, `UpsertManyAsync` (batch existence check by FeedGuid then insert-only), `MarkAsReadAsync`, `ToggleReadStatusAsync`, `DeleteByFeedIdAsync` — `src/RSSFeedReader.Infrastructure/Persistence/Repositories/ArticleRepository.cs`
- [ ] T015 Implement `SsrfGuard` static class with `Validate(string url)`: reject non-HTTP/HTTPS schemes; reject URLs > 2048 chars; resolve hostname via `Dns.GetHostAddressesAsync`; reject resolved IPs matching private ranges (10.x, 172.16–31.x, 192.168.x, 127.x, ::1); throw `ArgumentException` with descriptive message on failure — `src/RSSFeedReader.Infrastructure/FeedFetcher/SsrfGuard.cs`
- [ ] T016 Implement `HtmlSanitizerAdapter` implementing `IContentSanitizerService`: configure `HtmlSanitizer` allowlist (tags: p, a, img, ul, ol, li, h1–h6, blockquote, code, pre; attributes: href on a, src/alt/title on img, title on all; strip all event attrs, javascript: hrefs, style); return `null` for null/empty input — `src/RSSFeedReader.Infrastructure/ContentSanitizer/HtmlSanitizerAdapter.cs`
- [ ] T017 Implement `FeedFetcherService` implementing `IFeedFetcherService`: call `SsrfGuard.Validate(url)` pre-request; use named HttpClient "RssFeedClient" (Timeout=10s, AllowAutoRedirect=true, MaxRedirections=5); capture final URL from `response.RequestMessage!.RequestUri`; re-validate final URL via `SsrfGuard`; parse with `SyndicationFeed.Load`; sanitize each item's content via `IContentSanitizerService`; return typed `FeedFetchResult` (never throw for expected errors) — `src/RSSFeedReader.Infrastructure/FeedFetcher/FeedFetcherService.cs`

### Presentation DI Root

- [ ] T018 Configure `MauiProgram.cs`: register named HttpClient "RssFeedClient" (Timeout=10s, AllowAutoRedirect); register all repositories, services, and use-case handlers in DI; resolve `AppDbContext` and call `context.Database.MigrateAsync()` before UI renders; configure MAUI app shell — `src/RSSFeedReader.Presentation/MauiProgram.cs`

**Checkpoint**: Foundation complete — all entities, repositories, services, and DI wired. User story work can begin.

---

## Phase 3: User Story 1 — Subscribe to a Feed (Priority: P1) 🎯 MVP

**Goal**: User can add a feed by URL, see validation errors, and see the feed appear in the subscription list.

**Independent Test**: Launch app with no subscriptions → enter a valid RSS/Atom URL → confirm feed title appears in subscription list. Enter an invalid URL → confirm error message.

### Application Layer

- [ ] T019 Create `AddFeedSubscriptionCommand` record with `string Url` property — `src/RSSFeedReader.Application/UseCases/AddFeedSubscription/AddFeedSubscriptionCommand.cs`
- [ ] T020 [P] Create `FeedDto` record with `Guid Id`, `string Url`, `string Title`, `DateTimeOffset? LastRefreshedAt`, `int UnreadCount` — `src/RSSFeedReader.Application/DTOs/FeedDto.cs`
- [ ] T021 Implement `AddFeedSubscriptionHandler`: check duplicate via `IFeedRepository.GetByUrlAsync`; call `IFeedFetcherService.FetchAsync` to validate URL and get feed title; on `IsSuccess=false` map `FeedFetchError` to user-facing error string; on success create `Feed` entity and call `IFeedRepository.AddAsync`; return `FeedDto` — `src/RSSFeedReader.Application/UseCases/AddFeedSubscription/AddFeedSubscriptionHandler.cs`
- [ ] T022 [P] Create `GetFeedsQuery` record and `GetFeedsHandler` returning `IReadOnlyList<FeedDto>` via `IFeedRepository.GetAllAsync` — `src/RSSFeedReader.Application/UseCases/GetFeeds/`

### Presentation Layer

- [ ] T023 [P] [US1] Create `FeedListViewModel`: `ObservableCollection<FeedDto> Feeds`; `string UrlInput`; `ICommand AddFeedCommand` (calls `AddFeedSubscriptionHandler`, clears input on success); `string? ErrorMessage`; `bool IsLoading`; load feeds on init via `GetFeedsHandler` — `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [ ] T024 [US1] Create `FeedListPage.xaml`: URL text input + submit button; subscription list (feed title + URL, alphabetical); empty state "No subscriptions yet. Add a feed to get started."; error message label; loading indicator; navigate to ArticleListPage on feed tap — `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`
- [ ] T025 [US1] Register `FeedListPage` as root shell content in `AppShell.xaml`; register `FeedListViewModel` in DI; register route for `ArticleListPage` — `src/RSSFeedReader.Presentation/AppShell.xaml`

**Checkpoint**: User Story 1 fully functional and independently testable. ✅

---

## Phase 4: User Story 2 — Browse and Read Articles (Priority: P2)

**Goal**: User can tap a feed to see its articles sorted newest-first, open an article to read its content, and follow a link to the original source.

**Independent Test**: With one subscription and fetched articles → tap feed → see articles newest-first → tap article → see content rendered safely → tap original link.

### Application Layer

- [ ] T026 [P] Create `ArticleDto` record with `Guid Id`, `Guid FeedId`, `string FeedTitle`, `string Title`, `string? Summary`, `string? Content`, `string OriginalUrl`, `DateTimeOffset PublishedAt`, `bool IsRead` — `src/RSSFeedReader.Application/DTOs/ArticleDto.cs`
- [ ] T027 Implement `GetArticlesQuery` (with optional `Guid? FeedId` filter) and `GetArticlesHandler` returning `IReadOnlyList<ArticleDto>` sorted by `PublishedAt` descending — `src/RSSFeedReader.Application/UseCases/GetArticles/`

### Presentation Layer

- [ ] T028 [P] [US2] Create `ArticleListViewModel`: `ObservableCollection<ArticleDto> Articles`; accepts `FeedId` nav parameter; loads articles via `GetArticlesHandler` on init; `ICommand OpenArticleCommand` navigates to `ArticleDetailPage` — `src/RSSFeedReader.Presentation/ViewModels/ArticleListViewModel.cs`
- [ ] T029 [US2] Create `ArticleListPage.xaml`: list of articles showing title, feed name, publication date; visual read/unread distinction (bold title for unread); "No articles yet" empty state; tap navigates to ArticleDetailPage — `src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml`
- [ ] T030 [P] [US2] Create `ArticleDetailViewModel`: accepts `ArticleId` nav parameter; loads article via `GetArticlesHandler`; exposes `Content`, `OriginalUrl`, `Title`, `FeedTitle`, `PublishedAt` — `src/RSSFeedReader.Presentation/ViewModels/ArticleDetailViewModel.cs`
- [ ] T031 [US2] Create `ArticleDetailPage.xaml`: render `Content` as safe HTML via `WebView` or `HtmlLabel`; show "No content available" when Content is null; display link to `OriginalUrl` that opens in system browser — `src/RSSFeedReader.Presentation/Pages/ArticleDetailPage.xaml`
- [ ] T032 [US2] Register `ArticleListPage` and `ArticleDetailPage` routes in `AppShell.xaml`; wire navigation from `FeedListPage` → `ArticleListPage` (passing FeedId) and from `ArticleListPage` → `ArticleDetailPage` (passing ArticleId) — `src/RSSFeedReader.Presentation/AppShell.xaml`

**Checkpoint**: User Story 2 fully functional and independently testable. ✅

---

## Phase 5: User Story 3 — Refresh Feeds (Priority: P3)

**Goal**: User taps "Refresh All", sees a loading indicator, new articles appear without duplicates, and per-feed errors are shown for unreachable feeds.

**Independent Test**: Add subscription → tap Refresh All → loading indicator appears → completes → new articles appear in list → last-refreshed timestamp updates. Disconnect network on one feed → verify other feeds still refresh and per-feed error shows.

### Application Layer

- [ ] T033 Implement `RefreshFeedsCommand` and `RefreshFeedsHandler`: fetch all feeds via `IFeedRepository.GetAllAsync`; run `IFeedFetcherService.FetchAsync` per feed in parallel via `Task.WhenAll`; use `SemaphoreSlim(1,1)` to serialize `IArticleRepository.UpsertManyAsync` writes; call `IFeedRepository.UpdateLastRefreshedAtAsync` on success; collect per-feed `FeedFetchResult` errors; return `RefreshFeedsResult` (success count, error list) — `src/RSSFeedReader.Application/UseCases/RefreshFeeds/`

### Presentation Layer

- [ ] T034 [US3] Update `FeedListViewModel`: add `ICommand RefreshAllCommand` (calls `RefreshFeedsHandler`); `bool IsRefreshing` flag; `ObservableCollection<string> RefreshErrors` (per-feed error messages); `DateTimeOffset? LastRefreshedAt` display — `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [ ] T035 [US3] Update `FeedListPage.xaml`: add "Refresh All" button bound to `RefreshAllCommand`; `ActivityIndicator` bound to `IsRefreshing`; per-feed error list display; last-refreshed timestamp label — `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`

**Checkpoint**: User Story 3 fully functional and independently testable. ✅

---

## Phase 6: User Story 4 — Track Reading Progress (Priority: P4)

**Goal**: Articles auto-mark as read on open; user can manually toggle; read/unread state persists across restarts; unread count shown per feed.

**Independent Test**: Open article → close and restart app → confirm article shows as read. Manually toggle to unread → restart → confirm unread. Check feed in list shows correct unread count.

### Application Layer

- [ ] T036 Implement `MarkArticleReadCommand` and `MarkArticleReadHandler`: call `IArticleRepository.MarkAsReadAsync(articleId)` — `src/RSSFeedReader.Application/UseCases/MarkArticleRead/`
- [ ] T037 [P] Implement `ToggleReadStatusCommand` and `ToggleReadStatusHandler`: call `IArticleRepository.ToggleReadStatusAsync(articleId)` — `src/RSSFeedReader.Application/UseCases/ToggleReadStatus/`
- [ ] T038 [P] Implement `GetUnreadCountQuery` (with `Guid FeedId`) and `GetUnreadCountHandler`: call `IArticleRepository.GetUnreadCountByFeedIdAsync` — `src/RSSFeedReader.Application/UseCases/GetUnreadCount/`

### Presentation Layer

- [ ] T039 [US4] Update `ArticleDetailViewModel` to call `MarkArticleReadHandler` immediately on article load (before rendering) — `src/RSSFeedReader.Presentation/ViewModels/ArticleDetailViewModel.cs`
- [ ] T040 [US4] Update `ArticleListPage.xaml`: visually distinguish unread (bold title) vs read articles; add swipe action or context menu to manually toggle read/unread status per article — `src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml`
- [ ] T041 [US4] Update `FeedListViewModel` to load and expose unread count per feed via `GetUnreadCountHandler` after feed list loads and after any refresh — `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [ ] T042 [US4] Update `FeedListPage.xaml` to display unread count badge or label alongside each feed title in the subscription list — `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`

**Checkpoint**: User Story 4 fully functional and independently testable. ✅

---

## Phase 7: User Story 5 — Manage Subscriptions (Priority: P5)

**Goal**: User can remove a feed after confirming a prompt; feed and all its articles are deleted; list updates immediately.

**Independent Test**: Add feed → tap Remove → confirm prompt → feed disappears from list → navigate to article list for that feed (should show empty or be inaccessible).

### Application Layer

- [ ] T043 Implement `RemoveFeedSubscriptionCommand` (with `Guid FeedId`) and `RemoveFeedSubscriptionHandler`: call `IFeedRepository.DeleteAsync(feedId)` (cascade deletes articles at DB level) — `src/RSSFeedReader.Application/UseCases/RemoveFeedSubscription/`

### Presentation Layer

- [ ] T044 [US5] Update `FeedListViewModel`: add `ICommand RemoveFeedCommand` accepting `FeedDto`; show system confirmation dialog (DisplayAlert) before executing; on confirm call `RemoveFeedSubscriptionHandler` and remove feed from `Feeds` collection — `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [ ] T045 [US5] Update `FeedListPage.xaml`: add "Remove" swipe action or context menu item to each feed row; bind to `RemoveFeedCommand`; list updates immediately on removal — `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`

**Checkpoint**: User Story 5 fully functional and independently testable. ✅

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Quality, security, and UX improvements that span multiple stories.

- [ ] T046 [P] Add XML `/// <summary>` documentation comments to all public types, methods, and properties in `RSSFeedReader.Domain` and `RSSFeedReader.Application` — `src/RSSFeedReader.Domain/`, `src/RSSFeedReader.Application/`
- [ ] T047 [P] Extract all magic values to named constants: `FeedFetchTimeoutSeconds = 10`, `MaxFeedUrlLength = 2048`, `MaxFeedTitleLength = 256`, `MaxArticleTitleLength = 512`, `MaxRedirections = 5` — `src/RSSFeedReader.Infrastructure/Constants.cs`
- [ ] T048 Handle offline launch UX: when app starts with no network and cached content exists, display articles without showing an error; when user taps Refresh All with no network, show a clear "No internet connection" message rather than per-feed errors — `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [ ] T049 [P] Run `dotnet format` across the whole solution and commit any reformatting changes; ensure `dotnet format --verify-no-changes` passes — `all source files`
- [ ] T050 Run full test suite; verify `dotnet test --collect:"XPlat Code Coverage"` reports ≥ 80% coverage for `RSSFeedReader.Application.Tests` and `RSSFeedReader.Domain.Tests` — `tests/`
- [ ] T051 Validate against `quickstart.md`: follow the full happy-path walkthrough (install deps, build, run, add feed, refresh, read article, mark read, remove feed); confirm all steps succeed — `specs/001-mvp-rss-reader/quickstart.md`
- [ ] T052 [P] Security review: verify `SsrfGuard` rejects all private IP ranges; verify `HtmlSanitizerAdapter` strips scripts and event attrs; confirm `ServerCertificateCustomValidationCallback` is never overridden; confirm no hardcoded secrets in any committed file — `src/RSSFeedReader.Infrastructure/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) — BLOCKS all user stories
- **User Stories (Phases 3–7)**: All depend on Foundational (Phase 2)
  - Can proceed in priority order (P1 → P2 → P3 → P4 → P5)
  - Or in parallel across stories if team capacity allows
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2 — no dependency on other stories
- **US2 (P2)**: Starts after Phase 2 — independently testable; US1 provides feed data in practice
- **US3 (P3)**: Starts after Phase 2 — extends FeedListPage from US1 (T034, T035 update existing files)
- **US4 (P4)**: Starts after Phase 2 — extends ViewModels from US2 and US3 (T039–T042 update existing files)
- **US5 (P5)**: Starts after Phase 2 — extends FeedListPage from US1 (T044, T045 update existing files)

### Within Each User Story

- Domain interfaces (Phase 2) → Application handlers → Presentation ViewModels → Presentation Pages
- ViewModels marked [P] can be built in parallel with their corresponding Pages
- Application handlers marked [P] (same story) can be built in parallel

### Parallel Opportunities

All tasks marked [P] within the same phase can be worked simultaneously:
- T003 + T004 (Setup: NuGet + CI)
- T007 + T008 + T009 + T010 (Foundational: all 4 interfaces)
- T019/T020/T022 + T023 (US1: DTOs + query handler + ViewModel in parallel with implementation handler)
- T026 + T027 + T028 + T030 (US2: DTO + query + both ViewModels)
- T036 + T037 + T038 (US4: three independent use cases)
- T046 + T047 + T049 + T052 (Polish: docs, constants, format, security review)

---

## Parallel Execution Examples

### Phase 2 Foundation (example parallel launch)

```sh
# Launch simultaneously:
Task: "Create IFeedRepository in src/RSSFeedReader.Domain/Interfaces/Repositories/IFeedRepository.cs"    # T007
Task: "Create IArticleRepository in src/RSSFeedReader.Domain/Interfaces/Repositories/IArticleRepository.cs"  # T008
Task: "Create IFeedFetcherService + types in src/RSSFeedReader.Domain/Interfaces/Services/"              # T009
Task: "Create IContentSanitizerService in src/RSSFeedReader.Domain/Interfaces/Services/"                # T010
```

### User Story 1 (example parallel launch after T019–T022)

```sh
# Launch simultaneously after T021 and T022 complete:
Task: "Create FeedListViewModel in src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs"   # T023
# Then sequentially:
Task: "Create FeedListPage.xaml in src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml"           # T024
Task: "Register FeedListPage in AppShell.xaml"                                                         # T025
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (Subscribe to Feed)
4. **STOP and VALIDATE**: Add a real RSS feed, confirm it appears in the list
5. Demo/test independently

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 (US1) → Add subscriptions — **MVP demo ready**
3. Phase 4 (US2) → Browse and read articles
4. Phase 5 (US3) → Refresh feeds with real-time updates
5. Phase 6 (US4) → Track reading progress
6. Phase 7 (US5) → Remove subscriptions
7. Phase 8 → Polish and ship

### Parallel Team Strategy

With multiple developers (after Phase 2 completes):
- **Dev A**: US1 → US3 (FeedListPage-centric work)
- **Dev B**: US2 (ArticleListPage + ArticleDetailPage)
- **Dev C**: US4 → US5 (state management + removal)

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies — safe to parallelize
- `[Story]` label maps each task to a user story for traceability
- Tasks T034, T035, T039–T042, T044, T045 **update existing files** from earlier stories — do not create new files
- Each user story phase should be independently runnable and demonstrable before moving on
- Commit after each phase or logical group
- Stop at any checkpoint to validate the story independently before proceeding
- No test tasks generated (not requested); add via `/speckit.tasks --tdd` if desired

## Summary

| Phase | Tasks | Parallel | Story |
|-------|-------|----------|-------|
| 1 — Setup | T001–T004 | T003, T004 | — |
| 2 — Foundational | T005–T018 | T007–T010, T003/T004 | — |
| 3 — US1 Subscribe | T019–T025 | T020, T022, T023 | US1 |
| 4 — US2 Browse/Read | T026–T032 | T026, T027, T028, T030 | US2 |
| 5 — US3 Refresh | T033–T035 | — | US3 |
| 6 — US4 Track Progress | T036–T042 | T037, T038 | US4 |
| 7 — US5 Remove | T043–T045 | — | US5 |
| 8 — Polish | T046–T052 | T046, T047, T049, T052 | — |
| **Total** | **52 tasks** | **17 parallelizable** | — |
