namespace RSSFeedReader.Domain.Entities;

/// <summary>Represents an RSS or Atom feed subscription.</summary>
public sealed class Feed
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the feed URL (unique per row).</summary>
    public required string Url { get; set; }

    /// <summary>Gets or sets the feed title resolved from the feed document.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets when the feed was last successfully refreshed, or <see langword="null"/> if never refreshed.</summary>
    public DateTimeOffset? LastRefreshedAt { get; set; }

    /// <summary>Gets or sets when the subscription was first added.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets the articles belonging to this feed.</summary>
    public ICollection<Article> Articles { get; } = [];
}
