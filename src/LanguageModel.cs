using System.Text;

namespace LangSwitcher;

/// <summary>Символьная триграммная модель языка со сглаживанием (back-off по Дирихле).</summary>
public sealed class TrigramModel
{
    private const char Bos = '^', Eos = '$';
    private const double Beta = 1.5;

    private readonly Dictionary<long, int> _c3 = new();
    private readonly Dictionary<long, int> _c2ctx = new();
    private readonly Dictionary<long, int> _c2 = new();
    private readonly Dictionary<int, int> _c1ctx = new();
    private readonly Dictionary<int, int> _c1 = new();
    private readonly HashSet<char> _alphabet = new();
    private int _total;

    public HashSet<string> Words { get; } = new();

    private static long K3(char a, char b, char c) => ((long)a << 32) | ((long)b << 16) | c;
    private static long K2(char a, char b) => ((long)a << 16) | b;

    private static void Inc<TK>(Dictionary<TK, int> d, TK k, int w) where TK : notnull
        => d[k] = d.TryGetValue(k, out var v) ? v + w : w;

    /// <summary>Добавляет слово (уже нормализованное, только символы токена).</summary>
    public void AddToken(string token, int weight = 1)
    {
        if (token.Length == 0) return;
        Words.Add(token);
        string s = $"{Bos}{Bos}{token}{Eos}";
        for (int i = 2; i < s.Length; i++)
        {
            char a = s[i - 2], b = s[i - 1], c = s[i];
            Inc(_c3, K3(a, b, c), weight);
            Inc(_c2ctx, K2(a, b), weight);
            Inc(_c2, K2(b, c), weight);
            Inc(_c1ctx, b, weight);
            Inc(_c1, c, weight);
            _total += weight;
            _alphabet.Add(c);
        }
    }

    private double P(char a, char b, char c)
    {
        double p1 = (_c1.GetValueOrDefault(c) + 0.5) / (_total + 0.5 * (_alphabet.Count + 1));
        double p2 = (_c2.GetValueOrDefault(K2(b, c)) + Beta * p1) / (_c1ctx.GetValueOrDefault(b) + Beta);
        double p3 = (_c3.GetValueOrDefault(K3(a, b, c)) + Beta * p2) / (_c2ctx.GetValueOrDefault(K2(a, b)) + Beta);
        return p3;
    }

    /// <summary>Суммарный натуральный логарифм вероятности токена (len+1 переходов).</summary>
    public double LogProb(string token)
    {
        double sum = 0;
        char a = Bos, b = Bos;
        foreach (char c in token)
        {
            sum += Math.Log(P(a, b, c));
            a = b; b = c;
        }
        sum += Math.Log(P(a, b, Eos));
        return sum;
    }
}

/// <summary>Решает, набрано ли слово в неправильной раскладке.</summary>
public static class Detector
{
    // Параметры подобраны на тестовом наборе (tests/DetectorTests).
    public const double DictLogP = -1.0;       // «оценка» слова, найденного в словаре, на один переход
    public const double Margin = 1.0;          // насколько другой язык должен быть лучше (длинные слова)
    public const double ShortMargin = 1.6;     // то же для слов из 2–3 букв
    public const double MinScore = -3.3;       // минимальная правдоподобность результата
    private const double SoftPunct = 0.3;      // штраф за обычную пунктуацию по краям
    private const double HardPunct = 3.0;      // штраф за странные символы

    private static TrigramModel? _en, _ru;
    private static readonly object Lock = new();

    public static TrigramModel En { get { EnsureBuilt(); return _en!; } }
    public static TrigramModel Ru { get { EnsureBuilt(); return _ru!; } }
    public static TrigramModel Model(Lang l) => l == Lang.Ru ? Ru : En;

    private static void EnsureBuilt()
    {
        if (_en != null) return;
        lock (Lock)
        {
            if (_en != null) return;
            var en = new TrigramModel();
            var ru = new TrigramModel();
            AddWords(en, Lang.En, WordLists.En);
            AddWords(ru, Lang.Ru, WordLists.Ru);
            _ru = ru;
            _en = en;
        }
    }

    private static void AddWords(TrigramModel m, Lang lang, string text)
    {
        var sb = new StringBuilder();
        foreach (char ch in Normalize(text) + " ")
        {
            if (IsTokenChar(lang, ch)) { sb.Append(ch); continue; }
            if (sb.Length > 0)
            {
                string t = sb.ToString().Trim('\'');
                if (t.Length > 0) m.AddToken(t);
                sb.Clear();
            }
        }
    }

