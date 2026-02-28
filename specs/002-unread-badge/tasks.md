# Tasks: Unread Article Count Badge

**Input**: Design documents from `/specs/002-unread-badge/`
**Branch**: `002-unread-badge`

---

## Phase 1: Setup

**Purpose**: New Application-layer DTOs and directory scaffolding needed by all phases.

- [X] T001 Create `ArticleDto` record in `src/RSSFeedReader.Application/DTOs/ArticleDto.cs`
- [X] T002 Create directories: `src/RSSFeedReader.Application/UseCases/RefreshFeedSubscription/`, `MarkArticleAsRead/`, `ToggleArticleReadStatus/`

---

## Phase 2: Foundational (blocking — must complete before Phase 3+)

**Purpose**: Application use cases that the Presentation layer depends on.

**⚠️ CRITICAL**: Phases 3–5 cannot start until this phase is complete.

- [X] T003 [P] Create `RefreshFeedSubscriptionCommand` record in `src/RSSFeedReader.Application/UseCases/RefreshFeedSubscription/RefreshFeedSubscriptionCommand.cs`
- [X] T004 [P] Create `MarkArticleAsReadCommand` record in `src/RSSFeedReader.Application/UseCases/MarkArticleAsRead/MarkArticleAsReadCommand.cs`
- [X] T005 [P] Create `ToggleArticleReadStatusCommand` record in `src/RSSFeedReader.Application/UseCases/ToggleArticleReadStatus/ToggleArticleReadStatusCommand.cs`
- [X] T006 Implement `RefreshFeedSubscriptionHandler` in `src/RSSFeedReader.Application/UseCases/RefreshFeedSubscription/RefreshFeedSubscriptionHandler.cs` (depends on T003)
- [X] T007 [P] Implement `MarkArticleAsReadHandler` in `src/RSSFeedReader.Application/UseCases/MarkArticleAsRead/MarkArticleAsReadHandler.cs` (depends on T004)
- [X] T008 [P] Implement `ToggleArticleReadStatusHandler` in `src/RSSFeedReader.Application/UseCases/ToggleArticleReadStatus/ToggleArticleReadStatusHandler.cs` (depends on T005)
- [X] T009 Add `GetArticlesByFeedHandler` in `src/RSSFeedReader.Application/UseCases/GetArticlesByFeed/GetArticlesByFeedHandler.cs` + command (articles list for ArticleListPage)
- [X] T010 [P] Write unit tests: `RefreshFeedSubscriptionHandlerTests` in `tests/RSSFeedReader.Application.Tests/UseCases/RefreshFeedSubscriptionHandlerTests.cs`
- [X] T011 [P] Write unit tests: `MarkArticleAsReadHandlerTests` in `tests/RSSFeedReader.Application.Tests/UseCases/MarkArticleAsReadHandlerTests.cs`
- [X] T012 [P] Write unit tests: `ToggleArticleReadStatusHandlerTests` in `tests/RSSFeedReader.Application.Tests/UseCases/ToggleArticleReadStatusHandlerTests.cs`
- [X] T013 [P] Write integration tests: `ArticleRepositoryReadStatusTests` in `tests/RSSFeedReader.Infrastructure.Tests/Persistence/ArticleRepositoryReadStatusTests.cs`
- [X] T014 Register all new handlers in `src/RSSFeedReader.Presentation/MauiProgram.cs`

**Checkpoint**: `dotnet test` for Application.Tests and Infrastructure.Tests must pass before Phase 3.

---

## Phase 3: User Story 1 — See Which Feeds Have Unread Content (P1) 🎯 MVP

**Goal**: Badge appears on feeds with unread articles; absent when count is zero. Verified on app launch.

**Independent Test**: Launch with a mix of feeds (some with unread articles, some without). Feeds with unread articles show a number badge; feeds at zero show none.

