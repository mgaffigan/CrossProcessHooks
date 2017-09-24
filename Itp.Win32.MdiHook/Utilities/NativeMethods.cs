using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook
{
    internal static class NativeMethods
    {
        const string User32 = "user32.dll";
        const string Kernel32 = "kernel32.dll";
        internal const int MAX_PATH = 260;

        public const int
            WM_DESTROY = 0x0002,
            WM_CLOSE = 0x0010,
            WM_ERASEBKGND = 0x0014,
            WM_EXITSIZEMOVE = 0x0232,
            WM_SYSCHAR = 0x0106,
            WM_SYSCOMMAND = 0x0112;

        public const int
            SC_MOVE = 0xF010;

        public const int
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_TOOLWINDOW = 0x00000080;

        public const int
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = unchecked((int)0x80000000),
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
            WS_DISABLED = 0x08000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_MAXIMIZE = 0x01000000,
            WS_CAPTION = 0x00C00000,
            WS_BORDER = 0x00800000,
            WS_DLGFRAME = 0x00400000,
            WS_VSCROLL = 0x00200000,
            WS_HSCROLL = 0x00100000,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_GROUP = 0x00020000,
            WS_TABSTOP = 0x00010000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_TILED = WS_OVERLAPPED,
            WS_ICONIC = WS_MINIMIZE,
            WS_SIZEBOX = WS_THICKFRAME;

        public const int
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

        public const int
            HC_ACTION = 0;

        public const int
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CALLWNDPROCRET = 12;

        public static int SC_FROM_WPARAM(IntPtr wparam)
        {
            // In WM_SYSCOMMAND messages, the four low-order bits of the wParam parameter are used internally by
            // the system. To obtain the correct result when testing the value of wParam, an application must
            // combine the value 0xFFF0 with the wParam value by using the bitwise AND operator.
            return ((int)wparam & 0xfff0);
        }

        [DllImport(User32, SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport(User32, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public static void SetParent(this Control control, IntPtr parent)
        {
            var res = SetParent(control.Handle, parent);
            if (res == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr childAfter, string className, string windowTitle);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        public const int
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_FRAMECHANGED = 0x0020;
        public const int
            GWL_EXSTYLE = -20,
            GWL_STYLE = -16;

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport(User32, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        public delegate IntPtr GetOrSendWndProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(User32, ExactSpelling = true, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr p);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern HookSafeHandle SetWindowsHookEx(int idHook, GetOrSendWndProc wndProc, IntPtr hMod, int dwThreadId);

        [DllImport(User32, ExactSpelling = true)]
        public static extern IntPtr CallNextHookEx(HookSafeHandle hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(Kernel32)]
        public static extern int GetCurrentThreadId();

        public sealed class HookSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public HookSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return UnhookWindowsHookEx(handle);
            }
        }
    }
}
