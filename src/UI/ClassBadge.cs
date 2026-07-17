using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DofusPolyfocus;

public static class ClassBadge
{
    private static readonly Dictionary<string, Color> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Feca"] = Color.FromRgb(0x8E, 0x8E, 0x8E),
        ["Osamodas"] = Color.FromRgb(0x4C, 0xAF, 0x50),
        ["Enutrof"] = Color.FromRgb(0xFF, 0xB3, 0x00),
        ["Sram"] = Color.FromRgb(0x6A, 0x1B, 0x9A),
        ["Xelor"] = Color.FromRgb(0x60, 0x7D, 0x8B),
        ["Ecaflip"] = Color.FromRgb(0xFF, 0x98, 0x00),
        ["Eniripsa"] = Color.FromRgb(0xE9, 0x1E, 0x63),
        ["Iop"] = Color.FromRgb(0xF4, 0x43, 0x36),
        ["Cra"] = Color.FromRgb(0x2E, 0x7D, 0x32),
        ["Sadida"] = Color.FromRgb(0x8B, 0xC3, 0x4A),
        ["Sacrieur"] = Color.FromRgb(0x79, 0x55, 0x48),
        ["Pandawa"] = Color.FromRgb(0x3E, 0x27, 0x23),
        ["Rogue"] = Color.FromRgb(0x42, 0x42, 0x42),
        ["Masqueraider"] = Color.FromRgb(0x9C, 0x27, 0xB0),
        ["Foggernaut"] = Color.FromRgb(0x00, 0x96, 0x88),
        ["Eliotrope"] = Color.FromRgb(0x67, 0x3A, 0xB7),
        ["Huppermage"] = Color.FromRgb(0x30, 0x3F, 0x9F),
        ["Ouginak"] = Color.FromRgb(0x5D, 0x40, 0x37),
        ["Forgelance"] = Color.FromRgb(0x45, 0x5A, 0x64),
    };

    public static (string Name, string? ClassName) ParseTitle(string rawTitle)
    {
        var parts = rawTitle.Split(" - ", StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (rawTitle, null);
    }

    public static Brush BrushFor(string? className)
    {
        if (className is null) return Brushes.DimGray;

        if (Colors.TryGetValue(className, out var known)) return new SolidColorBrush(known);

        int hash = className.GetHashCode();
        return new SolidColorBrush(Color.FromRgb((byte)hash, (byte)(hash >> 8), (byte)(hash >> 16)));
    }

    public static string Initials(string? className) =>
        string.IsNullOrEmpty(className) ? "?" : className[..Math.Min(2, className.Length)].ToUpperInvariant();

    public static ImageSource? TryGetIcon(string? className)
    {
        if (string.IsNullOrEmpty(className)) return null;

        var uri = new Uri($"pack://application:,,,/assets/classes/{className.ToLowerInvariant()}.png", UriKind.Absolute);
        try
        {
            using var stream = Application.GetResourceStream(uri)?.Stream;
            if (stream is null) return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
