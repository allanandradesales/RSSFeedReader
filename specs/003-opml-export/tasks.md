# Tasks: OPML Export

**Input**: Design documents from `/specs/003-opml-export/`
**Branch**: `003-opml-export`

---

## Phase 1: Domain + Application (Foundational)

- [X] T001 Create `IOpmlFileExporter` interface in `src/RSSFeedReader.Domain/Interfaces/Services/IOpmlFileExporter.cs`
- [X] T002 [P] Create `ExportSubscriptionsAsOpmlCommand` record in `src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlCommand.cs`
- [X] T003 Implement `ExportSubscriptionsAsOpmlHandler` in `src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlHandler.cs` (depends on T001, T002)
- [X] T004 Write unit tests: `ExportSubscriptionsAsOpmlHandlerTests` in `tests/RSSFeedReader.Application.Tests/UseCases/ExportSubscriptionsAsOpmlHandlerTests.cs` (depends on T003)

**Checkpoint**: `dotnet test tests/RSSFeedReader.Application.Tests` must pass.

---

## Phase 2: Infrastructure

- [X] T005 Implement `DownloadsOpmlFileExporter` in `src/RSSFeedReader.Infrastructure/OpmlExport/DownloadsOpmlFileExporter.cs` (depends on T001)

---

## Phase 3: Presentation

- [X] T006 Register `IOpmlFileExporter → DownloadsOpmlFileExporter` and `ExportSubscriptionsAsOpmlHandler` in `src/RSSFeedReader.Presentation/MauiProgram.cs`
- [X] T007 Add `ExportFeedsCommand` to `src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs`
- [X] T008 Add Export `ToolbarItem` to `src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml`

---

## Phase 4: Polish & Commit

- [X] T009 Build `src/RSSFeedReader.Application` and `src/RSSFeedReader.Infrastructure` (Release) — fix any analyzer errors
- [X] T010 Run full test suite on all three test projects; all must pass
- [X] T011 Commit: `feat: implement OPML export — US1/US2/US3 (FR-001–FR-010)`

---

## Dependencies & Execution Order

```
T001 → T003 → T004
T002 → T003
T001 → T005
T003 → T006, T007, T008
T006, T007, T008 → T009 → T010 → T011
```
