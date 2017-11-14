/**
 * 
 * THANKS TO USER tekn (https://neosmarteconomy.slack.com) 
 * HE GAVE ME AN IDEA OF HOW TO TEST SMART CONTRACTS
 * https://github.com/aphtoken/NeoContractTester/tree/e8efbeccb836ea2e8252585da7bdcd56dfad042a
 * 
 */
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
        private const string OPERATION_REPLACE_PROMISE = "replace";
        private const string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";
        private string[] HASHES = new string[] {
            "0x22a4d553282d7eaf53538eb8ccb27e842d0d90b6",
            "0xbc89c04256bd0a5b9d53a0d239d615a8734bc459",
            "0x1e66cccfed7a0a9f4bc9bf6c92b286acef65fc77",
            "0xa42abb913fa551de74fd4626ad4a789a2987e52e"
        };
        private CustomInteropService service;
        private IScriptContainer scriptContainer;

        private string GetPromiseKey(string ownerKey, int i)
        {
            const char PROMISE_PREFIX = 'P';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = UInt160.Parse(ownerKey).ToArray().ToHexString();
            var postfix = Convert.ToByte(i).ToString("x2");
            return prefix + main + postfix;
        }

        private string GetPromiseCounterKey(string ownerKey)
        {
            const char PROMISE_PREFIX = 'C';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = UInt160.Parse(ownerKey).ToArray().ToHexString();
            return prefix + main;
        }

        private Transaction CreateTransaction(string scriptHash)
        {
            /** CREATE FAKE PREVIOUS TRANSACTION */
            var initialTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            var transactionOutput = new TransactionOutput
            {
                ScriptHash = UInt160.Parse(scriptHash),
                Value = new Fixed8(10),
                AssetId = UInt256.Parse("B283C915F482DBC3A89189D865C4B42E74210BED735DCD307B1915C4E0A46C01")
            };

            var reference = new TransactionOutput
            {
                ScriptHash = UInt160.Parse(scriptHash),
                Value = new Fixed8(3),
                AssetId = UInt256.Parse("B283C915F482DBC3A89189D865C4B42E74210BED735DCD307B1915C4E0A46C01")
            };
            
            initialTransaction.Outputs = new TransactionOutput[] { transactionOutput };
            /**FOR TESTING PURPOSE*/
            initialTransaction.CustomReferences = new TransactionOutput[] { reference };

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

            return initialTransaction;
        }
        
        private bool ReplacePromise(Promise promise, int index)
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

        private bool AddPromise(Promise promise)
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

        [TestInitialize]
        public void InitInteropService()
        {
            service = new CustomInteropService();
            service.storageContext.data = new Hashtable();
        }

        [TestMethod]
        public void CanCompletePromise()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            var data = service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);
            
            var promiseNotCompleted = new Promise
            {
                Id = 1,
                Title = "Title",
                Content = "Content",
                Status = PROMISE_STATUS.NOT_COMPLTED,
                Date = DateTime.Now,
                Complicity = 3,
                Proof = ""
            };
            var promiseCompleted = new Promise
            {
                Id = 2,
                Title = "Title",
                Content = "Content",
                Status = PROMISE_STATUS.COMPLTED,
                Date = DateTime.Now,
                Complicity = 3,
                Proof = "COMPLETED"
            };

            Assert.AreEqual(AddPromise(promiseNotCompleted), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseNotCompleted));
            string promiseKey = GetPromiseKey(HASHES[0], 1);
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);

            Assert.AreEqual(ReplacePromise(promiseCompleted, 1), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseCompleted));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);
        }

        [TestMethod]
        public void PromisesSizeCountesProperly()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            var data = service.storageContext.data;
            Assert.AreEqual(data.Count, 0);
            string promiseCounterKey;
            byte[] ba;
            int counter;

            var promise = new Promise
            {
                Id = 1,
                Title = "Title",
                Content = "Content",
                Status = PROMISE_STATUS.NOT_COMPLTED,
                Date = DateTime.Now,
                Complicity = Promise.MAX_COMPLICITY,
                Proof = ""
            };

            for (int i = 1; i <= 100; ++i)
            {
                Assert.AreEqual(AddPromise(promise), true);
                /** EVERY NEW PROMISE CREATES NEW RECORD IN STORAGE PLUS ONE RECORD TO STORE PROMISES COUNT*/
                Assert.AreEqual(data.Count, i + 1);
                
                promiseCounterKey = GetPromiseCounterKey(HASHES[0]);
                ba = (byte[])data[promiseCounterKey];
                counter = (int)ba[0];
                Assert.AreEqual(counter, i);
            }

            /**ANOTHER USER MAKES PROMISE*/
            scriptContainer = CreateTransaction(HASHES[1]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 101 + 2);
            promiseCounterKey = GetPromiseCounterKey(HASHES[1]);
            ba = (byte[])data[promiseCounterKey];
            counter = (int)ba[0];
            Assert.AreEqual(counter, 1);

            /**PREVIOUS USER MAKES PROMISE*/
            scriptContainer = CreateTransaction(HASHES[0]);
            promiseCounterKey = GetPromiseCounterKey(HASHES[0]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 103 + 1);
            ba = (byte[])data[promiseCounterKey];
            counter = (int)ba[0];
            Assert.AreEqual(counter, 101);
        }

        [TestMethod]
        public void ReturnsFalseWhenReplaceNotExistingPromise()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(10);
                sb.EmitPush("notExistingPromise");
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_REPLACE_PROMISE);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);
            var result = engine.EvaluationStack.Peek().GetBoolean();
            Assert.AreEqual(result, false);
        }

        [TestMethod]
        public void CanAddMultiplePromisesToMultipleOwners()
        {
            
            var data = service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);

            for (int i = 0; i < HASHES.Length; ++i)
            {
                /**CHANGE USER SCRIPT HASH*/
                scriptContainer = CreateTransaction(HASHES[i]);
                var promise = new Promise
                {
                    Id = i,
                    Title = "Title" + i,
                    Content = "Content" + i,
                    Status = PROMISE_STATUS.NOT_COMPLTED,
                    Date = DateTime.Now,
                    Complicity = i % Promise.MAX_COMPLICITY,
                    Proof = ""
                };

                Assert.AreEqual(AddPromise(promise), true);
                /**ONE RECORD FOR PROMISE DATA AND ONE RECORD FOR PROMISES COUNTER*/
                Assert.AreEqual(data.Count, (i + 1) * 2);
                /**PROMISES ID NUMERATION STARTS WITH ONE*/
                string promiseId = GetPromiseKey(HASHES[i], 1);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseId]), true);
            }
        }
        
        [TestMethod]
        public void CanCountPromisesProperly()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            var data = service.storageContext.data;
            Assert.AreEqual(data.Count, 0);
            
            var promise = new Promise
            {
                Id = 0,
                Title = "Title",
                Content = "Content",
                Status = PROMISE_STATUS.NOT_COMPLTED,
                Date = DateTime.Now,
                Complicity = 0,
                Proof = ""
            };

            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 2);

            /** ANOTHER USER INVOKED CONTRACT*/
            scriptContainer = CreateTransaction(HASHES[1]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 2 + 2);

            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 4 + 1);

            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 5 + 1);

            /** ONE MORE USER INVOKED CONTRACT*/
            scriptContainer = CreateTransaction(HASHES[2]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 6 + 2);

            /** PREVIOUS USER INVOKED CONTRACT*/
            scriptContainer = CreateTransaction(HASHES[0]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 8 + 1);

            /** ONE MORE USER INVOKED CONTRACT*/
            scriptContainer = CreateTransaction(HASHES[3]);
            Assert.AreEqual(AddPromise(promise), true);
            Assert.AreEqual(data.Count, 9 + 2);
        }
        
        [TestMethod]
        public void CanAddMultiplePromisesToOwner()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            var data = service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);

            for (int i = 1; i <= 100; ++i)
            {
                var promise = new Promise
                {
                    Id = i,
                    Title = "Title" + i,
                    Content = "Content" + i,
                    Status = PROMISE_STATUS.NOT_COMPLTED,
                    Date = DateTime.Now,
                    Complicity = i % Promise.MAX_COMPLICITY,
                    Proof = ""
                };

                Assert.AreEqual(AddPromise(promise), true);
                /** EVERY NEW PROMISE CREATES NEW RECORD IN STORAGE PLUS ONE RECORD TO STORE PROMISES COUNT*/
                Assert.AreEqual(data.Count, i + 1);
                string promiseId = GetPromiseKey(HASHES[0], i);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseId]), true);
            }
        }

        [TestMethod]
        public void ReturnsFalseWhenInvalidOperation()
        {
            scriptContainer = CreateTransaction(HASHES[0]);
            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
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
