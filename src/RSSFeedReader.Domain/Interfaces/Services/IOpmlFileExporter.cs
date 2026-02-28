namespace RSSFeedReader.Domain.Interfaces.Services;

/// <summary>Writes an OPML document to the device's Downloads folder.</summary>
public interface IOpmlFileExporter
{
    /// <summary>
    /// Saves <paramref name="opmlContent"/> as <c>subscriptions.opml</c> in the Downloads folder,
    /// overwriting any existing file, and returns the full path of the saved file.
    /// </summary>
    Task<string> SaveAsync(string opmlContent, CancellationToken cancellationToken = default);
}
