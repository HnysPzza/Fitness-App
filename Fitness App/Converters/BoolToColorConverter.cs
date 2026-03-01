using System.Globalization;

namespace Fitness_App.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRead)
        {
            return isRead 
                ? Color.FromArgb("#CBD5E1") 
                : Color.FromArgb("#FC5200");
        }
        return Color.FromArgb("#CBD5E1");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
