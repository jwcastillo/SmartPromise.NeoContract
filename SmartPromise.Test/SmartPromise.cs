using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Neo.Cryptography;
using Neo.VM;
using Newtonsoft.Json;
using Neo;
using System.Collections;
using Neo.Core;

namespace SmartPromise.Test
{
    [TestClass]
    public class SmartPromise
    {
        private const string OPERATION_ADD_PROMISE = "add";
        private const string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";
        private CustomInteropService service;

        [Serializable]
        public class Promise
        {
            
            public string Content { get; set; }
            public bool IsDone { get; set; }
        }

        [TestInitialize]
        public void InitInteropService()
        {
            /** CREATE FAKE PREVIOUS TRANSACTION */
            var initialTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            var transactionOutput = new Neo.Core.TransactionOutput
            {
                ScriptHash = UInt160.Parse("A518E4F561F37782B39AB4F28B8D538F47B8AA6C"),
                Value = new Neo.Fixed8(10),
                AssetId = UInt256.Parse("B283C915F482DBC3A89189D865C4B42E74210BED735DCD307B1915C4E0A46C01")

            };

            initialTransaction.Outputs = new Neo.Core.TransactionOutput[] { transactionOutput };

            /** CREATE FAKE CURRENT TRANSACTION */
            var coinRef = new CoinReference();
            coinRef.PrevHash = initialTransaction.Hash;
            coinRef.PrevIndex = 0;
            var currentTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            currentTransaction.Inputs = new CoinReference[] { coinRef };
            var hash = currentTransaction.Hash;
            
            service = new CustomInteropService();
            service.storageContext.data = new Hashtable();

            service.transactions.Add(initialTransaction.Hash.ToArray(), initialTransaction);
            service.transactions.Add(currentTransaction.Hash.ToArray(), currentTransaction);
        }
        
        private bool AddPromise(string owner, Promise promise)  
        {

            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(jsonPromise);
                sb.EmitPush(owner);
                sb.EmitPush(OPERATION_ADD_PROMISE);
                engine.LoadScript(sb.ToArray());
            }
            
            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
        }
        
        [TestMethod]
        public void CanAddPromise()
        {
            var owner = "AnkarenkoSergey";
            var promise = new Promise
            {
                Content = "Promise",
                IsDone = false
            };
            
            Assert.AreEqual(AddPromise(owner, promise), true);
            var data = service.storageContext.data;
            Assert.AreEqual(AddPromise("a", promise), true);
            Assert.AreEqual(AddPromise("b", promise), true);
            var a = service.storageContext.data;
        }
    }
}
