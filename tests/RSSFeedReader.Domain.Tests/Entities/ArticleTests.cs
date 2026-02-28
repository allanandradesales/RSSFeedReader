using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Domain.Tests.Entities;

public sealed class ArticleTests
{
    [Fact]
    public void Article_IsRead_DefaultsFalse()
    {
        var article = new Article
        {
            FeedGuid = "guid-1",
            Title = "Hello",
            OriginalUrl = "https://example.com/1",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow,
        };

        Assert.False(article.IsRead);
    }

    [Fact]
    public void Article_OptionalFields_AcceptNull()
    {
        var article = new Article
        {
            FeedGuid = "guid-2",
            Title = "No summary",
            OriginalUrl = "https://example.com/2",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow,
            Summary = null,
            Content = null,
        };

        Assert.Null(article.Summary);
        Assert.Null(article.Content);
    }
}
