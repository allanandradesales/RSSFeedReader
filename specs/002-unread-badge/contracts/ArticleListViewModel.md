# Contract: ArticleListViewModel

## Purpose

Backs the `ArticleListPage`. Loads articles for a specific feed, exposes a toggle-read command per article, and notifies the parent `FeedListViewModel` to update the badge in-place after each toggle.

---

## Navigation parameters received

| Parameter   | Type              | How received                              |
|-------------|-------------------|-------------------------------------------|
| `feedId`    | string (Guid)     | `[QueryProperty("FeedId", "feedId")]`     |
| `parentVm`  | FeedListViewModel | `ShellNavigationQueryParameters` object   |

---

## Public surface

```csharp
public sealed class ArticleListViewModel : INotifyPropertyChanged
{
    // Navigation-injected
    public string FeedId { get; set; }   // [QueryProperty]

    // Bindable state
    public string FeedTitle { get; }
    public ObservableCollection<ArticleItemViewModel> Articles { get; }
    public bool IsBusy { get; }
    public string StatusMessage { get; }

    // Commands
    public ICommand LoadArticlesCommand { get; }
    public ICommand ToggleReadCommand { get; }   // CommandParameter: ArticleItemViewModel

    // Called by page on navigation
    public Task LoadArticlesAsync();
}
```

### ArticleItemViewModel (inner)

```csharp
public sealed class ArticleItemViewModel : INotifyPropertyChanged
{
    public Guid Id { get; }
    public Guid FeedId { get; }
    public string Title { get; }
    public string? Summary { get; }
    public string OriginalUrl { get; }
    public DateTimeOffset PublishedAt { get; }
    public bool IsRead { get; set; }   // raises PropertyChanged
}
```

---

## Badge update contract

After `ToggleReadCommand` executes:
1. `ToggleArticleReadStatusHandler.HandleAsync` returns `(newIsRead, newUnreadCount)`.
2. `ArticleItemViewModel.IsRead` is updated to `newIsRead`.
3. `_feedListVm.UpdateUnreadCount(feedId, newUnreadCount)` is called, which replaces the `FeedDto` at its index in the parent ObservableCollection.

`FeedListViewModel.UpdateUnreadCount` signature:
```csharp
public void UpdateUnreadCount(Guid feedId, int newCount);
```
