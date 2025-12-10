using System;
using System.Globalization;
using System.Windows.Data;

namespace InventoryERP.Presentation.Converters;

/// <summary>
/// R-275: Extracts the first letter from a string for product icon display.
/// </summary>
public class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return str[0].ToString().ToUpperInvariant();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
