using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Xml;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Infrastructure.FeedFetcher;

/// <summary>Fetches and parses RSS 2.0 / Atom 1.0 feeds from remote URLs.</summary>
public sealed class FeedFetcherService : IFeedFetcherService
{
    private const int TimeoutSeconds = 10;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IContentSanitizerService _sanitizer;

    /// <summary>Initializes a new instance of <see cref="FeedFetcherService"/>.</summary>
    public FeedFetcherService(IHttpClientFactory httpClientFactory, IContentSanitizerService sanitizer)
    {
        _httpClientFactory = httpClientFactory;
        _sanitizer = sanitizer;
    }

    /// <inheritdoc/>
    public async Task<Result<FeedFetchResult, FeedFetchError>> FetchAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not "http" and not "https")
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.InvalidUrl);
        }

        var ssrfAllowed = await SsrfGuard.IsAllowedAsync(url, cancellationToken);
        if (!ssrfAllowed)
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.SsrfBlocked);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        string responseBody;
        try
        {
            var client = _httpClientFactory.CreateClient("FeedFetcher");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
                return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.HttpError);

            responseBody = await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.Timeout);
        }
        catch (HttpRequestException ex) when (IsSelfSignedCertificate(ex))
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.SelfSignedCertificate);
        }
        catch (HttpRequestException)
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.HttpError);
        }

        return ParseFeed(responseBody, url);
    }

    private Result<FeedFetchResult, FeedFetchError> ParseFeed(string xml, string feedUrl)
    {
        SyndicationFeed feed;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            feed = SyndicationFeed.Load(reader);
        }
        catch (XmlException)
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.ParseError);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.NotAFeed);
        }

        var title = feed.Title?.Text ?? feedUrl;
        var fetchedAt = DateTimeOffset.UtcNow;
        var feedId = Guid.Empty; // set by the caller after Feed is persisted

        var articles = feed.Items.Select(item => MapItem(item, feedId, fetchedAt)).ToList();

        return Result.Ok<FeedFetchResult, FeedFetchError>(FeedFetchResult.Success(title, articles));
    }

    private Article MapItem(SyndicationItem item, Guid feedId, DateTimeOffset fetchedAt)
    {
        var feedGuid = item.Id ?? item.Links.FirstOrDefault()?.Uri?.ToString() ?? Guid.NewGuid().ToString();
        var originalUrl = item.Links.FirstOrDefault(l => l.RelationshipType == "alternate")?.Uri?.ToString()
                       ?? item.Links.FirstOrDefault()?.Uri?.ToString()
                       ?? string.Empty;

        var rawContent = item.Content is TextSyndicationContent tc ? tc.Text : null;
        var rawSummary = item.Summary?.Text;

        return new Article
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            FeedGuid = feedGuid,
            Title = item.Title?.Text ?? "(no title)",
            Summary = _sanitizer.Sanitize(rawSummary),
            Content = _sanitizer.Sanitize(rawContent),
            OriginalUrl = originalUrl,
            PublishedAt = item.PublishDate == default ? fetchedAt : item.PublishDate,
            FetchedAt = fetchedAt,
            IsRead = false,
        };
    }

    private static bool IsSelfSignedCertificate(HttpRequestException ex) =>
        ex.InnerException?.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) == true;
}