- [X] T015 [US1] Add `UpdateUnreadCount(Guid feedId, int newCount)` method to `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [X] T016 [US1] Update `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml` feed item template: add a third `Auto` column with a Refresh icon button; verify badge Label binding is correct

**Checkpoint**: Badge label shows correct count; hidden at zero; Refresh button visible per feed.

---

## Phase 4: User Story 2 — Badge Updates After Feed Refresh (P2)

**Goal**: Tapping a feed's Refresh button fetches new articles and updates that feed's badge in-place.

**Independent Test**: Tap Refresh on a feed. If new articles exist, badge increments immediately. No navigate-away required.

- [X] T017 [US2] Add `RefreshFeedCommand` (ICommand, takes FeedDto as parameter) to `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [X] T018 [US2] Wire `RefreshFeedCommand` to Refresh button `CommandParameter="{Binding .}"` in `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`
- [X] T019 [US2] Implement `RefreshFeedCommand` execute logic in `FeedListViewModel`: call `RefreshFeedSubscriptionHandler`, replace `FeedDto` at index using `with { UnreadCount, LastRefreshedAt }`

**Checkpoint**: Tapping Refresh updates badge in-place; status message shown on failure.

---

## Phase 5: User Story 3 — Badge Updates After Marking Articles as Read (P3)

**Goal**: User taps a feed to open its article list. Toggling an article's read status immediately updates that feed's badge on the feed list.

**Independent Test**: Navigate to an article list, toggle one article as read, navigate back — badge count decremented.

- [X] T020 [US3] Create `ArticleListViewModel` in `src/RSSFeedReader.Presentation/ViewModels/ArticleListViewModel.cs` (loads articles, exposes ToggleReadCommand, holds parent FeedListViewModel ref)
- [X] T021 [US3] Create `ArticleListPage.xaml` in `src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml` (CollectionView with title, date, IsRead indicator, toggle button)
- [X] T022 [US3] Create `ArticleListPage.xaml.cs` in `src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml.cs`
- [X] T023 [US3] Add `NavigateToArticlesCommand` to `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs` (navigates to ArticleListPage passing feedId + self reference)
- [X] T024 [US3] Register ArticleList Shell route in `src/RSSFeedReader.Presentation/AppShell.xaml` and `AppShell.xaml.cs`
- [X] T025 [US3] Register `ArticleListViewModel` and `ArticleListPage` as Transient in `src/RSSFeedReader.Presentation/MauiProgram.cs`
- [X] T026 [US3] Wire feed cell tap to `NavigateToArticlesCommand` in `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`

**Checkpoint**: Tapping a feed navigates to its article list; toggling read status updates badge on navigate-back.

---

## Phase 6: Polish & Cross-Cutting

- [X] T027 Run `dotnet build src/RSSFeedReader.Application --configuration Release` and fix any analyzer errors
- [X] T028 Run `dotnet build src/RSSFeedReader.Infrastructure --configuration Release` and fix any analyzer errors
- [X] T029 Run full test suite: `dotnet test` on all three test projects; all must pass
- [X] T030 Commit: `feat: implement unread badge (US1–US3) — per-feed refresh, article list, toggle read status`

---

## Dependencies & Execution Order

```
Phase 1 (T001–T002) → Phase 2 (T003–T014) → Phase 3 (T015–T016) → Phase 4 (T017–T019) → Phase 5 (T020–T026) → Phase 6 (T027–T030)
```

Within Phase 2, T003/T004/T005 are parallel; T007/T008 are parallel (after T004/T005); T010/T011/T012/T013 are parallel.

---

## Implementation Strategy

### Full delivery (all stories)

1. Phase 1 + 2 (foundation) → 2. Phase 3 (badge display, P1 MVP) → 3. Phase 4 (refresh updates badge, P2) → 4. Phase 5 (mark-as-read updates badge, P3) → 5. Phase 6 (build + test + commit)

### MVP only (US1)

Complete Phases 1–3 and skip Phases 4–5 for a badge-only deliverable without refresh or article navigation.
