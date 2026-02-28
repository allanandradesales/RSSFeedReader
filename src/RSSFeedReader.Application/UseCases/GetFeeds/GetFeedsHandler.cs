using RSSFeedReader.Application.DTOs;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.UseCases.GetFeeds;

/// <summary>Handles <see cref="GetFeedsQuery"/>.</summary>
public sealed class GetFeedsHandler
{
    private readonly IFeedRepository _feedRepository;
    private readonly IArticleRepository _articleRepository;

    /// <summary>Initializes a new instance of <see cref="GetFeedsHandler"/>.</summary>
    public GetFeedsHandler(IFeedRepository feedRepository, IArticleRepository articleRepository)
    {
        _feedRepository = feedRepository;
        _articleRepository = articleRepository;
    }

    /// <summary>Returns all feeds with their unread article counts.</summary>
    public async Task<IReadOnlyList<FeedDto>> HandleAsync(
        GetFeedsQuery query,
        CancellationToken cancellationToken = default)
    {
        var feeds = await _feedRepository.GetAllAsync(cancellationToken);

        var results = new List<FeedDto>(feeds.Count);
        foreach (var feed in feeds)
        {
            var unread = await _articleRepository.GetUnreadCountByFeedIdAsync(feed.Id, cancellationToken);
            results.Add(new FeedDto(feed.Id, feed.Url, feed.Title, feed.LastRefreshedAt, unread));
        }

        return results;
    }
}
