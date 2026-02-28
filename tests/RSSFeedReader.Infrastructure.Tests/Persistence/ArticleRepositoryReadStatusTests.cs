using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Infrastructure.Persistence;
using RSSFeedReader.Infrastructure.Persistence.Repositories;

namespace RSSFeedReader.Infrastructure.Tests.Persistence;

public sealed class ArticleRepositoryReadStatusTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ArticleRepository _repo;
    private readonly Feed _feed;

    public ArticleRepositoryReadStatusTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new ArticleRepository(_db);

        _feed = new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/feed",
            Title = "Test Feed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Feeds.Add(_feed);
        _db.SaveChanges();
    }

    private Article MakeArticle(string guid, bool isRead = false) => new()
    {
        Id = Guid.NewGuid(),
        FeedId = _feed.Id,
        FeedGuid = guid,
        Title = $"Article {guid}",
        OriginalUrl = $"https://example.com/{guid}",
        PublishedAt = DateTimeOffset.UtcNow,
        FetchedAt = DateTimeOffset.UtcNow,
        IsRead = isRead,
    };

    [Fact]
    public async Task MarkAsReadAsync_SetsIsReadToTrue()
    {
        var article = MakeArticle("guid-1", isRead: false);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        await _repo.MarkAsReadAsync(article.Id);

        var updated = await _db.Articles.AsNoTracking().FirstAsync(a => a.Id == article.Id);
        Assert.True(updated.IsRead);
    }

    [Fact]
    public async Task ToggleReadStatusAsync_TogglesFromFalseToTrue_ReturnsTrue()
    {
        var article = MakeArticle("guid-2", isRead: false);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        var newIsRead = await _repo.ToggleReadStatusAsync(article.Id);

        Assert.True(newIsRead);
        var updated = await _db.Articles.AsNoTracking().FirstAsync(a => a.Id == article.Id);
        Assert.True(updated.IsRead);
    }

    [Fact]
    public async Task ToggleReadStatusAsync_TogglesFromTrueToFalse_ReturnsFalse()
    {
        var article = MakeArticle("guid-3", isRead: true);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        var newIsRead = await _repo.ToggleReadStatusAsync(article.Id);

        Assert.False(newIsRead);
        var updated = await _db.Articles.AsNoTracking().FirstAsync(a => a.Id == article.Id);
        Assert.False(updated.IsRead);
    }

    [Fact]
    public async Task ToggleReadStatusAsync_ArticleNotFound_ReturnsNull()
    {
        var newIsRead = await _repo.ToggleReadStatusAsync(Guid.NewGuid());

        Assert.Null(newIsRead);
    }

    [Fact]
    public async Task GetUnreadCountByFeedIdAsync_CountsOnlyUnreadArticles()
    {
        _db.Articles.AddRange(
            MakeArticle("guid-a", isRead: false),
            MakeArticle("guid-b", isRead: false),
            MakeArticle("guid-c", isRead: true));
        await _db.SaveChangesAsync();

        var count = await _repo.GetUnreadCountByFeedIdAsync(_feed.Id);

        Assert.Equal(2, count);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
