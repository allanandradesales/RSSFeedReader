using System.Xml.Linq;
using Moq;
using RSSFeedReader.Application.UseCases.ExportSubscriptionsAsOpml;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class ExportSubscriptionsAsOpmlHandlerTests
{
    private readonly Mock<IFeedRepository> _feedRepo = new();
    private readonly Mock<IOpmlFileExporter> _exporter = new();
    private readonly ExportSubscriptionsAsOpmlHandler _handler;

    public ExportSubscriptionsAsOpmlHandlerTests()
    {
        _handler = new ExportSubscriptionsAsOpmlHandler(_feedRepo.Object, _exporter.Object);
    }

    [Fact]
    public async Task HandleAsync_NoFeeds_ReturnsNoSubscriptionsError()
    {
        _feedRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _handler.HandleAsync(new ExportSubscriptionsAsOpmlCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportSubscriptionsAsOpmlError.NoSubscriptions, result.Error);
        _exporter.Verify(e => e.SaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithFeeds_SavesOpmlAndReturnsFilePath()
    {
        const string expectedPath = "/Users/test/Downloads/subscriptions.opml";
        var feeds = new List<Feed>
        {
            new() { Id = Guid.NewGuid(), Url = "https://feed1.example.com/rss", Title = "Feed One", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Url = "https://feed2.example.com/rss", Title = "Feed Two", CreatedAt = DateTimeOffset.UtcNow },
        };

        _feedRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feeds);
        _exporter.Setup(e => e.SaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expectedPath);

        var result = await _handler.HandleAsync(new ExportSubscriptionsAsOpmlCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPath, result.FilePath);
    }

    [Fact]
    public async Task HandleAsync_SaverThrows_ReturnsSaveFailedError()
    {
        var feeds = new List<Feed>
        {
            new() { Id = Guid.NewGuid(), Url = "https://feed.example.com/rss", Title = "Feed", CreatedAt = DateTimeOffset.UtcNow },
        };

        _feedRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feeds);
        _exporter.Setup(e => e.SaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new IOException("Disk full"));

        var result = await _handler.HandleAsync(new ExportSubscriptionsAsOpmlCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportSubscriptionsAsOpmlError.SaveFailed, result.Error);
        Assert.Contains("Disk full", result.ErrorDetail);
    }

    [Fact]
    public void BuildOpmlXml_WithFeeds_ProducesValidOpml()
    {
        var feeds = new List<Feed>
        {
            new() { Id = Guid.NewGuid(), Url = "https://tech.example.com/rss", Title = "Tech News", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Url = "https://sci.example.com/feed", Title = "Science & Research", CreatedAt = DateTimeOffset.UtcNow },
        };

        var xml = ExportSubscriptionsAsOpmlHandler.BuildOpmlXml(feeds);

        // Must be parseable
        var doc = XDocument.Parse(xml.Substring(xml.IndexOf('\n') + 1)); // skip XML declaration line
        var opml = doc.Root;
        Assert.NotNull(opml);
        Assert.Equal("opml", opml!.Name.LocalName);
        Assert.Equal("2.0", opml.Attribute("version")?.Value);

        // Head
        var head = opml.Element("head");
        Assert.NotNull(head);
        Assert.Equal("RSS Feed Reader Subscriptions", head!.Element("title")?.Value);

        // Body outlines
        var body = opml.Element("body");
        Assert.NotNull(body);
        var outlines = body!.Elements("outline").ToList();
        Assert.Equal(2, outlines.Count);

        Assert.Equal("Tech News", outlines[0].Attribute("text")?.Value);
        Assert.Equal("https://tech.example.com/rss", outlines[0].Attribute("xmlUrl")?.Value);
        Assert.Equal("rss", outlines[0].Attribute("type")?.Value);

        // Special character encoding: & must be escaped as &amp; in the raw XML
        Assert.Contains("&amp;", xml);
        Assert.Equal("Science & Research", outlines[1].Attribute("text")?.Value);
    }

    [Fact]
    public void BuildOpmlXml_StartsWithXmlDeclaration()
    {
        var feeds = new List<Feed>
        {
            new() { Id = Guid.NewGuid(), Url = "https://example.com/rss", Title = "Test", CreatedAt = DateTimeOffset.UtcNow },
        };

        var xml = ExportSubscriptionsAsOpmlHandler.BuildOpmlXml(feeds);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
    }
}
