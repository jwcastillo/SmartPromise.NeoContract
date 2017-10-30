using Neo.Cryptography;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPromise.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var engine = new ExecutionEngine(null, Crypto.Default);

                engine.LoadScript(File.ReadAllBytes(@"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm"));

                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitPush(2); // corresponds to the parameter c
                    sb.EmitPush(4); // corresponds to the parameter b
                    sb.EmitPush(3); // corresponds to the parameter a
                    engine.LoadScript(sb.ToArray());
                }

                engine.Execute(); // start execution
                var result = engine.EvaluationStack.Peek().GetBigInteger(); // set the return value here
                Console.WriteLine($"Execution result {result}");
                Console.ReadLine();

            } catch (Exception err)
            {
                Console.WriteLine($"Error {err}");
                Console.ReadLine();
            }

            
        }
    }
}
