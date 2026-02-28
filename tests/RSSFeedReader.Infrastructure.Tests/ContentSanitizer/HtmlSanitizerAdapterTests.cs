using RSSFeedReader.Infrastructure.ContentSanitizer;

namespace RSSFeedReader.Infrastructure.Tests.ContentSanitizer;

public sealed class HtmlSanitizerAdapterTests
{
    private readonly HtmlSanitizerAdapter _adapter = new();

    [Fact]
    public void Sanitize_Null_ReturnsNull()
    {
        var result = _adapter.Sanitize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsEmpty()
    {
        var result = _adapter.Sanitize(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_ScriptTag_IsStripped()
    {
        var result = _adapter.Sanitize("<p>Hello</p><script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Hello</p>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_AllowedTags_ArePreserved()
    {
        var result = _adapter.Sanitize("<p><b>Bold</b> and <em>italic</em></p>");

        Assert.Contains("<b>Bold</b>", result, StringComparison.Ordinal);
        Assert.Contains("<em>italic</em>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_JavscriptHref_IsStripped()
    {
        var result = _adapter.Sanitize("<a href=\"javascript:alert(1)\">click</a>");

        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }
}
