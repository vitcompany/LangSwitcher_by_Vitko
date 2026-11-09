using static LangSwitcher.Native;

namespace LangSwitcher;

/// <summary>Перекодирование выделенного текста через буфер обмена (UI-поток, STA).</summary>
internal static class SelectionConverter
{
    private static bool _busy;
    private const string NoHistoryFormat = "ExcludeClipboardContentFromMonitorProcessing";

    public static async void Run()
    {
        if (_busy) return;
        _busy = true;
        Dictionary<string, object>? backup = null;
        try
        {
            IntPtr fg = GetForegroundWindow();
            bool console = Layout.IsConsole(fg);   // в терминале Ctrl+C = прерывание, используем Ctrl+Insert
            backup = SaveClipboard();

            uint seq = GetClipboardSequenceNumber();
            if (console) Input.Combo(Input.VK_CONTROL, Input.VK_INSERT, keyExtended: true);
            else Input.Combo(Input.VK_CONTROL, Input.VK_C);

            string? text = null;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(15);
                if (GetClipboardSequenceNumber() == seq) continue;
                await Task.Delay(30);
                text = Retry(() => Clipboard.ContainsText() ? Clipboard.GetText() : null);
                break;
            }
            if (string.IsNullOrEmpty(text)) { RestoreClipboard(backup); return; }

            string converted = KeyMap.ConvertText(text, out Lang target);
            if (converted == text) { RestoreClipboard(backup); return; }

            var data = new DataObject();
            data.SetText(converted, TextDataFormat.UnicodeText);
            data.SetData(NoHistoryFormat, new MemoryStream(new byte[4])); // не засорять журнал Win+V
            if (!Retry(() => { Clipboard.SetDataObject(data, true); return true; }))
            {
                RestoreClipboard(backup);
                return;
            }

            await Task.Delay(30);
            if (console) Input.Combo(Input.VK_SHIFT, Input.VK_INSERT, keyExtended: true);
            else Input.Combo(Input.VK_CONTROL, Input.VK_V);
            Layout.SwitchTo(target);

            await Task.Delay(100);
            // Выделяем результат обратно, чтобы повторный Shift+Shift вернул как было.
            string flat = converted.Replace("\r", "");
            if (!console && flat.Length <= 300 && !flat.Contains('\n'))
                Input.SelectLeft(flat.Length);

            await Task.Delay(400);
            RestoreClipboard(backup);
        }
        catch
        {
            if (backup != null) RestoreClipboard(backup);
        }
        finally
        {
            _busy = false;
        }
    }

    private static T? Retry<T>(Func<T?> f)
    {
        for (int i = 0; i < 5; i++)
        {
            try { return f(); }
            catch (System.Runtime.InteropServices.ExternalException) { Thread.Sleep(30); }
        }
        return default;
    }

    private static Dictionary<string, object> SaveClipboard()
    {
        var saved = new Dictionary<string, object>();
        try
        {
            var d = Clipboard.GetDataObject();
            if (d == null) return saved;
            foreach (string f in d.GetFormats(false))
            {
                try
                {
                    var v = d.GetData(f, false);
                    if (v != null) saved[f] = v;
                }
                catch { /* некоторые форматы прочитать нельзя — пропускаем */ }
            }
        }
        catch { }
        return saved;
    }

    private static void RestoreClipboard(Dictionary<string, object> saved)
    {
        Retry(() =>
        {
            if (saved.Count == 0) { Clipboard.Clear(); return true; }
            var d = new DataObject();
            foreach (var (f, v) in saved) d.SetData(f, false, v);
            Clipboard.SetDataObject(d, true);
            return true;
        });
    }
}
