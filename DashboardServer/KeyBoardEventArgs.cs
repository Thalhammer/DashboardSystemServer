using System;
using System.Windows.Forms;

namespace DashboardServer
{
    class KeyBoardEventArgs: EventArgs
    {
        /// <summary>
        /// The pressed keycode
        /// </summary>
        public Keys Key { get; set; }
        /// <summary>
        /// Time in milliseconds since system boot
        /// </summary>
        public UInt32 Time { get; set; }
        /// <summary>
        /// Set to false to prevent calling the next hook.
        /// </summary>
        public bool CallNext { get; set; }
        /// <summary>
        /// Set to false to suppress any actions.
        /// </summary>
        public bool SuppressAction { get; set; }

        public bool IsKeyDown { get; set; }
        public bool IsKeyUp { get { return !IsKeyDown; } set { IsKeyDown = !value; } }

        public bool LShift { get; set; }
        public bool RShift { get; set; }
        public bool Shift { get { return LShift || RShift; } }
        public bool LControl { get; set; }
        public bool RControl { get; set; }
        public bool Control { get { return LControl || RControl; } }
        public bool RMenu { get; set; }
        public bool LMenu { get; set; }
        public bool Menu { get { return LMenu || RMenu; } }
    }
}
