using LangSwitcher;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// (что набрано, в какой раскладке, нужно ли переключать)
var cases = new (string Typed, Lang Cur, bool Expect)[]
{
    // Русские слова, набранные в английской раскладке -> переключать
    ("ghbdtn", Lang.En, true), ("rfr", Lang.En, true), ("ltkf", Lang.En, true), ("yf", Lang.En, true),
    ("lf", Lang.En, true), ("yt", Lang.En, true), ("xnj", Lang.En, true), ("j,sxyj", Lang.En, true),
    ("vjcrdf", Lang.En, true), ("lfdfq", Lang.En, true), ("ntcn", Lang.En, true), ("zyltrc", Lang.En, true),
    ("ult", Lang.En, true), ("e;t", Lang.En, true), ("ndjq", Lang.En, true), ("cgfcb,j", Lang.En, true),
    ("Ghbdtn", Lang.En, true), ("GHBDTN", Lang.En, true), ("rjnjhsq", Lang.En, true), ("gjxtve", Lang.En, true),
    ("ctujlyz", Lang.En, true), ("pfdnhf", Lang.En, true), ("hf,jnf", Lang.En, true), ("vfibyf", Lang.En, true),
    ("ghjuhfvvf", Lang.En, true), ("rjvgm.nth", Lang.En, true), ("ytcrjkmrj", Lang.En, true),
    ("gjkextybt", Lang.En, true), ("ljhjuf", Lang.En, true), ("rjhjdf", Lang.En, true), ("ghbdtn?", Lang.En, true),
    ("cltkfq", Lang.En, true), ("crf;b", Lang.En, true), ("ndjb", Lang.En, true), ("vs", Lang.En, true),
    ("nt,t", Lang.En, true), ("['", Lang.En, false), ("gjxtve", Lang.En, true), ("rjulf", Lang.En, true),
    ("pltcm", Lang.En, true), ("ctqxfc", Lang.En, true), ("ntgthm", Lang.En, true), ("ljv", Lang.En, true),
    ("k.,k.", Lang.En, true), ("ktym", Lang.En, true), ("[jxe", Lang.En, true), ("k.,jq", Lang.En, true), ("ult-nj", Lang.En, true),
    ("'nj", Lang.En, true), ("j,]zdktybt", Lang.En, true), ("cgfcb,j!", Lang.En, true), ("lheu", Lang.En, true),

    // Нормальные английские слова -> не трогать
    ("hello", Lang.En, false), ("ok", Lang.En, false), ("the", Lang.En, false), ("google.com", Lang.En, false),
    ("windows", Lang.En, false), ("programming", Lang.En, false), ("test", Lang.En, false), ("github", Lang.En, false),
    ("Hello,", Lang.En, false), ("I'm", Lang.En, false), ("don't", Lang.En, false), ("strength", Lang.En, false),
    ("keyboard", Lang.En, false), ("switcher", Lang.En, false), ("layout", Lang.En, false), ("jquery", Lang.En, false),
    ("npm", Lang.En, false), ("USB", Lang.En, false), ("PDF", Lang.En, false), ("http", Lang.En, false),
    ("asap", Lang.En, false), ("lol", Lang.En, false), ("btw", Lang.En, false), ("wtf", Lang.En, false),
    ("getValue", Lang.En, false), ("password123", Lang.En, false), ("dotnet", Lang.En, false), ("javascript", Lang.En, false),
    ("beautiful", Lang.En, false), ("unbelievable", Lang.En, false), ("Claude", Lang.En, false), ("Anthropic", Lang.En, false),
    ("synchronization", Lang.En, false), ("refactoring", Lang.En, false), ("deployment", Lang.En, false),
    ("is", Lang.En, false), ("at", Lang.En, false), ("my", Lang.En, false), ("go", Lang.En, false), ("xyz", Lang.En, false),
    ("e.g.", Lang.En, false), ("etc.", Lang.En, false), ("i.e.", Lang.En, false), ("vs.", Lang.En, false),

    // Английские слова, набранные в русской раскладке -> переключать
    ("руддщ", Lang.Ru, true), ("цщкдв", Lang.Ru, true), ("ершы", Lang.Ru, true), ("ыщ", Lang.Ru, true),
    ("шы", Lang.Ru, true), ("пщщпду", Lang.Ru, true), ("ещ", Lang.Ru, true), ("фтв", Lang.Ru, true),
    ("еру", Lang.Ru, true), ("цштвщцы", Lang.Ru, true), ("пшерги", Lang.Ru, true), ("лунищфкв", Lang.Ru, true),
    ("ыщьуерштп", Lang.Ru, true), ("цщклштп", Lang.Ru, true), ("ьщтвфн", Lang.Ru, true), ("еуые", Lang.Ru, true),
    ("Руддщ", Lang.Ru, true), ("вщтэе", Lang.Ru, true), ("ьщкуб", Lang.Ru, true), ("зкщпкфь", Lang.Ru, true),

    // Нормальные русские слова -> не трогать
    ("привет", Lang.Ru, false), ("как", Lang.Ru, false), ("мы", Lang.Ru, false), ("не", Lang.Ru, false),
    ("ты", Lang.Ru, false), ("лог", Lang.Ru, false), ("ок", Lang.Ru, false), ("переключатель", Lang.Ru, false),
    ("раскладка", Lang.Ru, false), ("клавиатуры", Lang.Ru, false), ("достопримечательность", Lang.Ru, false),
    ("программист", Lang.Ru, false), ("разработчики", Lang.Ru, false), ("кот", Lang.Ru, false),
    ("щи", Lang.Ru, false), ("ёж", Lang.Ru, false), ("съел", Lang.Ru, false), ("объявление", Lang.Ru, false),
    ("Санкт-Петербург", Lang.Ru, false), ("кое-что", Lang.Ru, false), ("т.е.", Lang.Ru, false),
    ("смешно", Lang.Ru, false), ("короче", Lang.Ru, false), ("шёл", Lang.Ru, false), ("эх", Lang.Ru, false),
    ("юла", Lang.Ru, false), ("щука", Lang.Ru, false), ("жюри", Lang.Ru, false), ("ультразвук", Lang.Ru, false),
};

