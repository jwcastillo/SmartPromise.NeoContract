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
using System.Numerics;

namespace SmartPromise.Test
{
    [TestClass]
    public class SmartPromise
    {
        private const string OPERATION_ADD_PROMISE = "add";
        private const string OPERATION_REPLACE_PROMISE = "replace";
        private const string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";
        private CustomInteropService service;

        [Serializable]
        public class Promise
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public int Complicity { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime Date { get; set; }            
        }
        
        private string GetPromiseKey(string ownerKey, int i)
        {
            const string PROMISE_PREFIX = "P";
            string index = Convert((new BigInteger(i)).ToByteArray());
            return PROMISE_PREFIX + ownerKey + index;
        }

        private string GetPromiseCountKey(string ownerKey)
        {
            const string PROMISE_COUNT_PREFIX = "C";
            return PROMISE_COUNT_PREFIX + ownerKey;
        }

        private string Convert(byte[] data)
        {
            char[] characters = data.Select(b => (char)b).ToArray();
            return new string(characters);
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

        private bool ReplacePromise(string owner, Promise promise, int index)
        {
            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(index);
                sb.EmitPush(jsonPromise);
                sb.EmitPush(owner);
                sb.EmitPush(3);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_REPLACE_PROMISE);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
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
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_ADD_PROMISE);
                engine.LoadScript(sb.ToArray());
                string v = sb.ToArray().ToHexString();
            }
            
            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
        }

        [TestMethod]
        public void CanCompletePromise()
        {
            var data = service.storageContext.data;
            byte[] promiseBytes = null;

            Assert.AreEqual(data.Count, 0);

            var owner = "owner";

            var promiseNotCompleted = new Promise
            {
                Id = new Guid(),
                Title = "Title",
                Content = "Content",
                IsCompleted = false,
                Date = DateTime.Now,
                Complicity = 3
            };
            var promiseCompleted = new Promise
            {
                Id = new Guid(),
                Title = "Title",
                Content = "Content",
                IsCompleted = true,
                Date = DateTime.Now,
                Complicity = 3
            };

            Assert.AreEqual(AddPromise(owner, promiseNotCompleted), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseNotCompleted));
            string promiseKey = GetPromiseKey(owner, 0);
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);

            Assert.AreEqual(ReplacePromise(owner, promiseCompleted, 0), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseCompleted));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);
        }

        [TestMethod]
        public void ReturnsFalseWhenReplaceNotExistingPromise()
        {
            ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(10);
                sb.EmitPush("notExistingPromise");
                sb.EmitPush("notExistingOwner");
                sb.EmitPush(OPERATION_REPLACE_PROMISE);
                sb.EmitPush(3);
                sb.Emit(OpCode.PACK);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);
            var result = engine.EvaluationStack.Peek().GetBoolean();
            Assert.AreEqual(result, false);
        }
        
        [TestMethod]
        public void CanAddedMultiplePromisesToMultipleOwners()
        {
            var data = service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);
            
            for (int i = 0; i < 100; ++i)
            {
                var owner = "owner" + i;
                var promise = new Promise
                {
                    Id = new Guid(),
                    Title = "Title" + i,
                    Content = "Content" + i,
                    IsCompleted = false,
                    Date = DateTime.Now,
                    Complicity = i % 5
                };

                Assert.AreEqual(AddPromise(owner, promise), true);
                /**one record for promise data one record for promise count*/
                Assert.AreEqual(data.Count, (i + 1) * 2);
                string promiseId = GetPromiseKey(owner, 0);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[]) data[promiseId]), true);
            }
        }

        [TestMethod]
        public void CanCountPromisesProperly()
        {
            var data = service.storageContext.data;
            Assert.AreEqual(data.Count, 0);
            var owner = "owner";
            var promise = new Promise
            {
                Id = new Guid(),
                Title = "Title",
                Content = "Content",
                IsCompleted = false,
                Date = DateTime.Now,
                Complicity = 0
            };

            int curCount = data.Count;
            for (int i = 0; i < 1; ++i)
            {
                Assert.AreEqual(AddPromise(owner, promise), true);
                Assert.AreEqual(data.Count, curCount + i + 2);
            }
            curCount = data.Count;
            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(AddPromise(owner + i, promise), true);
                Assert.AreEqual(data.Count, curCount + (i + 1)*2);
            }
        }

        [TestMethod]
        public void CanAddMultiplePromisesToOwner()
        {
            var data = service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);
            var owner = "owner";
           
            for (int i = 0; i < 100; ++i)
            {
                var promise = new Promise
                {
                    Id = new Guid(),
                    Title = "Title" + i,
                    Content = "Content" + i,
                    IsCompleted = false,
                    Date = DateTime.Now,
                    Complicity = i % 5
                };

                Assert.AreEqual(AddPromise(owner, promise), true);
                /** +1 to store promises count*/
                Assert.AreEqual(data.Count, i + 2);
                string promiseId = GetPromiseKey(owner, i);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseId]), true);
            }
        }

        [TestMethod]
        public void ReturnsFalseWhenInvalidOperation()
        {
            ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush("arg1");
                sb.EmitPush("arg2");
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush("invalidoperation");
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);
            var result = engine.EvaluationStack.Peek().GetBoolean();
            Assert.AreEqual(result, false);
        }

        
    }
}
