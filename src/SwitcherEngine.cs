using System.Runtime.InteropServices;
using static LangSwitcher.Native;

namespace LangSwitcher;

/// <summary>
/// Ядро: глобальные хуки клавиатуры и мыши на отдельном потоке, буфер последнего слова,
/// автопереключение по пробелу/Enter/Tab и ручное переключение по двойному Shift.
/// Всё состояние слова меняется только в потоке хуков.
/// </summary>
internal sealed class SwitcherEngine : IDisposable
{
    private const uint WM_APP_MANUAL = 0x8001;
    private const int ShiftTapMaxMs = 350;     // Shift нажат не дольше этого — это «тап»
    private const int DoubleTapMaxMs = 500;    // между двумя тапами не дольше этого

    private const int VK_BACK = 0x08, VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_CONTROL = 0x11,
        VK_MENU = 0x12, VK_CAPITAL = 0x14, VK_SPACE = 0x20, VK_PACKET = 0xE7,
        VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1, VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3,
        VK_LMENU = 0xA4, VK_RMENU = 0xA5, VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    private readonly SynchronizationContext _ui;
    private Thread? _thread;
    private volatile uint _threadId;
    private readonly ManualResetEventSlim _started = new();
    private LowLevelProc? _kbProc, _msProc;
    private IntPtr _kbHook, _msHook;

    public volatile bool AutoEnabled = true;
    public volatile int MinLetters = 2;
    public volatile bool LearnFromUndo = true;
    private volatile IReadOnlySet<string> _exceptions = new HashSet<string>();

    /// <summary>Слово добавлено в исключения после отмены автопереключения (вызывается в UI-потоке).</summary>
    public event Action<string>? WordLearned;
    /// <summary>Двойной Shift без набранного слова — конвертировать выделение (UI-поток).</summary>
    public event Action? SelectionRequested;

    // --- состояние потока хуков ---
    private readonly List<KeyStroke> _word = new();
    private int _trailingSpaces;
    private Lang _wordLang;
    private IntPtr _wordWindow;
    private string? _autoOriginal;   // что было набрано до последнего автопереключения
    private bool _manualTouched;     // слово переключали вручную — автоматически больше не трогаем
    private bool _lShift, _rShift, _lCtrl, _rCtrl, _lAlt, _rAlt, _lWin, _rWin;
    private bool _caps, _capsDown;
    private bool _tapCandidate;
    private long _shiftDownAt, _lastTapUpAt;
    private Lang _forcedLang;
    private long _forcedAt;

    public SwitcherEngine(SynchronizationContext ui)
    {
        _ui = ui;
        _caps = (GetKeyState(VK_CAPITAL) & 1) != 0;
    }

    public bool HooksInstalled => _kbHook != IntPtr.Zero;

    public void SetExceptions(IEnumerable<string> words) =>
        _exceptions = new HashSet<string>(words.Select(w => Detector.Normalize(w.Trim())).Where(w => w.Length > 0));

