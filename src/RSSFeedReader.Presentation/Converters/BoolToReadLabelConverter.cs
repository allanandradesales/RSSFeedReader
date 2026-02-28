using System.Globalization;

namespace RSSFeedReader.Presentation.Converters;

/// <summary>Converts a boolean read-status to a toggle button label.</summary>
public sealed class BoolToReadLabelConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Mark Unread" : "Mark Read";

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