    /// <summary>Подгружает пользовательские словари dict_en.txt / dict_ru.txt (если есть).</summary>
    public static void LoadUserWords(string dir)
    {
        try
        {
            string en = Path.Combine(dir, "dict_en.txt"), ru = Path.Combine(dir, "dict_ru.txt");
            if (File.Exists(en)) AddWords(En, Lang.En, File.ReadAllText(en));
            if (File.Exists(ru)) AddWords(Ru, Lang.Ru, File.ReadAllText(ru));
        }
        catch { /* словари необязательны */ }
    }

    public static string Normalize(string s) => s.ToLowerInvariant().Replace('ё', 'е');

    public static bool IsLetter(Lang l, char c) =>
        l == Lang.Ru ? c is >= 'а' and <= 'я' : c is >= 'a' and <= 'z';

    private static bool IsTokenChar(Lang l, char c) => IsLetter(l, c) || (l == Lang.En && c == '\'');

    /// <summary>Средний логарифм вероятности на символ. Чем ближе к нулю, тем «естественнее» текст.</summary>
    public static double Score(string text, Lang lang, out string core)
    {
        var m = Model(lang);
        string t = Normalize(text);
        int s = 0, e = t.Length;
        double pen = 0;
        while (s < e && !IsLetter(lang, t[s])) { pen += "(\"«'".IndexOf(t[s]) >= 0 ? SoftPunct : HardPunct; s++; }
        while (e > s && !IsLetter(lang, t[e - 1])) { pen += ".,!?:;)\"»'…".IndexOf(t[e - 1]) >= 0 ? SoftPunct : HardPunct; e--; }
        core = t[s..e];
        if (core.Length == 0) return -99;

        double total = 0;
        int trans = 0;
        var sb = new StringBuilder();

        void Flush()
        {
            if (sb.Length == 0) return;
            string p = sb.ToString().Trim('\'');
            sb.Clear();
            if (p.Length == 0) return;
            int n = p.Length + 1;
            double lp = m.LogProb(p);
            if (m.Words.Contains(p)) lp = Math.Max(lp, DictLogP * n);
            total += lp;
            trans += n;
        }

        foreach (char c in core)
        {
            if (IsTokenChar(lang, c)) sb.Append(c);
            else
            {
                Flush();
                if (c != '-') pen += HardPunct;
            }
        }
        Flush();
        if (trans == 0) return -99;
        return (total - pen) / trans;
    }

    private static bool IsCamelCase(string s)
    {
        bool lower = false, innerUpper = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!char.IsLetter(s[i])) continue;
            if (char.IsLower(s[i])) lower = true;
            else if (i > 0) innerUpper = true;
        }
        return lower && innerUpper;
    }

    /// <summary>Нужно ли переключить слово <paramref name="orig"/> (набрано в раскладке <paramref name="cur"/>)
    /// в вариант <paramref name="conv"/> (те же клавиши в другой раскладке).</summary>
    public static bool ShouldSwitch(string orig, string conv, Lang cur, int minLetters,
        IReadOnlySet<string> exceptions, out string origCore, out double sCur, out double sOth)
    {
        sCur = sOth = 0;
        origCore = Normalize(orig);
        var oth = KeyMap.OtherLang(cur);

        if (orig.Any(char.IsDigit)) return false;              // пароли, номера, h2o…
        string convN = Normalize(conv);
        int convLetters = convN.Count(c => IsLetter(oth, c));
        if (convLetters < Math.Max(1, minLetters)) return false;
        if (IsCamelCase(orig)) return false;                   // идентификаторы в коде

        sCur = Score(orig, cur, out origCore);
        sOth = Score(conv, oth, out var convCore);

        string origN = Normalize(orig);
        if (exceptions.Contains(origN) || exceptions.Contains(origCore) ||
            exceptions.Contains(convN) || exceptions.Contains(convCore))
            return false;

        if (Model(cur).Words.Contains(origCore)) return false; // это настоящее слово текущего языка
        double diff = sOth - sCur;
        if (Model(oth).Words.Contains(convCore)) return diff > 0;

        // Короткие слова капсом (PDF, USB, ЖКХ) — обычно аббревиатуры, не трогаем.
        if (convLetters <= 4 && orig.Where(char.IsLetter).All(char.IsUpper)) return false;

        double margin = convLetters <= 3 ? ShortMargin : Margin;
        if (diff <= margin) return false;
        if (sOth > MinScore) return true;
        // Длинное редкое слово: модель знает его хуже, но отрыв от текущего языка большой.
        return convLetters >= 5 && diff > 2.5 && sOth > -4.6;
    }
}
