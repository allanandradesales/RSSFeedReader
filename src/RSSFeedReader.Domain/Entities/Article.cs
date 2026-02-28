namespace RSSFeedReader.Domain.Entities;

/// <summary>Represents a single article fetched from an RSS or Atom feed.</summary>
public sealed class Article
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the parent <see cref="Feed"/>.</summary>
    public Guid FeedId { get; set; }

    /// <summary>Gets or sets the globally unique identifier from the feed document (guid or id element).</summary>
    public required string FeedGuid { get; set; }

    /// <summary>Gets or sets the article title.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets the short description / excerpt, or <see langword="null"/> when absent.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the sanitized full HTML body, or <see langword="null"/> when absent.</summary>
    public string? Content { get; set; }

    /// <summary>Gets or sets the canonical URL of the article on the publisher's website.</summary>
    public required string OriginalUrl { get; set; }

    /// <summary>Gets or sets the publication timestamp declared by the feed.</summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>Gets or sets when this article was fetched and stored locally.</summary>
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>Gets or sets whether the user has read this article.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets the parent feed navigation property.</summary>
    public Feed? Feed { get; set; }
}
