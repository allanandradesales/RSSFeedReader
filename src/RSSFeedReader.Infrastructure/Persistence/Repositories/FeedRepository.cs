using Microsoft.EntityFrameworkCore;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IFeedRepository"/>.</summary>
public sealed class FeedRepository : IFeedRepository
{
    private readonly AppDbContext _db;

    /// <summary>Initializes a new instance of <see cref="FeedRepository"/>.</summary>
    public FeedRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Feed>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Feeds.AsNoTracking().OrderBy(f => f.Title).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Feed?> GetByIdAsync(Guid feedId, CancellationToken cancellationToken = default) =>
        await _db.Feeds.AsNoTracking().FirstOrDefaultAsync(f => f.Id == feedId, cancellationToken);

    /// <inheritdoc/>
    public async Task<Feed?> GetByUrlAsync(string url, CancellationToken cancellationToken = default) =>
        await _db.Feeds.AsNoTracking().FirstOrDefaultAsync(f => f.Url == url, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Feed feed, CancellationToken cancellationToken = default)
    {
        _db.Feeds.Add(feed);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid feedId, CancellationToken cancellationToken = default)
    {
        var feed = await _db.Feeds.FindAsync([feedId], cancellationToken);
        if (feed is not null)
        {
            _db.Feeds.Remove(feed);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateLastRefreshedAtAsync(Guid feedId, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default) =>
        await _db.Feeds
            .Where(f => f.Id == feedId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.LastRefreshedAt, refreshedAt), cancellationToken);
}
