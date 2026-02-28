# Contract: IFeedFetcherService

**Layer**: Domain → implemented by Infrastructure
**File**: `RSSFeedReader.Domain/Interfaces/Services/IFeedFetcherService.cs`
**Date**: 2026-02-27

## Purpose

Abstracts the external feed-fetching concern: URL validation (SSRF guard), HTTP request,
redirect resolution, feed parsing (RSS 2.0 / Atom 1.0), and HTML content sanitization.
Returns a typed result that separates success from failure without throwing for expected
error conditions (unreachable feeds, parse errors, timeouts).

## Result Type

```csharp
public sealed record FeedFetchResult
{
    // Success path
    public string? CanonicalUrl { get; init; }     // Final URL after redirects
    public string? FeedTitle { get; init; }         // Feed-level <title>
    public IReadOnlyList<ParsedArticle>? Articles { get; init; }

    // Error path
    public bool IsSuccess { get; init; }
    public FeedFetchError? Error { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum FeedFetchError
{
    InvalidUrl,           // Scheme/format/length validation failed
    SsrfBlocked,          // URL resolved to a private IP range
    SelfSignedCertificate,// TLS validation failed (self-signed)
    HttpError,            // 4xx / 5xx response
    Timeout,              // 10-second per-feed timeout exceeded
    ParseError,           // Response is not valid RSS 2.0 or Atom 1.0
    NotAFeed,             // HTTP 200 but content is not a feed (HTML page, etc.)
}

public sealed record ParsedArticle
{
    public string FeedGuid { get; init; } = string.Empty; // <guid> or <id>; fallback: Link
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? SanitizedContent { get; init; }        // Post-HtmlSanitizer HTML
    public string OriginalUrl { get; init; } = string.Empty;
    public DateTimeOffset PublishedAt { get; init; }
}
```

## Interface

```csharp
public interface IFeedFetcherService
{
    /// <summary>
    /// Validates the URL, fetches the feed, follows redirects, parses RSS/Atom,
    /// and sanitizes article HTML content.
    ///
    /// Never throws for expected error conditions (network, parse, SSRF).
    /// Returns FeedFetchResult.IsSuccess = false with an appropriate FeedFetchError.
    ///
    /// Enforces a hard 10-second timeout per Constitution § IV.
    /// </summary>
    Task<FeedFetchResult> FetchAsync(string url, CancellationToken ct = default);
}
```

## Behavior Contracts

| Scenario | `IsSuccess` | `Error` | Notes |
|----------|-------------|---------|-------|
| Valid feed, HTTP 200 | `true` | `null` | `CanonicalUrl` is the post-redirect URL |
| 301/302 redirect to valid feed | `true` | `null` | `CanonicalUrl` is the final destination |
| Non-HTTP/HTTPS scheme | `false` | `InvalidUrl` | Caught pre-request, no DNS call made |
| URL > 2048 chars | `false` | `InvalidUrl` | Caught pre-request |
| URL resolves to private IP | `false` | `SsrfBlocked` | DNS resolution + IP check |
| Self-signed TLS certificate | `false` | `SelfSignedCertificate` | TLS handshake rejected |
| HTTP 404 / 500 | `false` | `HttpError` | Response code in `ErrorMessage` |
| 10-second timeout | `false` | `Timeout` | `OperationCanceledException` caught |
| Valid HTTP 200 but HTML page | `false` | `NotAFeed` | Content-type or parse failure |
| Feed XML malformed | `false` | `ParseError` | `XmlException` caught |

## Internal Responsibilities (Infrastructure implementation only)

The `FeedFetcherService` (Infrastructure) MUST:

1. Apply `SsrfGuard.Validate(url)` before any `HttpClient` call
2. Use the named `HttpClient` ("RssFeedClient") registered in DI with:
   - `Timeout = TimeSpan.FromSeconds(10)`
   - `AllowAutoRedirect = true`, `MaxAutomaticRedirections = 5`
3. Capture `response.RequestMessage!.RequestUri!.AbsoluteUri` as `CanonicalUrl`
4. Re-apply `SsrfGuard.Validate(canonicalUrl)` after redirect
5. Parse with `System.ServiceModel.Syndication.SyndicationFeed`
6. Sanitize each item's `Content` via `IContentSanitizerService.Sanitize()` before
   populating `ParsedArticle.SanitizedContent`
7. Return `FeedFetchResult` — never `throw` for any expected error condition

## Notes

- The result type uses `sealed record` to prevent inheritance and allow pattern matching
  in use-case handlers.
- `CancellationToken` is passed from the use-case handler to support user-initiated
  cancellation of a refresh-all operation.
- `FeedFetchError` enum values map 1:1 to user-visible error categories in the Presentation
  layer; the ViewModel translates them into localized UI strings.
