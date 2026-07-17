using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DofusPolyfocus;

public sealed class AppConfig
{
    public string[] Slots { get; set; } = Enumerable.Range(1, 9).Select(i => $"Ctrl+{i}").ToArray();
    public string Next { get; set; } = "Ctrl+Tab";
    public string Previous { get; set; } = "Ctrl+Shift+Tab";

    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppConfig LoadOrCreateDefault()
    {
        if (File.Exists(ConfigPath))
        {
            var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
            if (loaded is not null)
            {
                return loaded;
            }
        }

        var defaults = new AppConfig();
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(defaults, SerializerOptions));
        return defaults;
    }
}

public static class HotkeyBinding
{
    public static (uint Modifiers, uint VirtualKey) Parse(string binding)
    {
        var parts = binding.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        uint modifiers = 0;
        uint? key = null;

        foreach (var part in parts)
        {
            string lower = part.ToLowerInvariant();
            switch (lower)
            {
                case "ctrl": modifiers |= Native.MOD_CONTROL; break;
                case "shift": modifiers |= Native.MOD_SHIFT; break;
                case "alt": modifiers |= Native.MOD_ALT; break;
                case "tab": key = 0x09; break;
                case "left": key = 0x25; break;
                case "up": key = 0x26; break;
                case "right": key = 0x27; break;
                case "down": key = 0x28; break;
                default:
                    if (lower.StartsWith("numpad") && lower.Length == 7 && char.IsDigit(lower[6]))
                        key = (uint)(0x60 + (lower[6] - '0')); // numpad0..9
                    else if (part.Length == 1)
                        key = char.ToUpperInvariant(part[0]); // 0-9 and A-Z
                    else
                        throw new FormatException($"Unknown key '{part}' in '{binding}'.");
                    break;
            }
        }

        if (key is null)
        {
            throw new FormatException($"Hotkey '{binding}' has no key");
        }

        return (modifiers, key.Value);
    }
}
