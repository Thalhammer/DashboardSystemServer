using System;
using System.Windows.Forms;

namespace DashboardServer
{
    class KeyBoardEventArgs: EventArgs
    {
        public Keys Key { get; set; }
        public bool CallNext { get; set; }
        public bool SuppressAction { get; set; }

        public bool IsKeyDown { get; set; }
        public bool IsKeyUp { get { return !IsKeyDown; } set { IsKeyDown = !value; } }
    }
}
