using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace DashboardServer
{
    class SystemService: WebSocketBehavior
    {
        public static void FireKeyBoardEvent(KeyBoardEventArgs args)
        {
            KeyBoardEvent?.Invoke(null, args);
        }
        private static event KeyBoardHook.KeyBoardEventHandler KeyBoardEvent;
        private List<Keys> SupressList = new List<Keys>();
        JsonRPC _rpc;

        public SystemService()
        {
            _rpc = new JsonRPC();
            _rpc.SendCallback = (str) =>
            {
                this.Send(str);
            };
            _rpc.AddMethod("set_key_suppressed", (parameters) =>
            {
                bool suppressed = parameters.suppressed;
                string key = parameters.key;

                Keys keys = (Keys)Enum.Parse(typeof(Keys), key);
                if (suppressed && !SupressList.Contains(keys))
                {
                    SupressList.Add(keys);
                }
                else if (!suppressed && SupressList.Contains(keys))
                {
                    SupressList.Remove(keys);
                }
                return true;
            });
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
        }

        private void SystemService_KeyBoardEvent(object sender, KeyBoardEventArgs args)
        {
            if(!args.SuppressAction && SupressList.Contains(args.Key))
            {
                args.SuppressAction = true;
            }
            if(args.IsKeyDown)
            {
                _rpc.SendNotification("key_down", new { key = args.Key.ToString() });
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            _rpc.onReceive(e.Data);
        }
    }
}
