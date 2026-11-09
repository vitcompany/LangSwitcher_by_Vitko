using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace LangSwitcher;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly SwitcherEngine _engine;
    private readonly AppSettings _settings;
    private List<string> _exceptions;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<(Lang, bool), Icon> _icons = new();
    private readonly ToolStripMenuItem _autoItem;
    private (Lang, bool)? _shown;
    private Form? _openForm;

    public TrayApp()
    {
        _settings = AppSettings.Load();
        _exceptions = AppSettings.LoadExceptions();
        Detector.LoadUserWords(AppSettings.Dir);
        _ = Detector.En; // построить модели заранее, а не при первом слове

        var menu = new ContextMenuStrip();
        _autoItem = new ToolStripMenuItem("Автопереключение раскладки", null, (_, _) => SetAuto(!_settings.AutoSwitch))
        {
            Checked = _settings.AutoSwitch,
            Font = new Font(menu.Font, FontStyle.Bold),
        };
        menu.Items.Add(_autoItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Словарь исключений…", null, (_, _) => EditExceptions());
        menu.Items.Add("Настройки…", null, (_, _) => EditSettings());
        menu.Items.Add("Справка", null, (_, _) => ShowHelp());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => Quit());

        // ContextMenuStrip — это Control, после его создания WinForms-контекст синхронизации уже установлен.
        var ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _engine = new SwitcherEngine(ui)
        {
            AutoEnabled = _settings.AutoSwitch,
            MinLetters = _settings.MinLetters,
            LearnFromUndo = _settings.LearnFromUndo,
        };
        _engine.SetExceptions(_exceptions);
        _engine.WordLearned += OnWordLearned;
        _engine.SelectionRequested += SelectionConverter.Run;

        _tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) SetAuto(!_settings.AutoSwitch); };
        UpdateIcon();

        _engine.Start();
        if (!_engine.HooksInstalled)
            MessageBox.Show("Не удалось установить перехват клавиатуры.", "LangSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (!Layout.HasBoth())
            _tray.ShowBalloonTip(5000, "LangSwitcher",
                "В системе должны быть установлены английская и русская раскладки.", ToolTipIcon.Warning);

        _timer = new System.Windows.Forms.Timer { Interval = 250 };
        _timer.Tick += (_, _) => UpdateIcon();
        _timer.Start();
    }

    // ---------- иконка ----------

    private void UpdateIcon()
    {
        Lang lang;
        try { lang = Layout.Current(); } catch { lang = Lang.Other; }
        var key = (lang, _settings.AutoSwitch);
        if (_shown == key) return;
        _shown = key;
        if (!_icons.TryGetValue(key, out var icon)) _icons[key] = icon = DrawIcon(lang, _settings.AutoSwitch);
        _tray.Icon = icon;
        string name = lang switch { Lang.En => "English", Lang.Ru => "Русский", _ => "другая раскладка" };
        _tray.Text = $"LangSwitcher — {name}\nАвтопереключение: {(_settings.AutoSwitch ? "включено" : "выключено")}";
    }

    private static Icon DrawIcon(Lang lang, bool auto)
    {
        int s = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using var bmp = new Bitmap(s, s);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            Color bg = !auto ? Color.FromArgb(128, 128, 128)
                : lang == Lang.Ru ? Color.FromArgb(206, 52, 42)
                : lang == Lang.En ? Color.FromArgb(30, 102, 208)
                : Color.FromArgb(90, 90, 90);
            float r = s / 4f;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(s - 1 - r * 2, 0, r * 2, r * 2, 270, 90);
                path.AddArc(s - 1 - r * 2, s - 1 - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0, s - 1 - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);
            }

            string text = lang switch { Lang.En => "EN", Lang.Ru => "RU", _ => "··" };
            using var font = new Font("Segoe UI", s * 0.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, Brushes.White, new RectangleF(0, 0, s, s + 1), fmt);

            if (!auto) // перечёркнуто — автопереключение выключено
            {
                using var pen = new Pen(Color.FromArgb(230, 255, 255, 255), Math.Max(1.5f, s / 12f));
                g.DrawLine(pen, s * 0.15f, s * 0.85f, s * 0.85f, s * 0.15f);
            }
        }
        IntPtr h = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(h).Clone();
        Native.DestroyIcon(h);
        return icon;
    }

    // ---------- действия меню ----------

    private void SetAuto(bool on)
    {
        _settings.AutoSwitch = on;
        _settings.Save();
        _engine.AutoEnabled = on;
        _autoItem.Checked = on;
        UpdateIcon();
    }

    private void OnWordLearned(string word)
    {
        if (!_exceptions.Contains(word, StringComparer.OrdinalIgnoreCase))
        {
            _exceptions.Add(word);
            AppSettings.SaveExceptions(_exceptions);
        }
        _tray.ShowBalloonTip(2500, "LangSwitcher", $"«{word}» добавлено в исключения", ToolTipIcon.Info);
    }

    private bool BringOpenForm()
    {
        if (_openForm is { IsDisposed: false }) { _openForm.Activate(); return true; }
        return false;
    }

    private void EditExceptions()
    {
        if (BringOpenForm()) return;
        using var f = new ExceptionsForm(_exceptions);
        _openForm = f;
        if (f.ShowDialog() == DialogResult.OK)
        {
            _exceptions = f.Words;
            AppSettings.SaveExceptions(_exceptions);
            _engine.SetExceptions(_exceptions);
        }
        _openForm = null;
    }

    private void EditSettings()
    {
        if (BringOpenForm()) return;
        bool autostart = false;
        try { autostart = AppSettings.Autostart; } catch { }
        using var f = new SettingsForm(_settings, autostart);
        _openForm = f;
        if (f.ShowDialog() == DialogResult.OK)
        {
            _settings.MinLetters = (int)f.MinLetters.Value;
            _settings.LearnFromUndo = f.Learn.Checked;
            _engine.MinLetters = _settings.MinLetters;
            _engine.LearnFromUndo = _settings.LearnFromUndo;
            SetAuto(f.Auto.Checked); // сохраняет настройки
            try { if (f.Autostart.Checked != autostart) AppSettings.Autostart = f.Autostart.Checked; }
            catch (Exception ex) { MessageBox.Show("Не удалось изменить автозапуск: " + ex.Message, "LangSwitcher"); }
        }
        _openForm = null;
    }

    private static void ShowHelp() =>
        MessageBox.Show(
            "LangSwitcher — автоматическое переключение раскладки En/Ru.\n\n" +
            "• Слово, набранное не в той раскладке, исправляется после пробела, Enter или Tab\n" +
            "  (слова короче заданного числа букв не трогаются).\n" +
            "• Shift, Shift (двойное нажатие) — переключить последнее слово.\n" +
            "  Если слова нет (кликнули мышью, выделили текст) — переключается выделенный текст.\n" +
            "  Повторный двойной Shift возвращает как было.\n" +
            "• Отмена автопереключения двойным Shift добавляет слово в исключения.\n" +
            "• Щелчок по значку в трее — включить/выключить автопереключение.\n\n" +
            "Файлы настроек: " + AppSettings.Dir,
            "LangSwitcher — справка", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void Quit()
    {
        _timer.Stop();
        _engine.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        foreach (var i in _icons.Values) i.Dispose();
        ExitThread();
    }
}
