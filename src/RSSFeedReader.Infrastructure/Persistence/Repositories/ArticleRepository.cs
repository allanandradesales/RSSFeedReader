using Microsoft.EntityFrameworkCore;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IArticleRepository"/>.</summary>
public sealed class ArticleRepository : IArticleRepository
{
    private readonly AppDbContext _db;

    /// <summary>Initializes a new instance of <see cref="ArticleRepository"/>.</summary>
    public ArticleRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Article>> GetAllSortedAsync(CancellationToken cancellationToken = default) =>
        await _db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Article>> GetByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default) =>
        await _db.Articles.AsNoTracking()
            .Where(a => a.FeedId == feedId)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default) =>
        await _db.Articles.CountAsync(a => a.FeedId == feedId && !a.IsRead, cancellationToken);

    /// <inheritdoc/>
    public async Task UpsertManyAsync(IEnumerable<Article> articles, CancellationToken cancellationToken = default)
    {
        foreach (var incoming in articles)
        {
            var existing = await _db.Articles.FirstOrDefaultAsync(a => a.FeedGuid == incoming.FeedGuid, cancellationToken);
            if (existing is null)
            {
                _db.Articles.Add(incoming);
            }
            else
            {
                existing.Title = incoming.Title;
                existing.Summary = incoming.Summary;
                existing.Content = incoming.Content;
                existing.OriginalUrl = incoming.OriginalUrl;
                existing.PublishedAt = incoming.PublishedAt;
                existing.FetchedAt = incoming.FetchedAt;
                // IsRead is intentionally preserved
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task MarkAsReadAsync(Guid articleId, CancellationToken cancellationToken = default) =>
        await _db.Articles
            .Where(a => a.Id == articleId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true), cancellationToken);

    /// <inheritdoc/>
    public async Task ToggleReadStatusAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var article = await _db.Articles.FindAsync([articleId], cancellationToken);
        if (article is not null)
        {
            article.IsRead = !article.IsRead;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default) =>
        await _db.Articles.Where(a => a.FeedId == feedId).ExecuteDeleteAsync(cancellationToken);
}
