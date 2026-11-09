using System.Text.Json;
using Microsoft.Win32;

namespace LangSwitcher;

internal sealed class AppSettings
{
    public bool AutoSwitch { get; set; } = true;
    public int MinLetters { get; set; } = 2;
    public bool LearnFromUndo { get; set; } = true;

    public static string Dir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LangSwitcher");

    private static string SettingsPath => Path.Combine(Dir, "settings.json");
    public static string ExceptionsPath => Path.Combine(Dir, "exceptions.txt");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static List<string> LoadExceptions()
    {
        try
        {
            if (File.Exists(ExceptionsPath))
                return File.ReadAllLines(ExceptionsPath).Select(l => l.Trim())
                    .Where(l => l.Length > 0 && !l.StartsWith('#')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch { }
        return new();
    }

    public static void SaveExceptions(IEnumerable<string> words)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllLines(ExceptionsPath, words);
        }
        catch { }
    }

    // ---- автозапуск ----
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunName = "LangSwitcher";

    public static bool Autostart
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(RunName) is string;
        }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) k.SetValue(RunName, $"\"{Environment.ProcessPath}\"");
            else k.DeleteValue(RunName, false);
        }
    }
}
