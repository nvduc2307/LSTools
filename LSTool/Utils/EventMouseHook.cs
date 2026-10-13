using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LSTool.Utils
{
    public class EventMouseHook
    {
        #region Windows structure definitions

        [StructLayout(LayoutKind.Sequential)]
        private class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseLLHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }


        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        #endregion

        #region Windows function imports
        [DllImport("user32.dll", CharSet = CharSet.Auto,
           CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
             CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);

        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);

        [DllImport("user32")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion

        #region Windows constants

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;

        private const int WH_MOUSE = 7;
        private const int WH_KEYBOARD = 2;

        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private const int WM_MOUSEWHEEL = 0x020A;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_SHIFT = 0x10;
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_NUMLOCK = 0x90;

        #endregion

        private static HookProc MouseHookProcedure;
        private int hMouseHook = 0;
        public event System.Windows.Forms.MouseEventHandler OnMouseLClick;
        public event System.Windows.Forms.MouseEventHandler OnMouseRClick;
        public event System.Windows.Forms.MouseEventHandler OnMouseLDbClick;
        public event System.Windows.Forms.MouseEventHandler OnMouseRDbClick;
        public event System.Windows.Forms.MouseEventHandler OnMouseMidClick;
        public event System.Windows.Forms.MouseEventHandler OnMouseMove;
        public EventMouseHook()
        {

        }
        public void Start(MouseButtons mouseButton)
        {
            if (hMouseHook == 0)
            {
                switch (mouseButton)
                {
                    case MouseButtons.Left:
                        MouseHookProcedure = new HookProc(MouseLeftClickHookProc);
                        break;
                    case MouseButtons.Right:
                        MouseHookProcedure = new HookProc(MouseRightClickHookProc);
                        break;
                    case MouseButtons.Middle:
                        MouseHookProcedure = new HookProc(MouseMiddleClickHookProc);
                        break;
                    case MouseButtons.XButton1:
                    case MouseButtons.XButton2:
                    case MouseButtons.None:
                        MouseHookProcedure = new HookProc(MouseWheelHookProc);
                        break;
                }
                hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, MouseHookProcedure, IntPtr.Zero, 0);
                if (hMouseHook == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Stop(true, false);
                    throw new Win32Exception(errorCode);
                }
            }
        }
        public void Stop(bool UninstallMouseHook, bool ThrowExceptions)
        {
            if (hMouseHook != 0 && UninstallMouseHook)
            {
                int retMouse = UnhookWindowsHookEx(hMouseHook);
                hMouseHook = 0;
                if (retMouse == 0 && ThrowExceptions)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }
        private int MouseLeftClickHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (OnMouseLClick != null))
            {
                var mouseHookStruct = (MouseLLHookStruct)Marshal
                    .PtrToStructure(lParam, typeof(MouseLLHookStruct));
                if (wParam == WM_LBUTTONUP)
                {
                    var button = MouseButtons.Left;
                    short mouseDelta = 0;
                    int clickCount = 1;
                    if (button != MouseButtons.None)
                    {
                        var e = new System.Windows.Forms.MouseEventArgs(
                            button,
                            clickCount,
                            mouseHookStruct.pt.x,
                            mouseHookStruct.pt.y,
                            mouseDelta);
                        OnMouseLClick(this, e);
                    }
                }
            }
            return 0;
        }
        private int MouseRightClickHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (OnMouseRClick != null))
            {
                var mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));
                if (wParam == WM_RBUTTONUP)
                {
                    var button = MouseButtons.Right;
                    short mouseDelta = 0;
                    int clickCount = 1;
                    var e = new System.Windows.Forms.MouseEventArgs(
                        button,
                        clickCount,
                        mouseHookStruct.pt.x,
                        mouseHookStruct.pt.y,
                        mouseDelta);
                    OnMouseRClick(this, e);
                }
            }
            return 0;
        }
        private int MouseMiddleClickHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (OnMouseMidClick != null))
            {
                var mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));
                if (wParam == WM_MBUTTONUP)
                {
                    var button = MouseButtons.Middle;
                    short mouseDelta = 0;
                    int clickCount = 1;
                    var e = new System.Windows.Forms.MouseEventArgs(
                        button,
                        clickCount,
                        mouseHookStruct.pt.x,
                        mouseHookStruct.pt.y,
                        mouseDelta);
                    OnMouseMidClick(this, e);
                }
            }
            return 0;
        }
        private int MouseWheelHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (OnMouseMove != null))
            {
                var mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));
                var mouseDelta = 0;
                int clickCount = 1;
                var button = MouseButtons.None;
                switch (wParam)
                {
                    case WM_LBUTTONUP:
                        button = MouseButtons.Left;
                        mouseDelta = 0;
                        clickCount = 1;
                        var e = new System.Windows.Forms.MouseEventArgs(
                            button,
                            clickCount,
                            mouseHookStruct.pt.x,
                            mouseHookStruct.pt.y,
                            mouseDelta);
                        if (OnMouseLClick != null)
                            OnMouseLClick(this, e);
                        break;
                    case WM_RBUTTONUP:
                        button = MouseButtons.Right;
                        mouseDelta = 0;
                        clickCount = 1;
                        e = new System.Windows.Forms.MouseEventArgs(
                            button,
                            clickCount,
                            mouseHookStruct.pt.x,
                            mouseHookStruct.pt.y,
                            mouseDelta);
                        if (OnMouseRClick != null)
                            OnMouseRClick(this, e);
                        break;
                    case WM_MBUTTONUP:
                        break;
                    default:
                        mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
                        e = new System.Windows.Forms.MouseEventArgs(
                            button,
                            clickCount,
                            mouseHookStruct.pt.x,
                            mouseHookStruct.pt.y,
                            mouseDelta);
                        OnMouseMove(this, e);
                        break;
                }
            }
            return 0;
        }
    }
}
