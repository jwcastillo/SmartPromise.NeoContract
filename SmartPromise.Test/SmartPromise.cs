using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System.IO;
using Neo.Cryptography;
using System.Numerics;
using System.Text;

namespace SmartPromise.Test
{
    [TestClass]
    public class SmartPromise
    {
        const string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";

        private ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default);
        
        [TestInitialize]
        public void InitEngine()
        {
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));
        }

        private string Query(string owner)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                string operation = "query";
                sb.EmitPush(Encoding.ASCII.GetBytes(owner));
                sb.EmitPush(operation);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            var result = engine.EvaluationStack.Peek().GetByteArray();
            return Encoding.ASCII.GetString(result);

        }

        private bool Register(string domain, byte[] owner)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                string operation = "register";
                sb.EmitPush(owner);
                sb.EmitPush(Encoding.ASCII.GetBytes(domain));
                sb.EmitPush(operation);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            return engine.EvaluationStack.Peek().GetBoolean();
        }

        [TestMethod]
        public void CanSaveDomains()
        {
            string owner = "";

            Assert.AreEqual(Register("domain1", Encoding.ASCII.GetBytes("owner1")), true);
            Assert.AreEqual(Register("domain2", Encoding.ASCII.GetBytes("owner2")), true);

            owner = Query("domain1");
            owner = Query("domain2");
        }
    }
}
