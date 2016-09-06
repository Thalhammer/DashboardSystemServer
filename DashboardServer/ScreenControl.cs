using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DashboardServer
{
    class ScreenControl
    {
        /// <summary>
        /// Get current screen brightness in percent
        /// </summary>
        /// <returns>brightness</returns>
        public static byte GetBrightness()
        {
            try
            {
                // define scope (namespace)
                System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

                // define query
                System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

                // output current brightness
                System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

                System.Management.ManagementObjectCollection moc = mos.Get();

                // store result
                byte curBrightness = 0;
                foreach (System.Management.ManagementObject o in moc)
                {
                    curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
                    break; // only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

                return curBrightness;
            }catch(Exception)
            {
                return 255;
            }
        }

        /// <summary>
        /// Get a array of valid brightness values for this system.
        /// </summary>
        /// <returns>array of valid values</returns>
        public static byte[] GetBrightnessLevels()
        {
            try
            {
                // define scope (namespace)
                System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");
                // define query
                System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");
                // output current brightness
                System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);
                byte[] BrightnessLevels = new byte[0];
                System.Management.ManagementObjectCollection moc = mos.Get();

                foreach (System.Management.ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break; // only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

                return BrightnessLevels;
            }catch(Exception)
            {
                return new byte[] { };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetBrightness"></param>
        public static void SetBrightness(byte targetBrightness)
        {
            try
            {
                //define scope (namespace)
                System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

                //define query
                System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightnessMethods");

                //output current brightness
                System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

                System.Management.ManagementObjectCollection moc = mos.Get();

                foreach (System.Management.ManagementObject o in moc)
                {
                    o.InvokeMethod("WmiSetBrightness", new Object[] { UInt32.MaxValue, targetBrightness }); //note the reversed order - won't work otherwise!
                    break; //only work on the first object
                }

                moc.Dispose();
                mos.Dispose();
            }catch(Exception)
            {

            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);


        public static void SetState(MonitorState state)
        {
            if (state != MonitorState.On)
            {
                SendMessage(0xffff, WM_SYSCOMMAND, SC_MONITORPOWER, (int)state);
            } else
            {
                mouse_event(MouseEventMove, 0, 1, 0, UIntPtr.Zero);
            }
        }

        private const int SC_MONITORPOWER = 0xF170;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int MouseEventMove = 0x0001;

        public enum MonitorState
        {
            On = -1,
            Off = 2,
            LowPower = 1
        }
    }
}
