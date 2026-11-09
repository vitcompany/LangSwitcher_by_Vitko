using System.Runtime.InteropServices;
using static LangSwitcher.Native;

namespace LangSwitcher;

/// <summary>Эмуляция нажатий. Все наши события помечаются <see cref="Magic"/>,
/// чтобы собственный хук их игнорировал.</summary>
internal static class Input
{
    public static readonly IntPtr Magic = 0x4C53574D; // "LSWM"

    public const int VK_BACK = 0x08, VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_CONTROL = 0x11,
        VK_SPACE = 0x20, VK_LEFT = 0x25, VK_INSERT = 0x2D, VK_C = 0x43, VK_V = 0x56;

    public static void Key(List<INPUT> l, int vk, bool up, bool extended = false)
    {
        l.Add(new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = (ushort)MapVirtualKey((uint)vk, 0),
                    dwFlags = (up ? KEYEVENTF_KEYUP : 0) | (extended ? KEYEVENTF_EXTENDEDKEY : 0),
                    dwExtraInfo = Magic,
                }
            }
        });
    }

    public static void Tap(List<INPUT> l, int vk, int count = 1, bool extended = false)
    {
        for (int i = 0; i < count; i++) { Key(l, vk, false, extended); Key(l, vk, true, extended); }
    }

    public static void Text(List<INPUT> l, string s)
    {
        foreach (char c in s)
        {
            if (c == '\r') continue;
            if (c == '\n') { Tap(l, VK_RETURN); continue; }
            if (c == '\t') { Tap(l, VK_TAB); continue; }
            for (int up = 0; up < 2; up++)
                l.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = c,
                            dwFlags = KEYEVENTF_UNICODE | (up == 1 ? KEYEVENTF_KEYUP : 0),
                            dwExtraInfo = Magic,
                        }
                    }
                });
        }
    }

    public static void Send(List<INPUT> l)
    {
        if (l.Count > 0) SendInput((uint)l.Count, l.ToArray(), Marshal.SizeOf<INPUT>());
    }

    /// <summary>Модификатор + клавиша (Ctrl+C, Shift+Insert и т.п.).</summary>
    public static void Combo(int mod, int key, bool keyExtended = false)
    {
        var l = new List<INPUT>();
        Key(l, mod, false);
        Key(l, key, false, keyExtended);
        Key(l, key, true, keyExtended);
        Key(l, mod, true);
        Send(l);
    }

    /// <summary>Выделить n символов слева от курсора (Shift+Left × n).</summary>
    public static void SelectLeft(int n)
    {
        var l = new List<INPUT>();
        Key(l, VK_SHIFT, false);
        Tap(l, VK_LEFT, n, extended: true);
        Key(l, VK_SHIFT, true);
        Send(l);
    }
}
