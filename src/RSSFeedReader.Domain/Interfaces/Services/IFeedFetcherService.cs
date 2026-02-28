using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Domain.Interfaces.Services;

/// <summary>Describes why a feed fetch operation failed.</summary>
public enum FeedFetchError
{
    /// <summary>The supplied URL is not a valid absolute HTTP/HTTPS URL.</summary>
    InvalidUrl,

    /// <summary>The resolved IP address falls within a private/loopback range (SSRF prevention).</summary>
    SsrfBlocked,

    /// <summary>The server presented a self-signed or otherwise untrusted TLS certificate.</summary>
    SelfSignedCertificate,

    /// <summary>The server returned a non-success HTTP status code.</summary>
    HttpError,

    /// <summary>The request exceeded the 10-second timeout.</summary>
    Timeout,

    /// <summary>The response body could not be parsed as RSS 2.0 or Atom 1.0.</summary>
    ParseError,

    /// <summary>The response body is valid XML but does not represent a feed.</summary>
    NotAFeed,
}

/// <summary>Result returned by <see cref="IFeedFetcherService.FetchAsync"/>.</summary>
public sealed record FeedFetchResult
{
    /// <summary>Gets the resolved feed title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the articles discovered in the feed.</summary>
    public required IReadOnlyList<Article> Articles { get; init; }

    /// <summary>Creates a successful result.</summary>
    public static FeedFetchResult Success(string title, IReadOnlyList<Article> articles) =>
        new() { Title = title, Articles = articles };
}

/// <summary>Fetches and parses an RSS or Atom feed from a remote URL.</summary>
public interface IFeedFetcherService
{
    /// <summary>
    /// Fetches and parses the feed at <paramref name="url"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="FeedFetchResult"/> on success, or a <see cref="FeedFetchError"/> discriminant on failure.
    /// </returns>
    Task<Result<FeedFetchResult, FeedFetchError>> FetchAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>Non-generic factory for <see cref="Result{TValue,TError}"/> to satisfy CA1000.</summary>
public static class Result
{
    /// <summary>Creates a success result.</summary>
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) => Result<TValue, TError>.CreateOk(value);

    /// <summary>Creates an error result.</summary>
    public static Result<TValue, TError> Fail<TValue, TError>(TError error) => Result<TValue, TError>.CreateFail(error);
}

/// <summary>A discriminated union representing either a success value or an error value.</summary>
/// <typeparam name="TValue">The success type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    private Result(TValue value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(TError error)
    {
        _error = error;
        IsSuccess = false;
    }

    /// <summary>Gets whether this result represents a success.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the success value. Only valid when <see cref="IsSuccess"/> is <see langword="true"/>.</summary>
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is an error.");

    /// <summary>Gets the error value. Only valid when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public TError Error => !IsSuccess ? _error! : throw new InvalidOperationException("Result is a success.");

    internal static Result<TValue, TError> CreateOk(TValue value) => new(value);

    internal static Result<TValue, TError> CreateFail(TError error) => new(error);
}
