using System.Text;

namespace LangSwitcher;

public enum Lang { Other, En, Ru }

/// <summary>Одно нажатие клавиши: виртуальный код + состояние Shift и CapsLock.
/// По нажатию можно получить символ в любой раскладке.</summary>
public readonly record struct KeyStroke(int Vk, bool Shift, bool Caps);

/// <summary>Соответствие клавиш раскладкам US (QWERTY) и Русская (ЙЦУКЕН).</summary>
public static class KeyMap
{
    // vk -> 4 символа: EN, EN+Shift, RU, RU+Shift
    private static readonly Dictionary<int, string> Table = new();
    private static readonly Dictionary<char, (int Vk, bool Shift)> EnRev = new();
    private static readonly Dictionary<char, (int Vk, bool Shift)> RuRev = new();

    static KeyMap()
    {
        void Add(int vk, string s) => Table[vk] = s;

        Add(0xC0, "`~ёЁ");
        Add(0x31, "1!1!"); Add(0x32, "2@2\""); Add(0x33, "3#3№"); Add(0x34, "4$4;"); Add(0x35, "5%5%");
        Add(0x36, "6^6:"); Add(0x37, "7&7?"); Add(0x38, "8*8*"); Add(0x39, "9(9("); Add(0x30, "0)0)");
        Add(0xBD, "-_-_"); Add(0xBB, "=+=+");
        Add(0xDB, "[{хХ"); Add(0xDD, "]}ъЪ"); Add(0xDC, "\\|\\/");
        Add(0xBA, ";:жЖ"); Add(0xDE, "'\"эЭ");
        Add(0xBC, ",<бБ"); Add(0xBE, ".>юЮ"); Add(0xBF, "/?.,");

        const string en = "qwertyuiopasdfghjklzxcvbnm";
        const string ru = "йцукенгшщзфывапролдячсмить";
        for (int i = 0; i < en.Length; i++)
        {
            char e = en[i], r = ru[i];
            Add(char.ToUpperInvariant(e), $"{e}{char.ToUpperInvariant(e)}{r}{char.ToUpperInvariant(r)}");
        }

        foreach (var (vk, s) in Table)
        {
            EnRev.TryAdd(s[0], (vk, false)); EnRev.TryAdd(s[1], (vk, true));
            RuRev.TryAdd(s[2], (vk, false)); RuRev.TryAdd(s[3], (vk, true));
        }

        // Дополнительная клавиша \ на 102-клавишных клавиатурах (после обратных таблиц).
        Table[0xE2] = "\\|\\/";
    }

    /// <summary>Клавиша печатает символ (буква/цифра/знак), а не управляющая.</summary>
    public static bool IsWordKey(int vk) => Table.ContainsKey(vk);

    public static char CharOf(KeyStroke k, Lang lang)
    {
        string s = Table[k.Vk];
        int off = lang == Lang.Ru ? 2 : 0;
        char lo = s[off];
        bool upper = char.IsLetter(lo) ? k.Shift ^ k.Caps : k.Shift;
        return upper ? s[off + 1] : lo;
    }

    public static string Render(IReadOnlyList<KeyStroke> keys, Lang lang)
    {
        var sb = new StringBuilder(keys.Count);
        foreach (var k in keys) sb.Append(CharOf(k, lang));
        return sb.ToString();
    }

    public static Lang OtherLang(Lang l) => l == Lang.Ru ? Lang.En : Lang.Ru;

    public static bool IsCyr(char c) => c is >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё';
    public static bool IsLat(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    /// <summary>Перекодирует произвольный текст, набранный не в той раскладке.
    /// Направление определяется по тому, каких букв больше.</summary>
    public static string ConvertText(string text, out Lang target)
    {
        int cyr = 0, lat = 0;
        foreach (char c in text)
        {
            if (IsCyr(c)) cyr++;
            else if (IsLat(c)) lat++;
        }
        var from = cyr > lat ? Lang.Ru : Lang.En;
        target = OtherLang(from);
        var rev = from == Lang.Ru ? RuRev : EnRev;
        int off = target == Lang.Ru ? 2 : 0;

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(rev.TryGetValue(c, out var k) ? Table[k.Vk][off + (k.Shift ? 1 : 0)] : c);
        return sb.ToString();
    }
}
