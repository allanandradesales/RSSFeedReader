# Research: OPML Export

## Decision 1: XML generation in Application, file I/O in Infrastructure

**Decision**: `ExportSubscriptionsAsOpmlHandler` builds the OPML XML string using `System.Xml.Linq.XDocument` (BCL, no package needed) and passes it to `IOpmlFileExporter.SaveAsync(xml)`. The Infrastructure implementation handles disk I/O.

**Rationale**: XML generation is pure business logic (no external deps), so it belongs in Application. Disk I/O is infrastructure. Splitting them makes the handler fully unit-testable with `Mock<IOpmlFileExporter>` — capture the argument to verify OPML content.

---

## Decision 2: Atomic write via temp-file pattern

**Decision**: `DownloadsOpmlFileExporter` writes to `subscriptions.opml.tmp`, then calls `File.Move(tmp, target, overwrite: true)`. A `finally` block deletes the temp file if it still exists.

**Rationale**: FR-008 requires no partial/corrupt file. `File.Move` is atomic on most file systems. If the write fails, only the temp file is affected; the existing `subscriptions.opml` (if any) is untouched.

---

## Decision 3: Downloads folder path

**Decision**: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")`.

**Rationale**: This resolves to `~/Downloads` on macOS and `C:\Users\<name>\Downloads` on Windows — the standard user-accessible Downloads folder on both target platforms. No platform-conditional code needed.

---

## Decision 4: `ToolbarItem` for Export button

**Decision**: Add `<ContentPage.ToolbarItems><ToolbarItem Text="Export" Command="{Binding ExportFeedsCommand}" /></ContentPage.ToolbarItems>` to `FeedListPage.xaml`.

**Rationale**: MAUI Shell renders `ToolbarItem` in the native navigation bar on both macOS and Windows. It keeps the main content area clean and is discoverable at the page level. The command is already available via compiled binding (`x:DataType="vm:FeedListViewModel"`).

---

## Decision 5: IOpmlFileExporter interface in Domain layer

**Decision**: Interface placed at `src/RSSFeedReader.Domain/Interfaces/Services/IOpmlFileExporter.cs`, consistent with `IFeedFetcherService` and `IContentSanitizerService`.

**Rationale**: Application layer depends on Domain; Infrastructure implements Domain interfaces. Putting the interface in Domain gives the cleanest dependency graph.
