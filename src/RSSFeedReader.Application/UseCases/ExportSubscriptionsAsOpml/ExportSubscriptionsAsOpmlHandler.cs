using System.Globalization;
using System.Xml.Linq;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.UseCases.ExportSubscriptionsAsOpml;

/// <summary>Describes why exporting subscriptions as OPML failed.</summary>
public enum ExportSubscriptionsAsOpmlError
{
    /// <summary>There are no feed subscriptions to export.</summary>
    NoSubscriptions,

    /// <summary>The file could not be saved to the Downloads folder.</summary>
    SaveFailed,
}

/// <summary>Result of executing <see cref="ExportSubscriptionsAsOpmlCommand"/>.</summary>
public sealed record ExportSubscriptionsAsOpmlResult
{
    /// <summary>Gets the full path of the saved OPML file on success.</summary>
    public string? FilePath { get; init; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Gets the error type on failure.</summary>
    public ExportSubscriptionsAsOpmlError? Error { get; init; }

    /// <summary>Gets a human-readable error detail on failure.</summary>
    public string? ErrorDetail { get; init; }

    /// <summary>Creates a success result with the path of the saved file.</summary>
    public static ExportSubscriptionsAsOpmlResult Ok(string filePath) =>
        new() { FilePath = filePath };

    /// <summary>Creates a failure result when there are no subscriptions to export.</summary>
    public static ExportSubscriptionsAsOpmlResult NoSubscriptions() =>
        new() { Error = ExportSubscriptionsAsOpmlError.NoSubscriptions };

    /// <summary>Creates a failure result when saving the file failed.</summary>
    public static ExportSubscriptionsAsOpmlResult Fail(string detail) =>
        new() { Error = ExportSubscriptionsAsOpmlError.SaveFailed, ErrorDetail = detail };
}

/// <summary>Handles <see cref="ExportSubscriptionsAsOpmlCommand"/>.</summary>
public sealed class ExportSubscriptionsAsOpmlHandler
{
    private const string OpmlVersion = "2.0";
    private const string OpmlDocTitle = "RSS Feed Reader Subscriptions";
    private const string OutlineType = "rss";

    private readonly IFeedRepository _feedRepository;
    private readonly IOpmlFileExporter _fileExporter;

    /// <summary>Initializes a new instance of <see cref="ExportSubscriptionsAsOpmlHandler"/>.</summary>
    public ExportSubscriptionsAsOpmlHandler(IFeedRepository feedRepository, IOpmlFileExporter fileExporter)
    {
        _feedRepository = feedRepository;
        _fileExporter = fileExporter;
    }

    /// <summary>Generates an OPML file from all current feed subscriptions and saves it to Downloads.</summary>
    public async Task<ExportSubscriptionsAsOpmlResult> HandleAsync(
        ExportSubscriptionsAsOpmlCommand command,
        CancellationToken cancellationToken = default)
    {
        var feeds = await _feedRepository.GetAllAsync(cancellationToken);
        if (feeds.Count == 0)
            return ExportSubscriptionsAsOpmlResult.NoSubscriptions();

        var opmlXml = BuildOpmlXml(feeds);

        try
        {
            var filePath = await _fileExporter.SaveAsync(opmlXml, cancellationToken);
            return ExportSubscriptionsAsOpmlResult.Ok(filePath);
        }
        catch (Exception ex)
        {
            return ExportSubscriptionsAsOpmlResult.Fail(ex.Message);
        }
    }

    /// <summary>Builds the OPML 2.0 XML string for the given feeds.</summary>
    public static string BuildOpmlXml(IReadOnlyList<Feed> feeds)
    {
        var dateCreated = DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture);

        var doc = new XDocument(
            new XElement("opml",
                new XAttribute("version", OpmlVersion),
                new XElement("head",
                    new XElement("title", OpmlDocTitle),
                    new XElement("dateCreated", dateCreated)),
                new XElement("body",
                    feeds.Select(f =>
                        new XElement("outline",
                            new XAttribute("type", OutlineType),
                            new XAttribute("text", f.Title),
                            new XAttribute("xmlUrl", f.Url))))));

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine
               + doc.ToString(SaveOptions.None);
    }
}
