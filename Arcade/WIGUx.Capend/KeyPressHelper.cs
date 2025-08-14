
using System;
using System.Runtime.InteropServices;

static class KeyPressHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_ESCAPE = 0x1B;

    public static void SimulateEscKeyPress()
    {
        INPUT input = new INPUT();
        input.type = INPUT_KEYBOARD;
        input.u.ki.wVk = VK_ESCAPE;
        input.u.ki.wScan = 0;
        input.u.ki.dwFlags = KEYEVENTF_KEYDOWN;
        input.u.ki.time = 0;
        input.u.ki.dwExtraInfo = IntPtr.Zero;

        // Simular presión de la tecla Esc
        SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

        // Simular liberación de la tecla Esc
        input.u.ki.dwFlags = KEYEVENTF_KEYUP;
        SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
    }
}
