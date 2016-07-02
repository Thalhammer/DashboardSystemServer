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
                SystemService.FireKeyBoardEvent(args);
            };
        }
        private void CleanupHook()
        {
            _hook.RemoveHook();
            _hook = null;
        }

        private void SetupWebSocketServer(Configuration config)
        {
            _wssv = new WebSocketServer(config.Address, config.Port);
            _wssv.AddWebSocketService<SystemService>("/system", () => { return new SystemService(config); });
            _wssv.Start();
        }
        private void CleanupWebSocketServer()
        {
            _wssv.Stop();
            _wssv = null;
        }

        private void Run()
        {
            Configuration config = new Configuration();
            Console.WriteLine("Setting up keyboard hooks");
            SetupHook();
            Console.WriteLine("Starting WebSocketServer");
            SetupWebSocketServer(config);
            Console.WriteLine("Init done");
            DoConsole();
            Console.WriteLine("Shutdown Websocket");
            CleanupWebSocketServer();
            Console.WriteLine("Removing keyboard hooks");
            CleanupHook();
        }

        private void DoConsole()
        {
            Console.WriteLine("Type \"quit\" to exit");

            bool dumpKeys = false;
            _hook.KeyBoardEvent += (obj, args) =>
            {
                if (dumpKeys)
                {
                    Console.WriteLine((args.IsKeyDown ? "[D]" : "[U]") + args.Key.ToString());
                }
            };
            string cmd = Console.ReadLine();
            while (cmd != "quit")
            {
                if (cmd.StartsWith("enable"))
                {
                    string options = cmd.Substring(cmd.IndexOf(' ') + 1);
                    if (options == "dump_keys")
                    {
                        dumpKeys = true;
                    }
                    else
                    {
                        Console.WriteLine("Unknown option \"" + options + "\"");
                    }
                }
                else if (cmd.StartsWith("disable"))
                {
                    string options = cmd.Substring(cmd.IndexOf(' ') + 1);
                    if (options == "dump_keys")
                    {
                        dumpKeys = false;
                    }
                    else
                    {
                        Console.WriteLine("Unknown option \"" + options + "\" !");
                    }
                }
                else
                {
                    Console.WriteLine("Unknown command !");
                }
                cmd = Console.ReadLine();
            }
        }

        public static void Main()
        {
            new Program().Run();
        }
    }
}
