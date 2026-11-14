using System.Runtime.InteropServices;

namespace TextBlaze.Services;

// Simple keyboard hook that stays alive. Uses a dedicated input queue.
public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private IntPtr _hook = IntPtr.Zero;
    private IntPtr _procDelegate;

    public event Action<int, bool>? KeySignal;

    // Key constants
    public const int VK_SPACE = 0x20;
    public const int VK_OEM_PLUS = 0xBB;
    public const int VK_OEM_COMMA = 0xBC;
    public const int VK_OEM_MINUS = 0xBD;
    public const int VK_OEM_PERIOD = 0xBE;
    public const int VK_OEM_1 = 0xBA;
    public const int VK_OEM_2 = 0xBF;
    public const int VK_OEM_3 = 0xC0;
    public const int VK_OEM_4 = 0xDB;
    public const int VK_OEM_5 = 0xDC;
    public const int VK_OEM_6 = 0xDD;
    public const int VK_OEM_7 = 0xDE;
    public const int VK_BACK = 0x08;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    public KeyboardHook()
    {
        var proc = new HookProcDelegate(HookCallback);
        _procDelegate = Marshal.GetFunctionPointerForDelegate(proc);
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _procDelegate, GetModuleHandle(""), 0);
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var msg = (int)wParam;
            if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                var vk = Marshal.ReadInt32(lParam);
                KeySignal?.Invoke(vk, true);
            }
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr HookProcDelegate(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}