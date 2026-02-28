using System.Globalization;

namespace RSSFeedReader.Presentation.Converters;

/// <summary>Converts a string to a boolean — <see langword="true"/> when the string is non-empty.</summary>
public sealed class StringToBoolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrEmpty(s);

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
