using System.Globalization;

namespace Fitness_App.UI.Converters
{
    /// <summary>
    /// Returns true when the bound string is NOT null/empty.
    /// Useful for IsVisible bindings: show a label only when the text has a value.
    /// </summary>
    public sealed class StringNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
