# Contract: IArticleRepository

**Layer**: Domain → implemented by Infrastructure
**File**: `RSSFeedReader.Domain/Interfaces/Repositories/IArticleRepository.cs`
**Date**: 2026-02-27

## Purpose

Abstracts all persistence operations for `Article` entities, including read/unread state
management and bulk upsert during feed refresh.

## Interface

```csharp
public interface IArticleRepository
{
    /// <summary>
    /// Returns all articles across all feeds, sorted by PublishedAt descending (newest first).
    /// </summary>
    Task<IReadOnlyList<Article>> GetAllSortedAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all articles belonging to the specified feed,
    /// sorted by PublishedAt descending.
    /// </summary>
    Task<IReadOnlyList<Article>> GetByFeedIdAsync(
        Guid feedId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the count of unread articles for the specified feed.
    /// Used to display the unread badge in the subscription list (FR-026).
    /// </summary>
    Task<int> GetUnreadCountByFeedIdAsync(Guid feedId, CancellationToken ct = default);

    /// <summary>
    /// Inserts articles whose FeedGuid does not already exist in the database.
    /// Existing articles (matched by FeedGuid) are skipped — no update is performed.
    /// Deduplication is performed in a single batch query before insert.
    /// </summary>
    Task UpsertManyAsync(
        IEnumerable<Article> articles,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the specified article as read (IsRead = true).
    /// No-op if already read or if articleId not found.
    /// </summary>
    Task MarkAsReadAsync(Guid articleId, CancellationToken ct = default);

    /// <summary>
    /// Toggles IsRead: true → false, false → true.
    /// No-op if articleId not found.
    /// </summary>
    Task ToggleReadStatusAsync(Guid articleId, CancellationToken ct = default);

    /// <summary>
    /// Removes all articles belonging to the specified feed.
    /// Called as part of feed removal (cascade is handled at DB level,
    /// but this method exists for explicit use-case clarity).
    /// </summary>
    Task DeleteByFeedIdAsync(Guid feedId, CancellationToken ct = default);
}
```

## Behavior Contracts

| Method | Precondition | Postcondition | Error case |
|--------|-------------|---------------|------------|
| `GetAllSortedAsync` | — | Returns all articles, newest first | Returns empty list if none exist |
| `GetByFeedIdAsync` | — | Returns articles for feed, newest first | Returns empty list if feed has no articles |
| `GetUnreadCountByFeedIdAsync` | — | Returns count ≥ 0 | Returns 0 if feed not found |
| `UpsertManyAsync` | Articles have `FeedGuid` set | New articles inserted; duplicates skipped | Silently skips on unique constraint match |
| `MarkAsReadAsync` | — | `IsRead = true` for article | No-op if not found or already read |
| `ToggleReadStatusAsync` | — | `IsRead` flipped | No-op if not found |
| `DeleteByFeedIdAsync` | — | All articles for feed removed | No-op if feed has no articles |

## Deduplication Contract (UpsertManyAsync)

The implementation MUST perform deduplication in a single batch query:

1. Extract all `FeedGuid` values from the incoming `articles` collection
2. Query existing `FeedGuid` values from the database in one `WHERE IN (...)` query
3. Filter incoming articles to those whose `FeedGuid` is not in the result set
4. Batch-insert only the truly new articles

This avoids per-article round-trips and is safe under concurrent refresh via `SemaphoreSlim`
(see research.md § Decision 5).

## Notes

- `Content` stored in `Article` is always pre-sanitized; this repository never sanitizes —
  sanitization is the `IFeedFetcherService`'s responsibility before calling `UpsertManyAsync`.
- `GetAllSortedAsync` and `GetByFeedIdAsync` use `AsNoTracking()` for read performance.
- `UpsertManyAsync` acquires no locks itself — the caller (`RefreshFeedsHandler`) is
  responsible for serializing write operations via `SemaphoreSlim`.
