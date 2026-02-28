using System.Text;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Infrastructure.OpmlExport;

/// <summary>
/// Saves OPML content to the platform's standard Downloads folder
/// (<c>~/Downloads</c> on macOS, <c>%USERPROFILE%\Downloads</c> on Windows).
/// </summary>
public sealed class DownloadsOpmlFileExporter : IOpmlFileExporter
{
    private const string FileName = "subscriptions.opml";
    private const string TempSuffix = ".tmp";

    /// <inheritdoc/>
    public async Task<string> SaveAsync(string opmlContent, CancellationToken cancellationToken = default)
    {
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        Directory.CreateDirectory(downloadsPath);

        var targetPath = Path.Combine(downloadsPath, FileName);
        var tempPath = targetPath + TempSuffix;

        try
        {
            await File.WriteAllTextAsync(tempPath, opmlContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
            File.Move(tempPath, targetPath, overwrite: true);
            return targetPath;
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
