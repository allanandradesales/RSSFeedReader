namespace RSSFeedReader.Application.DTOs;

/// <summary>Lightweight projection of a <see cref="Domain.Entities.Feed"/> for presentation use.</summary>
public sealed record FeedDto(
    Guid Id,
    string Url,
    string Title,
    DateTimeOffset? LastRefreshedAt,
    int UnreadCount);
