# Contract: IContentSanitizerService

**Layer**: Domain → implemented by Infrastructure
**File**: `RSSFeedReader.Domain/Interfaces/Services/IContentSanitizerService.cs`
**Date**: 2026-02-27

## Purpose

Abstracts HTML sanitization for article content fetched from RSS/Atom feeds. Ensures
that all content stored in the database and rendered in the UI is free of executable
scripts, event handlers, and external tracking elements.

## Interface

```csharp
public interface IContentSanitizerService
{
    /// <summary>
    /// Sanitizes raw HTML from a feed article using the project's approved allowlist.
    ///
    /// Input:  raw HTML string from feed (may be null or empty)
    /// Output: sanitized HTML string safe for rendering, or null if input is null/empty
    ///
    /// Never throws. Malformed HTML is sanitized as best-effort.
    /// </summary>
    string? Sanitize(string? rawHtml);
}
```

## Behavior Contracts

| Input | Output | Notes |
|-------|--------|-------|
| `null` | `null` | Caller stores `null` → UI shows "No content available" |
| `""` (empty) | `null` | Treated as absent content |
| Valid HTML with safe tags only | Unchanged HTML | Pass-through for already-safe content |
| HTML with `<script>` | `<script>` stripped | All script tags removed |
| HTML with `on*` attributes | Attributes stripped | `onclick`, `onload`, etc. removed |
| HTML with `javascript:` href | `href` stripped / tag removed | `<a href="javascript:...">` neutralized |
| HTML with `<iframe>` | Tag stripped | Not in allowlist |
| HTML with external tracking pixel | `<img>` stripped | 1×1 pixel from analytics domain |
| Malformed HTML | Best-effort sanitization | No exception thrown |

## Allowlist (from Constitution § Security Constraints)

**Allowed tags**: `p`, `a`, `img`, `ul`, `ol`, `li`, `h1`, `h2`, `h3`, `h4`, `h5`,
`h6`, `blockquote`, `code`, `pre`

**Allowed attributes per tag**:

| Tag | Allowed attributes |
|-----|--------------------|
| `a` | `href` (HTTPS URLs or relative paths only; `javascript:` stripped) |
| `img` | `src` (HTTPS URLs only; `data:` URIs stripped), `alt`, `title` |
| All others | `title` |

**Always stripped** (regardless of tag):
- All event attributes: `on*` (`onclick`, `onmouseover`, `onerror`, etc.)
- `style` attribute (CSS injection vector)
- `data-*` attributes
- `javascript:` scheme in any attribute value

## Implementation Note (Infrastructure only)

The `HtmlSanitizerAdapter` (Infrastructure) wraps the `HtmlSanitizer` NuGet package
(Ganss). The adapter is the only class in the solution that references the NuGet package
directly, keeping Domain and Application free of the dependency.

The adapter MUST be registered as a singleton in DI — `HtmlSanitizer` configuration is
thread-safe after initialization.

## Notes

- This service is synchronous (`string? Sanitize(string? rawHtml)`) — `HtmlSanitizer`
  is CPU-bound with no I/O; `async` overhead is unnecessary.
- Called by `FeedFetcherService` (Infrastructure) before populating `ParsedArticle.SanitizedContent`,
  ensuring sanitization happens before data reaches Application or Domain layers.
- The return value is stored directly in `Article.Content` — no further sanitization is
  applied at render time (trust the stored value).
