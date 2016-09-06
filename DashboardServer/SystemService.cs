using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace DashboardServer
{
    class SystemService: WebSocketBehavior
    {
        public static void FireKeyBoardEvent(KeyBoardEventArgs args)
        {
            KeyBoardEvent?.Invoke(null, args);
        }

        private static event KeyBoardHook.KeyBoardEventHandler KeyBoardEvent;
        private List<Keys> SuppressList = new List<Keys>();
        private List<Keys> NotifyList = new List<Keys>();
        private JsonRPC _rpc;
        private Configuration _config;

        public SystemService(Configuration config)
        {
            _config = config;
            _rpc = new JsonRPC();
            _rpc.SendCallback = (str) =>
            {
                this.Send(str);
            };
            addRPCMethod("Keyboard.setSuppressedKeys", (parameters) =>
            {
                if (parameters.all == true)
                {
                    SuppressList = null;
                }
                else
                {
                    SuppressList = new List<Keys>();
                    foreach (string key in parameters.keys)
                    {
                        Keys keys = (Keys)Enum.Parse(typeof(Keys), key);
                        if (config.KeyboardSuppressAllowedKeys.Contains(keys))
                        {
                            SuppressList.Add(keys);
                        }
                    }
                }
                return new
                {
                    keys = SuppressList == null ? null : SuppressList.Select((n) => n.ToString())
                };
            });
            addRPCMethod("Keyboard.getSuppressedKeys", (parameters) =>
            {
                return SuppressList == null ? null : SuppressList.Select((n) => Enum.GetName(typeof(Keys), n)).ToArray();
            });
            addRPCMethod("Keyboard.setNotificationKeys", (parameters) =>
            {
                if (parameters.all == true)
                {
                    NotifyList = null;
                }
                else
                {
                    NotifyList = new List<Keys>();
                    foreach (string key in parameters.keys)
                    {
                        Keys keys = (Keys)Enum.Parse(typeof(Keys), key);
                        if (config.KeyboardNotifyAllowedKeys.Contains(keys))
                        {
                            NotifyList.Add(keys);
                        }
                    }
                }
                return new
                {
                    keys = NotifyList == null ? null : NotifyList.Select((n) => n.ToString())
                };
            });
            addRPCMethod("Keyboard.getNotificationKeys", (parameters) =>
            {
                return NotifyList == null ? null : NotifyList.Select((n) => Enum.GetName(typeof(Keys), n)).ToArray();
            });
            addRPCMethod("Screen.setBrightness", (parameters) =>
            {
                byte brightness = parameters.brightness;
                var valid = ScreenControl.GetBrightnessLevels();
                int bestlevel = 0;
                for (int i = 0; i < valid.Length; i++)
                {
                    if(Math.Abs(brightness - valid[bestlevel]) > Math.Abs(brightness - valid[i]))
                    {
                        bestlevel = i;
                    }
                }
                if (valid.Length != 0)
                {
                    ScreenControl.SetBrightness(valid[bestlevel]);
                }
                return new
                {
                    brightness = ScreenControl.GetBrightness()
                };
            });
            addRPCMethod("Screen.getBrightness", (parameters) =>
            {
                return new
                {
                    brightness = ScreenControl.GetBrightness()
                };
            });
            addRPCMethod("Screen.turnOn", (parameters) =>
            {
                ScreenControl.SetState(ScreenControl.MonitorState.On);
                return new { };
            });
            addRPCMethod("Screen.turnOff", (parameters) =>
            {
                ScreenControl.SetState(ScreenControl.MonitorState.Off);
                return new { };
            });
            addRPCMethod("Screen.turnOffLowPower", (parameters) =>
            {
                ScreenControl.SetState(ScreenControl.MonitorState.LowPower);
                return new { };
            });
            addRPCMethod("Server.executeCode", (parameters) =>
            {
                string code = parameters.code;
                string[] assemblies = new string[0];
                string type = parameters.type;
                string method = parameters.method;

                AppDomain domain = AppDomain.CreateDomain("CompileDomain");
                CompileHelper.CompilerService cs = (CompileHelper.CompilerService)domain.CreateInstanceFromAndUnwrap("CompileHelper.dll", "CompileHelper.CompilerService");
                cs.Compile(code, assemblies, type, method);
                AppDomain.Unload(domain);

                return new { };
            });
            addRPCMethod("Server.getSupportedMethods", (parameter) =>
            {
                return _rpc.GetMethods();
            });
        }

        private void addRPCMethod(string method, Func<dynamic, object> fn)
        {
            if(_config.AllowedCommands.Contains(method))
            {
                _rpc.AddMethod(method, fn);
            }
        }

        protected override void OnOpen()
        {
            KeyBoardEvent += this.SystemService_KeyBoardEvent;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            KeyBoardEvent -= this.SystemService_KeyBoardEvent;
        }

        protected override void OnError(ErrorEventArgs e)
        {
            KeyBoardEvent -= this.SystemService_KeyBoardEvent;
        }

        private void SystemService_KeyBoardEvent(object sender, KeyBoardEventArgs args)
        {
            if(SuppressList == null || SuppressList.Contains(args.Key))
            {
                args.SuppressAction = true;
            }
            if (NotifyList == null || NotifyList.Contains(args.Key))
            {
                object options = new
                {
                    key = args.Key.ToString(),
                    shift = args.Shift,
                    menu = args.Menu,
                    control = args.Control
                };
                if (args.IsKeyDown)
                {
                    _rpc.SendNotification("key_down", options);
                } else
                {
                    _rpc.SendNotification("key_up", options);
                }
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (!e.IsText) return;
            _rpc.onReceive(e.Data);
        }
    }
}
