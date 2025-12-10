using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InventoryERP.Presentation.Converters;

/// <summary>
/// R-280: Colors Quantity based on value (Green > 0, Red < 0, Gray = 0)
/// </summary>
public class QtyColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d > 0) return Brushes.ForestGreen; // or #059669
            if (d < 0) return Brushes.Red;
            return Brushes.Gray;
        }
        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
