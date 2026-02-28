# Implementation Plan: OPML Export

**Branch**: `003-opml-export` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-opml-export/spec.md`

---

## Technical Context

- **Language / Runtime**: C# 12 / .NET 8 LTS + .NET MAUI
- **XML generation**: `System.Xml.Linq.XDocument` — BCL, no NuGet needed
- **File I/O**: `System.IO.File.WriteAllTextAsync` (atomic via temp-file pattern)
- **Downloads path**: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")` — works on macOS and Windows
- **No new NuGet packages**
- **No schema / migration changes** — export is read-only against existing `Feed` entities

---

## Constitution Check

| Gate | Result | Notes |
|------|--------|-------|
| Clean Architecture dependency rule | PASS | Domain ← Application ← Infrastructure ← Presentation |
| No circular dependencies | PASS | IOpmlFileExporter in Domain, implemented in Infrastructure |
| Nullable reference types | PASS | All new code uses `<Nullable>enable</Nullable>` |
| Analyzer compliance | PASS | Named constants; XML doc on public APIs; no magic values |
| Feature parity with spec | PASS | FR-001–FR-010 all addressed |

---

## Project Structure

### New files

```text
src/RSSFeedReader.Domain/Interfaces/Services/IOpmlFileExporter.cs
src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlCommand.cs
src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlHandler.cs
src/RSSFeedReader.Infrastructure/OpmlExport/DownloadsOpmlFileExporter.cs
tests/RSSFeedReader.Application.Tests/UseCases/ExportSubscriptionsAsOpmlHandlerTests.cs
```

### Updated files

```text
src/RSSFeedReader.Presentation/MauiProgram.cs                       — register handler + service
src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs      — add ExportFeedsCommand
src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml              — add Export ToolbarItem
```

---

## Key Decisions

1. **Handler generates OPML XML; Infrastructure writes file** — `ExportSubscriptionsAsOpmlHandler` builds the XML string then delegates disk I/O to `IOpmlFileExporter`. XML generation is unit-testable by capturing the Moq argument.
2. **Atomic write** — `DownloadsOpmlFileExporter` writes to a `.tmp` file first, then `File.Move(tmp, target, overwrite: true)`. On failure, the tmp is cleaned in `finally`; the target is never partially written (satisfies FR-008).
3. **Empty list = no file** — handler short-circuits with `NoSubscriptions` result before touching disk (satisfies FR-009).
4. **Special character encoding** — `XAttribute` values are automatically XML-escaped by `System.Xml.Linq` (satisfies FR-010).
5. **OPML 2.0 format** — `<opml version="2.0">` with RFC 1123 `dateCreated` in `<head>`; one `<outline type="rss" text="..." xmlUrl="...">` per feed.
6. **Export button** — `ToolbarItem` on `FeedListPage` bound to `ExportFeedsCommand` on `FeedListViewModel`. Appears in the Shell navigation bar on macOS/Windows.
