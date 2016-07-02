using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
            if (_methods.ContainsKey(method))
            {
                try
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
                catch (Exception)
                {
                    if (id != null)
                    {
                        string str = JObject.FromObject(new
                        {
                            jsonrpc = "2.0",
                            error = new
                            {
                                code = -32603,
                                message = "Internal error"
                            },
                            id = id
                        }).ToString();
                        SendCallback(str);
                    }
                }
            }
            else
            {
                if (id != null)
                {
                    string str = JObject.FromObject(new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32601,
                            message = "Method not found"
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
            if(response.TryGetValue("result", out dummy))
            {
                cb(response.result);
            } else
            {
                JsonRPCError error = new JsonRPCError(response.error.code, response.error.message);
                cb(error);
            }
        }

        public void onReceive(string message)
        {
            var obj = JObject.Parse(message);

            JToken dummy;
            if(obj.TryGetValue("method", out dummy))
            {
                this.handleRequest(obj);
            } else
            {
                this.handleResponse(obj);
            }
        }
    }
}
