using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DashboardServer
{
    class JsonRPC
    {
        private Dictionary<string, Func<dynamic, object>> _methods;
        public Action<string> SendCallback { get; set; }

        private Dictionary<UInt64, Action<dynamic>> _callbacks = new Dictionary<ulong, Action<dynamic>>();
        private UInt64 _next_id = 0;

        public JsonRPC()
        {
            _methods = new Dictionary<string, Func<dynamic, object>>();
        }

        public void AddMethod(string method, Func<dynamic, object> fn)
        {
            _methods.Add(method, fn);
        }

        public void RemoveMethod(string method)
        {
            _methods.Remove(method);
        }

        public List<string> GetMethods()
        {
            return _methods.Select((n) => n.Key).ToList();
        }

        public void SendCallMethod(string method, dynamic parameters, Action<dynamic> cb)
        {
            UInt64 id = _next_id;
            _next_id++;
            string str = JObject.FromObject(new
            {
                jsonrpc = "2.0",
                @params = parameters,
                method = method,
                id = id
            }).ToString();
            _callbacks.Add(id, cb);
            SendCallback(str);
        }

        public void SendNotification(string method, dynamic parameters)
        {
            string str = JObject.FromObject(new
            {
                jsonrpc = "2.0",
                @params = parameters,
                method = method
            }).ToString();
            SendCallback(str);
        }

        private void handleRequest(dynamic request)
        {
            string method = request.method;
            dynamic parameters = request["params"];
            dynamic id = request.id;
            try
            {
                try
                {
                    if (_methods.ContainsKey(method))
                    {

                        object result = _methods[method](parameters);
                        if (id != null)
                        {
                            string str = JObject.FromObject(new
                            {
                                jsonrpc = "2.0",
                                result = result,
                                id = id
                            }).ToString();
                            SendCallback(str);
                        }

                    }
                    else throw new JsonRPCError(-32601, "Method not found");
                }
                catch (JsonRPCError e) { throw e; }
                catch (Exception) { throw new JsonRPCError(-32603, "Internal error"); }
            }
            catch (JsonRPCError e)
            {
                if (id != null)
                {
                    string str = JObject.FromObject(new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = e.Code,
                            message = e.Message
                        },
                        id = id
                    }).ToString();
                    SendCallback(str);
                }
            }
        }

        private void handleResponse(dynamic response)
        {
            dynamic id = response.id;
            JToken dummy;
            Action<dynamic> cb = _callbacks[(UInt64)id];
            _callbacks.Remove((UInt64)id);
            if (response.TryGetValue("result", out dummy))
            {
                cb(response.result);
            }
            else
            {
                JsonRPCError error = new JsonRPCError(response.error.code, response.error.message);
                cb(error);
            }
        }

        public void onReceive(string message)
        {
            var obj = JObject.Parse(message);

            JToken dummy;
            if (obj.TryGetValue("method", out dummy))
            {
                this.handleRequest(obj);
            }
            else
            {
                this.handleResponse(obj);
            }
        }
    }
}