    public void Start()
    {
        _thread = new Thread(ThreadMain) { IsBackground = true, Name = "LangSwitcher hooks", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
        _started.Wait(3000);
    }

    public void Dispose()
    {
        if (_threadId != 0) PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread?.Join(1000);
    }

    private void ThreadMain()
    {
        _threadId = GetCurrentThreadId();
        PeekMessage(out _, IntPtr.Zero, 0, 0, PM_NOREMOVE); // создаём очередь сообщений потока
        _kbProc = KeyboardProc;
        _msProc = MouseProc;
        IntPtr hMod = GetModuleHandle(null);
        _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, hMod, 0);
        _msHook = SetWindowsHookEx(WH_MOUSE_LL, _msProc, hMod, 0);
        _started.Set();

        while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.hwnd == IntPtr.Zero && msg.message == WM_APP_MANUAL)
            {
                try { DoManual(); } catch { /* не роняем поток хуков */ }
                continue;
            }
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_kbHook != IntPtr.Zero) UnhookWindowsHookEx(_kbHook);
        if (_msHook != IntPtr.Zero) UnhookWindowsHookEx(_msHook);
    }

    // ================= хуки =================

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                var k = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (k.dwExtraInfo != Input.Magic)
                {
                    int msg = (int)wParam;
                    bool down = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
                    bool block = down ? OnKeyDown((int)k.vkCode) : OnKeyUp((int)k.vkCode);
                    if (block) return 1;
                }
            }
            catch { /* никогда не бросаем исключения из хука */ }
        }
        return CallNextHookEx(_kbHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN)
            {
                ResetWord();             // курсор мог переместиться — старое слово больше не актуально
                _tapCandidate = false;
                _lastTapUpAt = 0;
            }
        }
        return CallNextHookEx(_msHook, nCode, wParam, lParam);
    }

    private bool ShiftDown => _lShift || _rShift;
    private bool OtherModsDown => _lCtrl || _rCtrl || _lAlt || _rAlt || _lWin || _rWin;

    private bool OnKeyDown(int vk)
    {
        switch (vk)
        {
            case VK_LSHIFT or VK_RSHIFT or VK_SHIFT:
                bool was = ShiftDown;
                if (vk == VK_RSHIFT) _rShift = true; else _lShift = true;
                if (!was)
                {
                    _tapCandidate = !OtherModsDown;
                    _shiftDownAt = Environment.TickCount64;
                }
                return false;
            case VK_LCONTROL or VK_RCONTROL or VK_CONTROL:
                if (vk == VK_RCONTROL) _rCtrl = true; else _lCtrl = true;
                CancelTap(); return false;
            case VK_LMENU or VK_RMENU or VK_MENU:
                if (vk == VK_RMENU) _rAlt = true; else _lAlt = true;
                CancelTap(); return false;
            case VK_LWIN or VK_RWIN:
                if (vk == VK_RWIN) _rWin = true; else _lWin = true;
                CancelTap(); return false;
            case VK_CAPITAL:
                if (!_capsDown) { _caps = !_caps; _capsDown = true; }
                CancelTap(); return false;
        }

        CancelTap();

        if (OtherModsDown) { ResetWord(); return false; }   // сочетания клавиш

        if (vk == VK_BACK)
        {
            if (_trailingSpaces > 0) _trailingSpaces--;
            else if (_word.Count > 0) _word.RemoveAt(_word.Count - 1);
            _autoOriginal = null;
            if (_word.Count == 0) ResetWord();
            return false;
        }

        if (vk is VK_SPACE or VK_RETURN or VK_TAB)
        {
            if (_word.Count > 0 && _trailingSpaces == 0 && AutoEnabled && !_manualTouched && TryAutoSwitch(vk))
                return true;                                   // разделитель отправлен заново
            if (vk == VK_SPACE && _word.Count > 0 && _trailingSpaces < 3) _trailingSpaces++;
            else ResetWord();
            return false;
        }

        if (KeyMap.IsWordKey(vk))
        {
            IntPtr fg = GetForegroundWindow();
            if (_trailingSpaces > 0 || fg != _wordWindow) ResetWord();
            if (_word.Count == 0)
            {
                _wordLang = CurrentLang();
                _wordWindow = fg;
            }
            _word.Add(new KeyStroke(vk, ShiftDown, _caps));
            _autoOriginal = null;
            if (_word.Count > 60) ResetWord();
            return false;
        }

        // Стрелки, Home/End, Delete, Esc, F-клавиши, VK_PACKET и т.д.
        ResetWord();
        return false;
    }

    private bool OnKeyUp(int vk)
    {
        switch (vk)
        {
            case VK_LSHIFT or VK_RSHIFT or VK_SHIFT:
                if (vk == VK_RSHIFT) _rShift = false; else _lShift = false;
                if (!ShiftDown && _tapCandidate)
                {
                    long now = Environment.TickCount64;
                    if (now - _shiftDownAt <= ShiftTapMaxMs)
                    {
                        if (_lastTapUpAt != 0 && now - _lastTapUpAt <= DoubleTapMaxMs)
                        {
                            _lastTapUpAt = 0;
                            // выполняем после возврата из хука, чтобы отпускание Shift дошло до приложения
                            PostThreadMessage(_threadId, WM_APP_MANUAL, IntPtr.Zero, IntPtr.Zero);
                        }
                        else _lastTapUpAt = now;
                    }
                    else _lastTapUpAt = 0;
                }
                if (!ShiftDown) _tapCandidate = false;
                break;
            case VK_LCONTROL or VK_RCONTROL or VK_CONTROL:
                if (vk == VK_RCONTROL) _rCtrl = false; else _lCtrl = false;
                break;
            case VK_LMENU or VK_RMENU or VK_MENU:
                if (vk == VK_RMENU) _rAlt = false; else _lAlt = false;
                break;
            case VK_LWIN or VK_RWIN:
                if (vk == VK_RWIN) _rWin = false; else _lWin = false;
                break;
            case VK_CAPITAL:
                _capsDown = false;
                break;
        }
        return false;
    }

    private void CancelTap()
    {
        _tapCandidate = false;
        _lastTapUpAt = 0;
    }

    private void ResetWord()
    {
        _word.Clear();
        _trailingSpaces = 0;
        _autoOriginal = null;
        _manualTouched = false;
    }

    private Lang CurrentLang()
    {
        // Сразу после нашего переключения система может ещё не успеть сменить раскладку.
        if (_forcedLang != Lang.Other && Environment.TickCount64 - _forcedAt < 400) return _forcedLang;
        return Layout.Current();
    }

    private void SwitchLayout(Lang to)
    {
        Layout.SwitchTo(to);
        _forcedLang = to;
        _forcedAt = Environment.TickCount64;
    }

    // ================= действия =================

    /// <summary>Вызывается на разделителе. true — слово исправлено, исходное нажатие надо заблокировать.</summary>
    private bool TryAutoSwitch(int delimiterVk)
    {
        Lang cur = _wordLang;
        if (cur == Lang.Other) return false;
        Lang oth = KeyMap.OtherLang(cur);
        string orig = KeyMap.Render(_word, cur);
        string conv = KeyMap.Render(_word, oth);

        if (!Detector.ShouldSwitch(orig, conv, cur, MinLetters, _exceptions, out string origCore, out _, out _))
            return false;
        if (Layout.FindHkl(oth) == IntPtr.Zero) return false;

        var inp = new List<INPUT>();
        Input.Tap(inp, VK_BACK, _word.Count);
        Input.Text(inp, conv);
        Input.Tap(inp, delimiterVk);
        Input.Send(inp);
        SwitchLayout(oth);

        _wordLang = oth;
        if (delimiterVk == VK_SPACE)
        {
            _trailingSpaces = 1;
            _autoOriginal = origCore;   // если пользователь сразу отменит — запомним слово
        }
        else ResetWord();
        return true;
    }

    /// <summary>Двойной Shift: переключить последнее слово, а если его нет — выделенный текст.</summary>
    private void DoManual()
    {
        IntPtr fg = GetForegroundWindow();
        if (_word.Count == 0 || fg != _wordWindow)
        {
            ResetWord();
            _ui.Post(_ => SelectionRequested?.Invoke(), null);
            return;
        }

        Lang from = _wordLang != Lang.Other ? _wordLang : CurrentLang();
        if (from == Lang.Other) from = Lang.En;
        Lang to = KeyMap.OtherLang(from);

        string text = KeyMap.Render(_word, to) + new string(' ', _trailingSpaces);
        var inp = new List<INPUT>();
        Input.Tap(inp, VK_BACK, _word.Count + _trailingSpaces);
        Input.Text(inp, text);
        Input.Send(inp);
        SwitchLayout(to);
        _wordLang = to;
        _manualTouched = true;

        // Отмена автопереключения → слово в исключения.
        if (_autoOriginal is { Length: > 0 } learned && LearnFromUndo)
        {
            _exceptions = new HashSet<string>(_exceptions) { learned };
            _ui.Post(_ => WordLearned?.Invoke(learned), null);
        }
        _autoOriginal = null;
    }
}
