# Data Model: MVP RSS Feed Reader

**Branch**: `001-mvp-rss-reader`
**Phase**: 1 — Design
**Date**: 2026-02-27
**Source**: spec.md entities + research.md decisions

---

## Entities

### Feed

Represents a content source the user has subscribed to.

| Field | Type | Nullable | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | `Guid` | No | PK, auto-generated | `Guid.NewGuid()` on creation |
| `Url` | `string` | No | Unique, max 2048 chars | Canonical URL after redirect resolution; HTTPS preferred |
| `Title` | `string` | No | Max 256 chars | Fetched from feed `<title>` / `<feed><title>` metadata |
| `LastRefreshedAt` | `DateTimeOffset?` | Yes | — | `null` until first successful refresh |
| `CreatedAt` | `DateTimeOffset` | No | Default: `now()` | Timestamp of subscription creation |

**Indexes**:
- `PK_Feeds` — `Id` (primary key)
- `IX_Feeds_Url` — `Url` (unique)

**Business rules**:
- `Url` is the identity key for deduplication — two subscriptions to the same canonical URL
  are rejected (FR-006)
- `Title` is always sourced from feed metadata, never user-provided
- `LastRefreshedAt` updates only on successful full parse — a partial/timeout refresh does
  not update this field

---

### Article

An individual piece of content published by a feed.

| Field | Type | Nullable | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | `Guid` | No | PK, auto-generated | |
| `FeedId` | `Guid` | No | FK → `Feed.Id` (cascade delete) | Owning feed |
| `FeedGuid` | `string` | No | Unique, max 2048 chars | Feed's own `<guid>` or `<id>` element; fallback: `OriginalUrl` |
| `Title` | `string` | No | Max 512 chars | Article headline |
| `Summary` | `string?` | Yes | Max 2048 chars | Plain-text excerpt or description snippet |
| `Content` | `string?` | Yes | Unbounded | Sanitized HTML body (post-`HtmlSanitizer`); `null` if feed provides none |
| `OriginalUrl` | `string` | No | Max 2048 chars | Link to the original article on the web |
| `PublishedAt` | `DateTimeOffset` | No | — | From feed `<pubDate>` / `<published>`; defaults to `FetchedAt` if absent |
| `FetchedAt` | `DateTimeOffset` | No | Default: `now()` | When the article was stored locally |
| `IsRead` | `bool` | No | Default: `false` | Read/unread tracking state |

**Indexes**:
- `PK_Articles` — `Id` (primary key)
- `IX_Articles_FeedGuid` — `FeedGuid` (unique) — primary deduplication key
- `IX_Articles_FeedId_PublishedAt` — `(FeedId, PublishedAt DESC)` — fast filtered sorted list
- `IX_Articles_PublishedAt` — `PublishedAt DESC` — fast global sorted list
- `IX_Articles_FeedId_IsRead` — `(FeedId, IsRead)` — fast unread count per feed

**Business rules**:
- `FeedGuid` uniqueness is enforced at the database level; the application performs a
  batch existence check before insert to avoid round-trips (see research.md § Decision 5)
- `Content` is always stored post-sanitization; raw HTML from the feed is never persisted
- Deleting a `Feed` cascades to delete all its `Article` rows (FR-030)
- `IsRead` transitions: `false → true` on article open (FR-023); bidirectional toggle
  available to the user (FR-024)

---

## Relationships

```
Feed (1) ──────────────── (*) Article
      [Id]               [FeedId FK]
      cascade delete
```

- One `Feed` has zero or more `Article` rows
- An `Article` belongs to exactly one `Feed`
- Deleting a `Feed` removes all associated `Article` rows

---

## State Transitions

### Article.IsRead

```
[false] ──── open article (FR-023) ────► [true]
             OR manual toggle (FR-024)

[true]  ──── manual toggle (FR-024) ───► [false]
```

No other state exists. `IsRead` is a simple boolean with no intermediate states.

---

## Validation Rules

### Feed.Url
- MUST match `https?://` scheme (HTTP allowed with user warning per FR-003; HTTPS preferred)
- MUST NOT exceed 2048 characters (FR-005)
- MUST NOT resolve to a private IP range (SSRF prevention — research.md § Decision 3)
- MUST NOT be a duplicate of an existing `Feed.Url` (FR-006)
- After 301/302 redirect, the final destination URL is the value stored (FR-007)

### Article.FeedGuid
- Derived from the feed item's `<guid>` (RSS) or `<id>` (Atom) element
- If the feed item lacks a GUID element, `OriginalUrl` is used as the fallback value
- Uniqueness is global across all feeds (a cross-feed duplicate is an edge case but is
  handled gracefully by skipping the insert)

### Article.Content
- MUST be sanitized via `IContentSanitizerService` before storage
- Raw feed HTML is never written to the database
- `null` is stored when the feed provides no content body (FR-022 shows "No content available")

---

## EF Core Configuration Notes

```
AppDbContext
├── DbSet<Feed>    Feeds
└── DbSet<Article> Articles

Feed entity:
- HasKey(f => f.Id)
- HasIndex(f => f.Url).IsUnique()
- Property(f => f.Url).HasMaxLength(2048)
- Property(f => f.Title).HasMaxLength(256)

Article entity:
- HasKey(a => a.Id)
- HasIndex(a => a.FeedGuid).IsUnique()
- HasIndex(a => new { a.FeedId, a.PublishedAt })
- HasIndex(a => a.PublishedAt)
- HasIndex(a => new { a.FeedId, a.IsRead })
- HasOne<Feed>().WithMany().HasForeignKey(a => a.FeedId).OnDelete(DeleteBehavior.Cascade)
- Property(a => a.IsRead).HasDefaultValue(false)
- Property(a => a.Content) — no max length (sanitized HTML body)
```

---

## Migration Strategy

- Migrations live in `RSSFeedReader.Infrastructure/Persistence/Migrations/`
- Applied via `context.Database.MigrateAsync()` in `MauiProgram.cs` on every app startup
  (idempotent; creates DB file if absent)
- Initial migration creates both tables and all indexes above
- Future schema changes (new columns, new entities) get additional numbered migrations;
  no destructive migrations without a data migration companion
