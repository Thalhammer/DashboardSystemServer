using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashboardServer
{
    class Configuration
    {
        public UInt16 Port { get; private set; }
        public IPAddress Address { get; private set; }

        private List<string> _allowedCommands;
        public List<string> AllowedCommands
        {
            get
            {
                return new List<string>(_allowedCommands);
            }
            private set
            {
                _allowedCommands = value;
            }
        }
        private List<Keys> _keyboardNotifyAllowedKeys;
        public List<Keys> KeyboardNotifyAllowedKeys
        {
            get
            {
                return new List<Keys>(_keyboardNotifyAllowedKeys);
            }
            private set
            {
                _keyboardNotifyAllowedKeys = value;
            }
        }
        private List<Keys> _keyboardSuppressAllowedKeys;
        public List<Keys> KeyboardSuppressAllowedKeys
        {
            get
            {
                return new List<Keys>(_keyboardSuppressAllowedKeys);
            }
            private set
            {
                _keyboardSuppressAllowedKeys = value;
            }
        }


        public Configuration(string filename = "config.json")
        {
            dynamic config = JObject.Parse(File.ReadAllText(filename));
            _allowedCommands = new List<string>();
            foreach(string str in config.allowed_commands)
            {
                _allowedCommands.Add(str);
            }
            _keyboardNotifyAllowedKeys = new List<Keys>();
            foreach (string str in config.keyboard.notify_allowed_keys)
            {
                _keyboardNotifyAllowedKeys.Add((Keys)Enum.Parse(typeof(Keys), str));
            }
            _keyboardSuppressAllowedKeys = new List<Keys>();
            foreach (string str in config.keyboard.suppress_allowed_keys)
            {
                _keyboardSuppressAllowedKeys.Add((Keys)Enum.Parse(typeof(Keys), str));
            }
            Port = config.port;
            Address = IPAddress.Parse((string)config.address);
        }

    }
}
