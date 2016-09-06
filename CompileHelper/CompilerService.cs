using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileHelper
{
    public class CompilerService : MarshalByRefObject
    {
        public void PrintDomain()
        {
            Console.WriteLine("Object is executing in AppDomain \"{0}\"",
                AppDomain.CurrentDomain.FriendlyName);
        }

        public string Compile(string code, string[] assemblies, string type, string method)
        {
            try
            {
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerParameters param = new CompilerParameters();
                param.ReferencedAssemblies.AddRange(assemblies);
                param.GenerateInMemory = true;
                param.GenerateExecutable = true;
                CompilerResults results = provider.CompileAssemblyFromSource(param, code);
                List<object> errors = new List<object>();
                foreach (CompilerError e in results.Errors)
                {
                    errors.Add(new
                    {
                        number = e.ErrorNumber,
                        line = e.Line,
                        column = e.Column,
                        text = e.ErrorText
                    });
                }
                if (!results.Errors.HasErrors)
                {
                    Assembly prog_assembly = results.CompiledAssembly;
                    Type program = prog_assembly.GetType(type);
                    MethodInfo m = program.GetMethod(method);
                    m.Invoke(null, null);
                }

                return "OK";
            }
            catch (Exception e)
            {
                return "Exception:" + e.Message;
            }
        }
    }
}
