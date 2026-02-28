# Data Model: Unread Article Count Badge

## No schema changes required

The SQLite schema from `InitialCreate` migration fully covers this feature. All relevant columns and queries already exist.

---

## Existing entities used (no changes)

### Article

| Field        | Type            | Notes                                        |
|--------------|-----------------|----------------------------------------------|
| Id           | Guid PK         | Used in MarkAsRead / Toggle commands         |
| FeedId       | Guid FK         | FK → Feed; used to recalculate unread count  |
| FeedGuid     | string          | Deduplication key (unchanged)                |
| Title        | string          | Displayed in ArticleListPage                 |
| Summary      | string?         | Optional excerpt                             |
| OriginalUrl  | string          | "Open in browser" link                       |
| PublishedAt  | DateTimeOffset  | Sort key (newest-first)                      |
| FetchedAt    | DateTimeOffset  | When stored locally                          |
| **IsRead**   | **bool**        | **Toggled by this feature; default false**   |

### Feed

Unchanged. `LastRefreshedAt` is updated by `RefreshFeedSubscriptionHandler` after a successful fetch.

---

## Existing DTO (no changes)

### FeedDto

| Field             | Type             | Notes                                               |
|-------------------|------------------|-----------------------------------------------------|
| Id                | Guid             | Used to locate item in ObservableCollection         |
| Url               | string           |                                                     |
| Title             | string           |                                                     |
| LastRefreshedAt   | DateTimeOffset?  | Updated in place after refresh                      |
| **UnreadCount**   | **int**          | **Drives badge visibility and label**               |

`FeedDto` is an immutable positional record. Badge updates use `with` expression to replace the item at its ObservableCollection index.

---

## New DTO

### ArticleDto

| Field        | Type            | Notes                                                  |
|--------------|-----------------|--------------------------------------------------------|
| Id           | Guid            | Passed in MarkAsRead / Toggle commands                 |
| FeedId       | Guid            | Used to update parent feed's badge after toggle        |
| Title        | string          | Displayed in ArticleListPage                           |
| Summary      | string?         | Optional excerpt shown below title                     |
| OriginalUrl  | string          | "Open" action URL                                      |
| PublishedAt  | DateTimeOffset  | Display date; sort key                                 |
| IsRead       | bool            | Drives per-article read indicator and toggle command   |

**Mapped from**: `Article` entity by `GetArticlesByFeedHandler`.

---

## Existing repository methods used by new use cases

| Method                                    | Used by                          |
|-------------------------------------------|----------------------------------|
| `GetByFeedIdAsync(feedId)`                | GetArticlesByFeedHandler (new)   |
| `MarkAsReadAsync(articleId)`              | MarkArticleAsReadHandler (new)   |
| `ToggleReadStatusAsync(articleId)`        | ToggleArticleReadStatusHandler (new) |
| `GetUnreadCountByFeedIdAsync(feedId)`     | All three handlers (post-action) |
| `UpsertManyAsync(articles)`               | RefreshFeedSubscriptionHandler (new) |
| `UpdateLastRefreshedAtAsync(id, time)`    | RefreshFeedSubscriptionHandler (new) |
