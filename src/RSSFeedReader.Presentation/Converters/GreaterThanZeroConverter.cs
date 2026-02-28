using System.Globalization;

namespace RSSFeedReader.Presentation.Converters;

/// <summary>Converts an integer to a boolean — <see langword="true"/> when the value is greater than zero.</summary>
public sealed class GreaterThanZeroConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i > 0;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
