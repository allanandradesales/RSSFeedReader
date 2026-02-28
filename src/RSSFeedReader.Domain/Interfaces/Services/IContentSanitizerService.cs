namespace RSSFeedReader.Domain.Interfaces.Services;

/// <summary>Sanitizes raw HTML from feed content before storage and display.</summary>
public interface IContentSanitizerService
{
    /// <summary>
    /// Strips disallowed tags and attributes from <paramref name="rawHtml"/>.
    /// </summary>
    /// <param name="rawHtml">Raw HTML from a feed, or <see langword="null"/>.</param>
    /// <returns>
    /// Sanitized HTML string, an empty string when the result is blank, or
    /// <see langword="null"/> when <paramref name="rawHtml"/> is <see langword="null"/>.
    /// </returns>
    string? Sanitize(string? rawHtml);
}
