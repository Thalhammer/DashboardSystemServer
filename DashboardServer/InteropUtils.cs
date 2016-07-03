using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DashboardServer
{
    class InteropUtils
    {
        public static bool ShowConsole
        {
            set
            {
                var handle = Native.GetConsoleWindow();
                Native.ShowWindow(handle, value ? Native.SW_SHOW : Native.SW_HIDE);
            }
        }
        private class Native
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetConsoleWindow();

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            public const int SW_HIDE = 0;
            public const int SW_SHOW = 5;
        }
    }
}
