using System.Runtime.InteropServices;
using System.Text;
using static LangSwitcher.Native;

namespace LangSwitcher;

/// <summary>Определение и смена раскладки активного окна.</summary>
internal static class Layout
{
    private static (IntPtr Hwnd, uint Tid) FocusTarget()
    {
        IntPtr fg = GetForegroundWindow();
        uint tid = GetWindowThreadProcessId(fg, out _);
        var gti = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        if (tid != 0 && GetGUIThreadInfo(tid, ref gti) && gti.hwndFocus != IntPtr.Zero)
            return (gti.hwndFocus, GetWindowThreadProcessId(gti.hwndFocus, out _));
        return (fg, tid);
    }

    public static Lang LangOf(IntPtr hkl) => ((long)hkl & 0x3FF) switch
    {
        0x09 => Lang.En,
        0x19 => Lang.Ru,
        _ => Lang.Other,
    };

    public static Lang Current() => LangOf(GetKeyboardLayout(FocusTarget().Tid));

    public static IntPtr FindHkl(Lang lang)
    {
        int n = GetKeyboardLayoutList(0, null);
        if (n <= 0) return IntPtr.Zero;
        var list = new IntPtr[n];
        GetKeyboardLayoutList(n, list);
        foreach (var h in list)
            if (LangOf(h) == lang) return h;
        return IntPtr.Zero;
    }

    public static bool HasBoth() => FindHkl(Lang.En) != IntPtr.Zero && FindHkl(Lang.Ru) != IntPtr.Zero;

    public static void SwitchTo(Lang lang)
    {
        IntPtr hkl = FindHkl(lang);
        if (hkl == IntPtr.Zero) return;
        PostMessage(FocusTarget().Hwnd, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
    }

    public static bool IsConsole(IntPtr hwnd)
    {
        var sb = new StringBuilder(128);
        GetClassName(hwnd, sb, sb.Capacity);
        string cls = sb.ToString();
        return cls is "ConsoleWindowClass" or "CASCADIA_HOSTING_WINDOW_CLASS" or "mintty" or "PuTTY";
    }
}
