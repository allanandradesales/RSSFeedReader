using Ganss.Xss;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Infrastructure.ContentSanitizer;

/// <summary>
/// Wraps <see cref="HtmlSanitizer"/> with a fixed allowlist of tags and attributes
/// safe for displaying feed content.
/// </summary>
public sealed class HtmlSanitizerAdapter : IContentSanitizerService
{
    // Declared before Sanitizer so the field is ready when BuildSanitizer() runs
    private static readonly string[] AllowedTags =
    [
        "p", "br", "b", "i", "strong", "em",
        "ul", "ol", "li", "blockquote",
        "a", "img", "pre", "code",
    ];

    private static readonly HtmlSanitizer Sanitizer = BuildSanitizer();

    private static HtmlSanitizer BuildSanitizer()
    {
        var s = new HtmlSanitizer();

        s.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
            s.AllowedTags.Add(tag);

        s.AllowedAttributes.Clear();
        s.AllowedAttributes.Add("href");
        s.AllowedAttributes.Add("src");
        s.AllowedAttributes.Add("alt");
        s.AllowedAttributes.Add("title");

        s.AllowedSchemes.Clear();
        s.AllowedSchemes.Add("https");
        s.AllowedSchemes.Add("http");

        return s;
    }

    /// <inheritdoc/>
    public string? Sanitize(string? rawHtml)
    {
        if (rawHtml is null)
            return null;

        var result = Sanitizer.Sanitize(rawHtml);
        return result.Length == 0 ? string.Empty : result;
    }
}
