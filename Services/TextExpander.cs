using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;
using TextBlaze.Models;

namespace TextBlaze.Services;

// Watches keystrokes, keeps a rolling buffer of printable characters, and
// when the buffer matches a snippet shortcut followed by the trigger key,
// expands it: backspaces out the typed shortcut, then types the replacement.
public sealed class TextExpander : IDisposable
{
    private readonly SnippetStore _store;
    private readonly KeyboardHook _hook;
    private readonly StringBuilder _buffer = new();
    private readonly object _gate = new();
    private bool _expanding;

    public string TriggerKey { get; set; } = "Tab";
    public bool Enabled { get; set; } = true;

    public TextExpander(SnippetStore store)
    {
        _store = store;
        _hook = new KeyboardHook();
        _hook.KeySignal += OnKey;
    }

    private void OnKey(int vk, bool down)
    {
        if (!Enabled || _expanding) return;

        var key = MapKey(vk);
        lock (_gate)
        {
            if (vk == KeyboardHook.VK_BACK)
            {
                if (_buffer.Length > 0) _buffer.Length--;
                return;
            }

            if (key == TriggerKey)
            {
                var typed = _buffer.ToString();
                _buffer.Clear();
                if (typed.Length > 0)
                    TryExpand(typed);
                return;
            }

            if (key is null)
            {
                _buffer.Clear();
                return;
            }

            _buffer.Append(key);
            if (_buffer.Length > 128) _buffer.Remove(0, _buffer.Length - 128);
        }
    }

    private void TryExpand(string typed)
    {
        var snippet = _store.FindByShortcut(typed);
        if (snippet is null) return;

        _expanding = true;
        try
        {
            var ctx = new SnippetRenderContext
            {
                ClipboardText = ClipboardFacade.GetText(),
            };
            var rendered = PlaceholderEngine.Render(snippet.Body, ctx);

            for (int i = 0; i < typed.Length; i++)
                SendKey(KeyboardHook.VK_BACK);

            SendUnicodeString(rendered.Text);

            foreach (var img in snippet.Images)
            {
                var path = Path.Combine(AppPaths.ImageCacheDir, img.LocalFile);
                if (!File.Exists(path)) continue;
                if (ClipboardFacade.SetImageFile(path))
                {
                    SendPaste();
                    Thread.Sleep(80);
                }
            }
        }
        finally
        {
            Task.Run(async () =>
            {
                await Task.Delay(150);
                _expanding = false;
            });
        }
    }

    private static string? MapKey(int vk)
    {
        if (vk is >= 0x30 and <= 0x39) return ((char)vk).ToString();
        if (vk is >= 0x41 and <= 0x5A) return ((char)vk).ToString();
        if (vk == KeyboardHook.VK_SPACE) return " ";
        if (vk == KeyboardHook.VK_OEM_MINUS) return "-";
        if (vk == KeyboardHook.VK_OEM_PERIOD) return ".";
        if (vk == KeyboardHook.VK_OEM_COMMA) return ",";
        if (vk == KeyboardHook.VK_OEM_1) return ";";
        if (vk == KeyboardHook.VK_OEM_2) return "/";
        if (vk == KeyboardHook.VK_OEM_3) return "`";
        if (vk == KeyboardHook.VK_OEM_4) return "[";
        if (vk == KeyboardHook.VK_OEM_5) return "\\";
        if (vk == KeyboardHook.VK_OEM_6) return "]";
        if (vk == KeyboardHook.VK_OEM_7) return "'";
        if (vk == KeyboardHook.VK_OEM_PLUS) return "=";
        return null;
    }

    private static void SendKey(int vk)
    {
        var inputs = new PlatformInterop.INPUT[1];
        inputs[0].type = PlatformInterop.INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)vk;
        inputs[0].U.ki.wScan = 0;
        inputs[0].U.ki.dwFlags = 0;
        inputs[0].U.ki.time = 0;
        inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;
        PlatformInterop.SendInput(1, inputs, PlatformInterop.INPUT.Size);

        inputs[0].U.ki.dwFlags = PlatformInterop.KEYEVENTF_KEYUP;
        PlatformInterop.SendInput(1, inputs, PlatformInterop.INPUT.Size);
    }

    private static void SendUnicodeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (var chunk in text.Chunk(20))
        {
            var inputs = new List<PlatformInterop.INPUT>();
            foreach (var ch in chunk)
            {
                var down = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
                down.U.ki.wVk = 0; down.U.ki.wScan = ch; down.U.ki.dwFlags = PlatformInterop.KEYEVENTF_UNICODE; down.U.ki.time = 0; down.U.ki.dwExtraInfo = IntPtr.Zero;
                inputs.Add(down);
                var up = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
                up.U.ki.wVk = 0; up.U.ki.wScan = ch; up.U.ki.dwFlags = PlatformInterop.KEYEVENTF_UNICODE | PlatformInterop.KEYEVENTF_KEYUP; up.U.ki.time = 0; up.U.ki.dwExtraInfo = IntPtr.Zero;
                inputs.Add(up);
            }
            var arr = inputs.ToArray();
            PlatformInterop.SendInput((uint)arr.Length, arr, Marshal.SizeOf<PlatformInterop.INPUT>());
        }
    }

    private static void SendPaste()
    {
        const ushort VK_CONTROL = 0x11, VK_V = 0x56;
        var inputs = new PlatformInterop.INPUT[4];
        inputs[0] = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
        inputs[0].U.ki.wVk = VK_CONTROL; inputs[0].U.ki.dwFlags = 0;
        inputs[1] = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
        inputs[1].U.ki.wVk = VK_V; inputs[1].U.ki.dwFlags = 0;
        inputs[2] = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
        inputs[2].U.ki.wVk = VK_V; inputs[2].U.ki.dwFlags = PlatformInterop.KEYEVENTF_KEYUP;
        inputs[3] = new PlatformInterop.INPUT { type = PlatformInterop.INPUT_KEYBOARD };
        inputs[3].U.ki.wVk = VK_CONTROL; inputs[3].U.ki.dwFlags = PlatformInterop.KEYEVENTF_KEYUP;
        PlatformInterop.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<PlatformInterop.INPUT>());
    }

    public void Dispose() => _hook.Dispose();
}

internal static class ClipboardFacade
{
    public static string? GetText()
    {
        string? text = null;
        var t = new Thread(() =>
        {
            try { if (System.Windows.Clipboard.ContainsText()) text = System.Windows.Clipboard.GetText(); }
            catch { }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join(500);
        return text;
    }

    public static bool SetImageFile(string path)
    {
        bool ok = false;
        var t = new Thread(() =>
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                System.Windows.Clipboard.SetImage(bmp);
                ok = true;
            }
            catch { }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join(1000);
        return ok;
    }
}