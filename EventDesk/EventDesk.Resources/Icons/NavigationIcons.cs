using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace EventDesk.Resources.Icons;

public static class NavigationIcons
{
    public static ImageIcon FromImage(string path)
    {
        BitmapImage source = new(new Uri($"pack://application:,,,{path}", UriKind.Absolute));

        // The supplied menu PNGs have wide transparent margins around their artwork.
        CroppedBitmap cropped = new(source, new Int32Rect(
            source.PixelWidth / 4,
            source.PixelHeight / 4,
            source.PixelWidth / 2,
            source.PixelHeight / 2));

        return new ImageIcon
        {
            Source = cropped,
            Width = 24,
            Height = 24
        };
    }
}