// Строим KeyStroke-последовательность для набранного текста в указанной раскладке.
static List<KeyStroke> ToKeys(string s, Lang lang)
{
    var keys = new List<KeyStroke>();
    foreach (char c in s)
    {
        bool found = false;
        foreach (int vk in Enumerable.Range(0, 256))
        {
            if (!KeyMap.IsWordKey(vk)) continue;
            foreach (bool sh in new[] { false, true })
            {
                var k = new KeyStroke(vk, sh, false);
                if (KeyMap.CharOf(k, lang) == c) { keys.Add(k); found = true; break; }
            }
            if (found) break;
        }
        if (!found) throw new Exception($"нет клавиши для '{c}'");
    }
    return keys;
}

var empty = new HashSet<string>();
int fails = 0;
foreach (var (typed, cur, expect) in cases)
{
    var keys = ToKeys(typed, cur);
    string orig = KeyMap.Render(keys, cur);
    string conv = KeyMap.Render(keys, KeyMap.OtherLang(cur));
    bool got = Detector.ShouldSwitch(orig, conv, cur, 2, empty, out _, out double sc, out double so);
    bool ok = got == expect;
    if (!ok) fails++;
    Console.WriteLine($"{(ok ? "  " : "XX")} {orig,-22} -> {conv,-22} cur={sc,6:F2} oth={so,6:F2} diff={so - sc,6:F2} switch={got}");
}
Console.WriteLine($"\nОшибок: {fails} из {cases.Length}");

// Перекодирование выделенного текста
Console.WriteLine(KeyMap.ConvertText("Ghbdtn? rfr ltkf/", out var t1) + "  -> " + t1);
Console.WriteLine(KeyMap.ConvertText("Руддщб цщкдв!", out var t2) + "  -> " + t2);
return fails == 0 ? 0 : 1;
