namespace RSSFeedReader.Application.DTOs;

/// <summary>Lightweight projection of an <see cref="Domain.Entities.Article"/> for presentation use.</summary>
public sealed record ArticleDto(
    Guid Id,
    Guid FeedId,
    string Title,
    string? Summary,
    string OriginalUrl,
    DateTimeOffset PublishedAt,
    bool IsRead);
