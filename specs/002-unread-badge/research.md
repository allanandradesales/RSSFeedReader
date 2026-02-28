# Research: Unread Article Count Badge

## Decision 1: ObservableCollection update — item replacement vs. full reload

**Decision**: Replace the specific `FeedDto` record at its index in `ObservableCollection<FeedDto>` using the `with` expression after a refresh or toggle, rather than calling `LoadFeedsAsync()` (which clears and rebuilds the whole list).

**Rationale**: `ObservableCollection<T>` raises `CollectionChanged` with `NotifyCollectionChangedAction.Replace` when `collection[idx] = newValue` is used — MAUI updates only the affected cell. `FeedDto` is an immutable positional record so a new instance is created: `Feeds[idx] = Feeds[idx] with { UnreadCount = newCount, LastRefreshedAt = refreshedAt }`. Full reload resets scroll position and issues N×2 SQL queries (feeds + unread count per feed) for a change to a single item.

**Pattern**:
```csharp
var idx = Feeds.IndexOf(Feeds.FirstOrDefault(f => f.Id == feedId)!);
if (idx >= 0)
    Feeds[idx] = Feeds[idx] with { UnreadCount = newCount };
```

**Alternatives considered**:
- Full list reload (`Feeds.Clear()` + re-add): Simpler but resets scroll, triggers all cells to re-render, and issues redundant DB queries.
- Mutable `FeedItemViewModel` wrapper: More idiomatic MVVM for heavy scenarios; unnecessary complexity here where FeedDto already carries everything needed.

---

## Decision 2: Real-time badge update after mark-as-read (US3)

**Decision**: `ArticleListViewModel` calls `_feedListViewModel.UpdateUnreadCount(feedId, newCount)` after each toggle. `FeedListViewModel` exposes `UpdateUnreadCount(Guid feedId, int newCount)` as a public method that performs the index-replacement described in Decision 1.

**Rationale**: The spec requires the badge to update "within the same interaction". MAUI Shell `OnAppearing` (called when navigating back) already triggers `LoadFeedsAsync`, giving correct lazy-refresh for free. For updates visible *while the user is on ArticleListPage*, the ViewModel-to-ViewModel call is the lightest correct approach.

**`FeedListViewModel` registration as Transient**: MAUI DI resolves `ArticleListViewModel` as Transient too; passing the parent `FeedListViewModel` reference is achieved by resolving it at navigation time and injecting it through the `ArticleListViewModel` constructor (not a `[QueryProperty]`).

**Concrete flow**:
1. `FeedListPage` taps a feed → navigates to `ArticleListPage` passing `feedId` and the resolved `FeedListViewModel` instance.
2. `ArticleListViewModel` holds a reference to the parent VM.
3. On each toggle, handler returns new `IsRead` + new unread count; ViewModel calls `_feedListVm.UpdateUnreadCount(feedId, newCount)`.
4. On navigate-back, `FeedListPage.OnAppearing` re-runs `LoadFeedsAsync` as a final accuracy pass.

**Alternatives considered**:
- `WeakReferenceMessenger` (CommunityToolkit.Mvvm): Clean pub/sub; adds a NuGet dependency not currently in the project — rejected.
- Singleton `FeedListViewModel`: Avoids the reference-passing problem but singletons in MAUI navigation can cause stale state across tab switches — rejected for this simple case.

---

## Decision 3: Per-feed refresh trigger (US2)

**Decision**: Add a Refresh icon button (`↺`) as a trailing column in each feed list cell in `FeedListPage.xaml`. Tapping it calls a `RefreshFeedCommand` on `FeedListViewModel` passing the `FeedDto` as `CommandParameter`.

**Rationale**: Most discoverable on desktop. No new gesture library needed. Fits the existing `Grid ColumnDefinitions="*,Auto"` cell layout — add a third column `Auto` for the refresh button.

**Alternatives considered**:
- `SwipeView` (left-swipe reveals Refresh): Works on touch; inconsistent on MacCatalyst with mouse. Spec says "tap" so an explicit button is correct.
- Global "Refresh All" only: Doesn't satisfy "a user manually triggers a *feed* refresh" — single-feed granularity is implied by US2 acceptance scenario 1 (one feed gets new articles while others don't change).

---

## Decision 4: No EF Core migration required

**Decision**: `Article.IsRead` (BOOLEAN, default false) is already in the `InitialCreate` migration. `MarkAsReadAsync` and `ToggleReadStatusAsync` are already implemented in `ArticleRepository`. `GetUnreadCountByFeedIdAsync` is already implemented. No schema changes needed.

**Rationale**: The MVP implementation fully anticipated the read-status feature. This feature is purely Application + Presentation layer work.

---

## Decision 5: ArticleListPage Shell navigation

**Decision**: Register `ArticleList` as a Shell route in `AppShell.xaml.cs` code-behind. Navigate via `Shell.Current.GoToAsync("ArticleList", parameters)` passing `feedId` (string form of Guid) as a query parameter. `ArticleListViewModel` receives it via `[QueryProperty("FeedId", "feedId")]`.

**Rationale**: Standard MAUI Shell relative navigation with query properties. No global state required. The `FeedListViewModel` reference is passed as a non-string parameter via `ShellNavigationQueryParameters` dictionary (supported since MAUI .NET 7).

**Pattern**:
```csharp
// In FeedListPage.xaml.cs (or FeedListViewModel):
await Shell.Current.GoToAsync("ArticleList", new ShellNavigationQueryParameters
{
    ["feedId"] = feed.Id.ToString(),
    ["parentVm"] = _viewModel, // passed as object
});
```

**Alternatives considered**:
- Passing entire FeedDto as serialised query string: Not supported; `[QueryProperty]` only works with string/primitives for automatic injection; object passing requires `ShellNavigationQueryParameters`.
- Global `CurrentFeedService` singleton: Avoided because it creates hidden shared state.
