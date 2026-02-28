using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Domain.Tests.Entities;

public sealed class FeedTests
{
    [Fact]
    public void Feed_DefaultArticles_IsEmpty()
    {
        var feed = new Feed { Url = "https://example.com/feed", Title = "Example" };

        Assert.Empty(feed.Articles);
    }

    [Fact]
    public void Feed_Properties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var feed = new Feed
        {
            Id = id,
            Url = "https://example.com/feed",
            Title = "My Feed",
            CreatedAt = now,
            LastRefreshedAt = now,
        };

        Assert.Equal(id, feed.Id);
        Assert.Equal("https://example.com/feed", feed.Url);
        Assert.Equal("My Feed", feed.Title);
        Assert.Equal(now, feed.CreatedAt);
        Assert.Equal(now, feed.LastRefreshedAt);
    }
}
