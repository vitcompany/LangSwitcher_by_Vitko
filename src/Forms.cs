namespace LangSwitcher;

internal sealed class ExceptionsForm : Form
{
    private readonly TextBox _box;

    public List<string> Words => _box.Lines.Select(l => l.Trim()).Where(l => l.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public ExceptionsForm(IEnumerable<string> words)
    {
        Text = "LangSwitcher — словарь исключений";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 480);
        MinimumSize = new Size(360, 320);
        Font = new Font("Segoe UI", 9f);
        ShowInTaskbar = true;
        TopMost = true;

        _box = new TextBox
        {
            Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11f), Lines = words.ToArray(), WordWrap = false,
        };
        var hint = new Label
        {
            Dock = DockStyle.Top, Height = 76, Padding = new Padding(8, 8, 8, 0),
            Text = "Слова из этого списка никогда не переключаются автоматически.\n" +
                   "Одно слово на строку. Можно писать как набранную форму (ghbdtn), так и итоговую (привет).\n" +
                   "Слова также добавляются сами, если отменить автопереключение двойным Shift.",
        };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 44, Padding = new Padding(6) };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        Controls.Add(_box);       // Fill добавляется первым, чтобы занять оставшееся место
        Controls.Add(hint);
        Controls.Add(buttons);
        CancelButton = cancel;
    }
}

internal sealed class SettingsForm : Form
{
    public readonly CheckBox Auto, Learn, Autostart;
    public readonly NumericUpDown MinLetters;

    public SettingsForm(AppSettings s, bool autostart)
    {
        Text = "LangSwitcher — настройки";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9f);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        TopMost = true;

        var t = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(12), Dock = DockStyle.Fill };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Auto = new CheckBox { Text = "Автоматически переключать раскладку", Checked = s.AutoSwitch, AutoSize = true };
        t.Controls.Add(Auto); t.SetColumnSpan(Auto, 2);

        t.Controls.Add(new Label { Text = "Переключать слова длиной от (букв):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
        MinLetters = new NumericUpDown { Minimum = 1, Maximum = 10, Value = Math.Clamp(s.MinLetters, 1, 10), Width = 60 };
        t.Controls.Add(MinLetters);

        Learn = new CheckBox { Text = "Добавлять слово в исключения, если автопереключение\nотменено двойным Shift", Checked = s.LearnFromUndo, AutoSize = true };
        t.Controls.Add(Learn); t.SetColumnSpan(Learn, 2);

        Autostart = new CheckBox { Text = "Запускать вместе с Windows", Checked = autostart, AutoSize = true };
        t.Controls.Add(Autostart); t.SetColumnSpan(Autostart, 2);

        var help = new Label
        {
            AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 12, 3, 3),
            Text = "Shift, Shift (двойное нажатие) — переключить последнее слово\n" +
                   "или выделенный текст. Повторное нажатие возвращает как было.",
        };
        t.Controls.Add(help); t.SetColumnSpan(help, 2);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        t.Controls.Add(buttons); t.SetColumnSpan(buttons, 2);

        Controls.Add(t);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
