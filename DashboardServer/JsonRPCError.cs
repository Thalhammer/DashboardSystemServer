using System;

namespace DashboardServer
{
    class JsonRPCError: Exception
    {
        public int Code { get; set; }
        
        public JsonRPCError(int code, string message): base(message)
        {
            Code = code;
        }
    }
}
