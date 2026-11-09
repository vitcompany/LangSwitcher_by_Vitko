namespace LangSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\LangSwitcher.SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show("LangSwitcher уже запущен — значок в области уведомлений.", "LangSwitcher",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}
