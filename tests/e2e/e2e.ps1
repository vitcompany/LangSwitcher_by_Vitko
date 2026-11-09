Add-Type -AssemblyName System.Windows.Forms
Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -TypeDefinition @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public static class H {
    [StructLayout(LayoutKind.Sequential)] struct KI { public ushort vk, scan; public uint flags, time; public IntPtr extra; }
    [StructLayout(LayoutKind.Explicit, Size = 32)] struct U { [FieldOffset(0)] public KI ki; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public U u; }
    [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] i, int cb);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    public static Form F; public static TextBox T;

    public static void Pump(int ms) {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(5); }
    }
    public static bool Init() {
        F = new Form(); F.Text = "LangSwitcher e2e"; F.TopMost = true; F.Width = 700; F.Height = 200;
        T = new TextBox(); T.Multiline = true; T.Dock = DockStyle.Fill; T.Font = new System.Drawing.Font("Consolas", 14);
        F.Controls.Add(T); F.Show(); Pump(200);
        for (int i = 0; i < 5 && !Fg(); i++) {
            keybd_event(0x87, 0, 0, UIntPtr.Zero); keybd_event(0x87, 0, 2, UIntPtr.Zero); // F24 — разблокировать смену фокуса
            SetForegroundWindow(F.Handle); F.Activate(); Pump(200);
        }
        T.Focus(); Pump(100);
        return Fg();
    }
    public static bool Fg() { return GetForegroundWindow() == F.Handle; }
    static void Key(ushort vk, bool up) {
        if (!Fg()) throw new Exception("test window lost focus, aborting");
        var a = new INPUT[1]; a[0].type = 1; a[0].u.ki.vk = vk; a[0].u.ki.flags = up ? 2u : 0u;
        SendInput(1, a, Marshal.SizeOf(typeof(INPUT)));
        Pump(12);
    }
    static ushort Vk(char c) {
        if (c >= 'a' && c <= 'z') return (ushort)char.ToUpper(c);
        switch (c) {
            case ' ': return 0x20; case ',': return 0xBC; case '.': return 0xBE; case ';': return 0xBA;
            case '\'': return 0xDE; case '[': return 0xDB; case ']': return 0xDD; case (char)96: return 0xC0;case '/': return 0xBF;
        }
        throw new Exception("no vk for " + c);
    }
    public static void Type(string s) {
        foreach (char ch in s) {
            bool up = char.IsUpper(ch); ushort vk = Vk(char.ToLower(ch));
            if (up) Key(0xA0, false);
            Key(vk, false); Key(vk, true);
            if (up) Key(0xA0, true);
        }
        Pump(350);
    }
    public static void Key2(string what) { // '?' = Shift + /
        Key(0xA0, false); Key(0xBF, false); Key(0xBF, true); Key(0xA0, true); Pump(50);
    }
    public static void ShiftShift() {
        Key(0xA0, false); Key(0xA0, true); Pump(60);
        Key(0xA1, false); Key(0xA1, true);
        Pump(900);
    }
    public static void CtrlA() { Key(0xA2, false); Key(0x41, false); Key(0x41, true); Key(0xA2, true); Pump(150); }
    public static void Clear() { T.Text = ""; Key(0x23, false); Key(0x23, true); Pump(100); } // End сбрасывает буфер
    public static string Lang() { return InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName; }
    public static void SetLang(string two) {
        foreach (InputLanguage l in InputLanguage.InstalledInputLanguages)
            if (l.Culture.TwoLetterISOLanguageName == two) InputLanguage.CurrentInputLanguage = l;
        Pump(150);
    }
    public static string Sel() { return T.SelectedText; }
}
"@

$results = @()
function Check($name, $expected, $lang) {
    $actual = [H]::T.Text
    $l = [H]::Lang()
    $ok = ($actual -ceq $expected) -and ((-not $lang) -or $l -eq $lang)
    $script:results += [pscustomobject]@{ Ok = $(if ($ok) {'OK'} else {'FAIL'}); Test = $name; Text = $actual; Layout = $l }
}

if (-not [H]::Init()) { Write-Output "Could not focus test window"; [H]::F.Close(); exit 2 }
try {
    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('ghbdtn ');          Check 'auto en->ru'          'привет ' 'ru'
    [H]::Type('rfr ');             Check 'continue in ru'       'привет как ' 'ru'
    [H]::Type('hello ');           Check 'auto ru->en'          'привет как hello ' 'en'
    [H]::Type('world ');           Check 'continue in en'       'привет как hello world ' 'en'
    [H]::Type('Ghbdtn');           [H]::Key2('?'); [H]::Type(' ')
                                   Check 'capital + ru comma'   'привет как hello world Привет, ' 'ru'
    [H]::Type('k.,k. ');           Check 'typed in ru, stays'   'привет как hello world Привет, люблю ' 'ru'
    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('k.,k. ');           Check 'word with ю/б keys'   'люблю ' 'ru'

    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('ntcn');             [H]::ShiftShift(); Check 'manual last word'   'тест' 'ru'
    [H]::ShiftShift();             Check 'manual toggle back'   'ntcn' 'en'
    [H]::Type(' ');                Check 'no auto after manual undo' 'ntcn ' 'en'

    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('lf ');              Check 'two-letter word'      'да ' 'ru'
    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('z ');               Check 'one letter untouched' 'z ' 'en'

    [H]::SetLang('en'); [H]::Clear()
    [H]::Type('ghbdtn ');          [H]::ShiftShift(); Check 'undo auto -> learn'  'ghbdtn ' 'en'
    [H]::Clear(); [H]::SetLang('en')
    [H]::Type('ghbdtn ');          Check 'learned word not switched' 'ghbdtn ' 'en'

    [H]::SetLang('en'); [H]::Clear()
    [H]::T.Text = 'ghbdtn vbh'; [H]::T.SelectAll(); [H]::Pump(100)
    [H]::ShiftShift();             Check 'selection convert'    'привет мир' 'ru'
    $script:results += [pscustomobject]@{ Ok = $(if ([H]::Sel() -eq 'привет мир') {'OK'} else {'FAIL'}); Test = 'reselected'; Text = [H]::Sel(); Layout = '' }
    [H]::ShiftShift();             Check 'selection toggle back' 'ghbdtn vbh' 'en'
}
catch { Write-Output "ERROR: $_" }
finally { [H]::F.Close() }
$results | Format-Table -AutoSize | Out-String -Width 200
