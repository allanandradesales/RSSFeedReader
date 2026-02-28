# Contract: IFeedRepository

**Layer**: Domain → implemented by Infrastructure
**File**: `RSSFeedReader.Domain/Interfaces/Repositories/IFeedRepository.cs`
**Date**: 2026-02-27

## Purpose

Abstracts all persistence operations for `Feed` entities. The Application layer depends
on this interface exclusively — no direct `DbContext` or EF Core reference allowed in
Application or Domain.

## Interface

```csharp
public interface IFeedRepository
{
    /// <summary>Returns all subscribed feeds, ordered alphabetically by Title.</summary>
    Task<IReadOnlyList<Feed>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the feed with the given canonical URL, or null if not found.
    /// Used to check for duplicate subscriptions before adding.
    /// </summary>
    Task<Feed?> GetByUrlAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Persists a new Feed. Throws if Url already exists (duplicate check
    /// is the caller's responsibility, but the unique index is the final guard).
    /// </summary>
    Task<Feed> AddAsync(Feed feed, CancellationToken ct = default);

    /// <summary>
    /// Removes the feed and all its articles (cascade). No-op if not found.
    /// </summary>
    Task DeleteAsync(Guid feedId, CancellationToken ct = default);

    /// <summary>
    /// Stamps LastRefreshedAt on a feed after a successful refresh.
    /// Does not modify any other field.
    /// </summary>
    Task UpdateLastRefreshedAtAsync(
        Guid feedId,
        DateTimeOffset refreshedAt,
        CancellationToken ct = default);
}
```

## Behavior Contracts

| Method | Precondition | Postcondition | Error case |
|--------|-------------|---------------|------------|
| `GetAllAsync` | — | Returns 0..* feeds sorted A–Z by `Title` | Returns empty list if no subscriptions |
| `GetByUrlAsync` | `url` is non-null | Returns `Feed` or `null` | Returns `null` if not found (never throws) |
| `AddAsync` | `feed.Url` unique, `feed.Title` non-empty | Feed persisted, `Id` set | Throws if `Url` duplicate (unique constraint violation) |
| `DeleteAsync` | — | Feed + all its articles removed | No-op (no exception) if `feedId` not found |
| `UpdateLastRefreshedAtAsync` | Feed exists | `LastRefreshedAt` updated | No-op if `feedId` not found |

## Notes

- All methods accept an optional `CancellationToken` to support user-initiated cancellation
  during refresh operations.
- `GetAllAsync` returns `IReadOnlyList<T>` — callers MUST NOT cast to `IQueryable<T>` or
  attempt further EF Core query composition.
- Caller (Application use-case) is responsible for the duplicate URL check via
  `GetByUrlAsync` before calling `AddAsync`; the unique index is the final enforcement layer.
