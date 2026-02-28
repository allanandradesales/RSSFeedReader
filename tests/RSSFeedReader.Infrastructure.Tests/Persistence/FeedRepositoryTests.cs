using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Infrastructure.Persistence;
using RSSFeedReader.Infrastructure.Persistence.Repositories;

namespace RSSFeedReader.Infrastructure.Tests.Persistence;

public sealed class FeedRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly FeedRepository _repo;

    public FeedRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new FeedRepository(_db);
    }

    [Fact]
    public async Task AddAsync_ThenGetAll_ReturnsFeed()
    {
        var feed = new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/feed",
            Title = "Test Feed",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repo.AddAsync(feed);
        var all = await _repo.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Test Feed", all[0].Title);
    }

    [Fact]
    public async Task GetByUrlAsync_ExistingUrl_ReturnsFeed()
    {
        var feed = new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/rss",
            Title = "RSS Feed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await _repo.AddAsync(feed);

        var found = await _repo.GetByUrlAsync("https://example.com/rss");

        Assert.NotNull(found);
        Assert.Equal("RSS Feed", found!.Title);
    }

    [Fact]
    public async Task GetByUrlAsync_NonExistingUrl_ReturnsNull()
    {
        var result = await _repo.GetByUrlAsync("https://no-such-feed.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFeed()
    {
        var id = Guid.NewGuid();
        var feed = new Feed { Id = id, Url = "https://example.com/del", Title = "Del", CreatedAt = DateTimeOffset.UtcNow };
        await _repo.AddAsync(feed);

        await _repo.DeleteAsync(id);
        var all = await _repo.GetAllAsync();

        Assert.Empty(all);
    }

    [Fact]
    public async Task UpdateLastRefreshedAtAsync_SetsTimestamp()
    {
        var id = Guid.NewGuid();
        var feed = new Feed { Id = id, Url = "https://example.com/upd", Title = "Upd", CreatedAt = DateTimeOffset.UtcNow };
        await _repo.AddAsync(feed);

        var refreshed = DateTimeOffset.UtcNow;
        await _repo.UpdateLastRefreshedAtAsync(id, refreshed);

        var updated = await _repo.GetByUrlAsync("https://example.com/upd");
        Assert.NotNull(updated);
        Assert.NotNull(updated!.LastRefreshedAt);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
