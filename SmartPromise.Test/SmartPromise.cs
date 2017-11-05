using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Neo.Cryptography;
using Neo.VM;
using Newtonsoft.Json;
using Neo;
using System.Collections;
using Neo.Core;
using System.Text;
using System.Linq;

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
            var transactionOutput = new TransactionOutput
            {
                ScriptHash = UInt160.Parse("A518E4F561F37782B39AB4F28B8D538F47B8AA6C"),
                Value = new Fixed8(10),
                AssetId = UInt256.Parse("B283C915F482DBC3A89189D865C4B42E74210BED735DCD307B1915C4E0A46C01")

            };

            initialTransaction.Outputs = new TransactionOutput[] { transactionOutput };

            /** CREATE FAKE CURRENT TRANSACTION */
            var coinRef = new CoinReference
            {
                PrevHash = initialTransaction.Hash,
                PrevIndex = 0
            };

            var currentTransaction = new CustomTransaction(TransactionType.ContractTransaction)
            {
                Inputs = new CoinReference[] { coinRef }
            };

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
            var data = service.storageContext.data;
            byte[] promiseBytes = null;

            Assert.AreEqual(data.Count, 0);

            var owner1 = "owner1";
            var owner2 = "owner2";
            var owner3 = "owner3";

            var promise1 = new Promise
            {
                Content = "Promise",
                IsDone = false
            };
            var promise2 = new Promise
            {
                Content = "Promise",
                IsDone = false
            };
            var promise3 = new Promise
            {
                Content = "Promise",
                IsDone = false
            };
            
            Assert.AreEqual(AddPromise(owner1, promise1), true);
            Assert.AreEqual(data.Count, 1);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise1));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[owner1]), true);
            
            Assert.AreEqual(AddPromise(owner2, promise2), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise2));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[owner2]), true);

            Assert.AreEqual(AddPromise(owner3, promise3), true);
            Assert.AreEqual(data.Count, 3);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise3));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[owner3]), true);
        }
    }
}
