using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.Cryptography;
using Neo.VM;
using Newtonsoft.Json;
using System;
using System.IO;

namespace SmartPromise.Test
{
    static class Helper
    {
        public static string OPERATION_ADD_PROMISE = "add";
        public static string OPERATION_REPLACE_PROMISE = "replace";
        public static string OPERATION_MINT_TOKENS = "mintTokens";
        public static string OPERATION_TRANSFER = "transfer";
        public static string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";

        static public string GetPromiseKey(string ownerKey, int i)
        {
            const char PROMISE_PREFIX = 'P';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = GetKey(ownerKey);
            var postfix = Convert.ToByte(i).ToString("x2");
            return prefix + main + postfix;
        }

        static public string GetKey(string ownerKey)
        {
            return UInt160.Parse(ownerKey).ToArray().ToHexString();
        }

        static public string GetPromiseCounterKey(string ownerKey)
        {
            const char PROMISE_PREFIX = 'C';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = GetKey(ownerKey);
            return prefix + main;
        }

        static public bool TransferToken(string fromSH, string toSH, int value, 
            IScriptContainer scriptContainer, InteropService service)
        {
            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(value);
                sb.EmitPush(UInt160.Parse(toSH));
                sb.EmitPush(UInt160.Parse(fromSH));
                sb.EmitPush(3);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_TRANSFER);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);
            return engine.EvaluationStack.Peek().GetBoolean();
        }

        public static bool AddPromise(Promise promise, IScriptContainer scriptContainer,
            InteropService service)
        {
            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {

                sb.EmitPush(jsonPromise);
                sb.EmitPush(1);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_ADD_PROMISE);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
        }

        static public bool ReplacePromise(Promise promise, int index,
            IScriptContainer scriptContainer, InteropService service)
        {
            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(index);
                sb.EmitPush(jsonPromise);
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_REPLACE_PROMISE);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
        }

    }
}
