using System.Runtime.InteropServices;
namespace CodexUsageTray;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly Action _toggle;
    private readonly bool _registered;
    internal HotkeyWindow(Action toggle)
    {
        _toggle = toggle;
        CreateHandle(new CreateParams());
        _registered = RegisterHotKey(Handle, 1, 0x0001 | 0x0002, (uint)Keys.C);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == 1) _toggle();
        base.WndProc(ref m);
    }
    public void Dispose()
    {
        if (_registered) UnregisterHotKey(Handle, 1);
        DestroyHandle();
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr handle, int id);
}