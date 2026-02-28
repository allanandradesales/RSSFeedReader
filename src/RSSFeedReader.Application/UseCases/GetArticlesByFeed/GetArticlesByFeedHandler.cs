using RSSFeedReader.Application.DTOs;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.UseCases.GetArticlesByFeed;

/// <summary>Handles <see cref="GetArticlesByFeedQuery"/>.</summary>
public sealed class GetArticlesByFeedHandler
{
    private readonly IArticleRepository _articleRepository;

    /// <summary>Initializes a new instance of <see cref="GetArticlesByFeedHandler"/>.</summary>
    public GetArticlesByFeedHandler(IArticleRepository articleRepository) =>
        _articleRepository = articleRepository;

    /// <summary>Returns all articles for the specified feed, sorted newest-first.</summary>
    public async Task<IReadOnlyList<ArticleDto>> HandleAsync(
        GetArticlesByFeedQuery query,
        CancellationToken cancellationToken = default)
    {
        var articles = await _articleRepository.GetByFeedIdAsync(query.FeedId, cancellationToken);
        return articles
            .Select(a => new ArticleDto(a.Id, a.FeedId, a.Title, a.Summary, a.OriginalUrl, a.PublishedAt, a.IsRead))
            .ToList();
    }
}
