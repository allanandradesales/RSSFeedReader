# Contract: RefreshFeedSubscriptionHandler

## Purpose

Refreshes a single subscribed feed: fetches latest articles, upserts them, updates `LastRefreshedAt`, and returns the new unread article count so the presentation layer can update the badge in-place.

---

## Command

```csharp
public sealed record RefreshFeedSubscriptionCommand(Guid FeedId, string FeedUrl);
```

---

## Result

```csharp
public enum RefreshFeedError
{
    FeedNotFound,    // No Feed with the given FeedId exists in the database
    FetchFailed,     // FeedFetcherService returned an error
}

public sealed record RefreshFeedSubscriptionResult
{
    public int NewUnreadCount { get; init; }
    public DateTimeOffset? NewLastRefreshedAt { get; init; }
    public bool IsSuccess => Error is null;
    public RefreshFeedError? Error { get; init; }
    public FeedFetchError? FetchError { get; init; }

    public static RefreshFeedSubscriptionResult Ok(int count, DateTimeOffset refreshedAt);
    public static RefreshFeedSubscriptionResult NotFound();
    public static RefreshFeedSubscriptionResult Fail(FeedFetchError fetchError);
}
```

---

## Handler signature

```csharp
public sealed class RefreshFeedSubscriptionHandler
{
    public RefreshFeedSubscriptionHandler(
        IFeedRepository feedRepository,
        IArticleRepository articleRepository,
        IFeedFetcherService fetcher);

    public Task<RefreshFeedSubscriptionResult> HandleAsync(
        RefreshFeedSubscriptionCommand command,
        CancellationToken cancellationToken = default);
}
```

---

## Behaviour

1. Verify the feed exists in `IFeedRepository`; return `NotFound` if absent.
2. Call `IFeedFetcherService.FetchAsync(command.FeedUrl)`; return `Fail(fetchError)` if unsuccessful.
3. Stamp `FeedId` on each returned `Article`.
4. Call `IArticleRepository.UpsertManyAsync(articles)` (preserves `IsRead` on existing articles).
5. Call `IFeedRepository.UpdateLastRefreshedAtAsync(feedId, utcNow)`.
6. Call `IArticleRepository.GetUnreadCountByFeedIdAsync(feedId)` to get the new count.
7. Return `Ok(newUnreadCount, utcNow)`.

---

## Caller responsibilities

- Update the corresponding `FeedDto` in the `ObservableCollection` using:
  ```csharp
  Feeds[idx] = Feeds[idx] with { UnreadCount = result.NewUnreadCount, LastRefreshedAt = result.NewLastRefreshedAt };
  ```
- Display an error status message on `Fail`.
