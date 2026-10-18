using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EventDesk.Resources.Converters;

public sealed class CalendarScaleConverter : IMultiValueConverter
{
    public double BaseSize { get; set; } = 1;

    public double MinimumValue { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double value || values[1] is not double size ||
            !double.IsFinite(value) || !double.IsFinite(size) || BaseSize <= 0)
        {
            return DependencyProperty.UnsetValue;
        }

        return Math.Max(MinimumValue, value * size / BaseSize);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
