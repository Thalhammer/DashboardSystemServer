using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DashboardServer
{
    class KeyBoardHook
    {
        public delegate void KeyBoardEventHandler(object sender, KeyBoardEventArgs args);
        public event KeyBoardEventHandler KeyBoardEvent;

        private Thread _messageThread;
        public KeyBoardHook()
        {
            _messageThread = new Thread(ThreadFunction);
            _messageThread.Start();
        }

        public void RemoveHook()
        {
            _messageThread.Abort();
        }

        private void ThreadFunction()
        {
            try
            {
                _proc = this.HookCallback;
                _hookID = SetHook(_proc);
                // We need a message loop in this thread.
                Application.Run();
            } finally
            {
                Native.UnhookWindowsHookEx(_hookID);
            }
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        // Prevent Garbage collector from collecting our delegate.
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Keys vkCode = (Keys)Marshal.ReadInt32(lParam);
                UInt32 scancode = (UInt32)Marshal.ReadInt32(IntPtr.Add(lParam, 4));
                UInt32 flags = (UInt32)Marshal.ReadInt32(IntPtr.Add(lParam, 8));
                UInt32 time = (UInt32)Marshal.ReadInt32(IntPtr.Add(lParam, 12));

                KeyBoardEventArgs args = new KeyBoardEventArgs();
                args.CallNext = true;
                args.SuppressAction = false;
                args.Key = vkCode;
                args.IsKeyDown = ((wParam == (IntPtr)WM_KEYDOWN) || (wParam == (IntPtr)WM_SYSKEYDOWN));
                args.Time = time;

                args.LShift = (Native.GetKeyState((int)Keys.LShiftKey) & 0x8000) != 0;
                args.RShift = (Native.GetKeyState((int)Keys.RShiftKey) & 0x8000) != 0;
                args.LControl = (Native.GetKeyState((int)Keys.LControlKey) & 0x8000) != 0;
                args.RControl = (Native.GetKeyState((int)Keys.RControlKey) & 0x8000) != 0;
                args.LMenu = (Native.GetKeyState((int)Keys.LMenu) & 0x8000) != 0;
                args.RMenu = (Native.GetKeyState((int)Keys.RMenu) & 0x8000) != 0;

                if (KeyBoardEvent != null)
                    KeyBoardEvent(this, args);

                IntPtr result = IntPtr.Zero;
                if(args.CallNext)
                {
                    result = Native.CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if(args.SuppressAction)
                {
                    result = (IntPtr)1;
                }

                return result;
            }
            else
            {
                return Native.CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return Native.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    Native.GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private static class Native
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern UInt16 GetKeyState(int vkey);
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);
        }
    }
}
