using System.Windows.Media;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Models;

namespace EventDesk.Resources.Theme;

public static class CalendarColorPalette
{
    public const string Blue = "#425EBA";
    public const string Green = "#259B70";
    public const string Orange = "#D88436";
    public const string Purple = "#8657B5";
    public const string Red = "#CE5967";

    public static IReadOnlyList<CalendarColorOption> CreateOptions(IResourceWrapper resources) =>
    [
        new(Blue, resources.ColorBlue, CreateBrush(Blue)),
        new(Green, resources.ColorGreen, CreateBrush(Green)),
        new(Orange, resources.ColorOrange, CreateBrush(Orange)),
        new(Purple, resources.ColorPurple, CreateBrush(Purple)),
        new(Red, resources.ColorRed, CreateBrush(Red))
    ];

    public static Brush CreateBrush(string? colorHex)
    {
        Color color;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(colorHex ?? Blue);
        }
        catch (FormatException)
        {
            color = (Color)ColorConverter.ConvertFromString(Blue);
        }

        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
