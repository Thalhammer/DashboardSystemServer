using System;
using System.Diagnostics;
using WebSocketSharp.Server;

namespace DashboardServer
{
    class Program
    {
        private KeyBoardHook _hook;
        private WebSocketServer _wssv;

        private void SetupHook()
        {
            _hook = new KeyBoardHook();
            _hook.KeyBoardEvent += (obj, args) =>
            {
                Debug.WriteLine((args.IsKeyDown ? "[DOWN]" : "[UP]  ") + args.Key.ToString());
                SystemService.FireKeyBoardEvent(args);
            };
        }
        private void CleanupHook()
        {
            _hook.RemoveHook();
            _hook = null;
        }

        private void SetupWebSocketServer()
        {
            _wssv = new WebSocketServer(666);
            _wssv.AddWebSocketService<SystemService>("/system");
            _wssv.Start();
        }
        private void CleanupWebSocketServer()
        {
            _wssv.Stop();
            _wssv = null;
        }

        private void Run()
        {
            SetupHook();
            SetupWebSocketServer();
            Console.ReadLine();
            CleanupWebSocketServer();
            CleanupHook();
        }

        public static void Main()
        {
            new Program().Run();
        }
    }
}
