using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ReiseArbeitszeitApp.Helpers;

public class OvertimeBrushConverter : IValueConverter
{
    private static readonly Brush PositiveBrush = new SolidColorBrush(Color.FromRgb(52, 199, 89));
    private static readonly Brush NegativeBrush = new SolidColorBrush(Color.FromRgb(255, 69, 58));
    private static readonly Brush NeutralBrush = new SolidColorBrush(Color.FromRgb(166, 175, 190));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            TimeSpan time when time > TimeSpan.Zero => PositiveBrush,
            TimeSpan time when time < TimeSpan.Zero => NegativeBrush,
            _ => NeutralBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
