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
    public class UnitTest
    {
        private string[] HASHES = new string[] {
            "0x22a4d553282d7eaf53538eb8ccb27e842d0d90b6",
            "0xbc89c04256bd0a5b9d53a0d239d615a8734bc459",
            "0x1e66cccfed7a0a9f4bc9bf6c92b286acef65fc77",
            "0xa42abb913fa551de74fd4626ad4a789a2987e52e"
        };

        [TestInitialize]
        public void InitInteropservice()
        {
            Helper.Init();
        }
        
        [TestMethod]
        public void CanMintWhenMultipleInputs()
        {
            /*
            var data = Helper.service.storageContext.data;
            string key;
            byte[] ba;

            Helper.InitTransactionContext(HASHES[0], 5, 1);
            Assert.AreEqual(Helper.MintTokens(), true);
            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 15);
            */
        }

        [TestMethod]
        public void CanCompletePromise()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            var data = Helper.service.storageContext.data;
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

            Assert.AreEqual(Helper.AddPromise(HASHES[0], promiseNotCompleted), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseNotCompleted));
            string promiseKey = Helper.GetPromiseKey(HASHES[0], 1);
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);

            Assert.AreEqual(Helper.ReplacePromise(HASHES[0], promiseCompleted, 1), true);
            Assert.AreEqual(data.Count, 2);
            promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promiseCompleted));
            Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseKey]), true);
        }

        [TestMethod]
        public void PromisesSizeCountesProperly()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            var data = Helper.service.storageContext.data;
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
                Assert.AreEqual(Helper.AddPromise(HASHES[0], promise), true);
                /** EVERY NEW PROMISE CREATES NEW RECORD IN STORAGE PLUS ONE RECORD TO STORE PROMISES COUNT*/
                Assert.AreEqual(data.Count, i + 1);
                
                promiseCounterKey = Helper.GetPromiseCounterKey(HASHES[0]);
                ba = (byte[])data[promiseCounterKey];
                counter = (int)ba[0];
                Assert.AreEqual(counter, i);
            }

            /**ANOTHER USER MAKES PROMISE*/
            Helper.InitTransactionContext(HASHES[1], 10);
            Assert.AreEqual(Helper.AddPromise(HASHES[1], promise), true);
            Assert.AreEqual(data.Count, 101 + 2);
            promiseCounterKey = Helper.GetPromiseCounterKey(HASHES[1]);
            ba = (byte[])data[promiseCounterKey];
            counter = (int)ba[0];
            Assert.AreEqual(counter, 1);

            /**PREVIOUS USER MAKES PROMISE*/
            Helper.InitTransactionContext(HASHES[0], 10);
            promiseCounterKey = Helper.GetPromiseCounterKey(HASHES[0]);
            Assert.AreEqual(Helper.AddPromise(HASHES[0], promise), true);
            Assert.AreEqual(data.Count, 103 + 1);
            ba = (byte[])data[promiseCounterKey];
            counter = (int)ba[0];
            Assert.AreEqual(counter, 101);
        }


        
        [TestMethod]
        public void CanMintTokens()
        {
            var data = Helper.service.storageContext.data;
            string key;
            byte[] ba;

            Helper.InitTransactionContext(HASHES[0], 5);
            Assert.AreEqual(Helper.MintTokens(), true);
            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 5);

            Helper.InitTransactionContext(HASHES[1], 40);
            Assert.AreEqual(Helper.MintTokens(), true);
            key = Helper.GetKey(HASHES[1]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 40);

            Helper.InitTransactionContext(HASHES[0], 100);
            Assert.AreEqual(Helper.MintTokens(), true);
            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 100 + 5);

            Helper.InitTransactionContext(HASHES[0], 5);
            Assert.AreEqual(Helper.MintTokens(), true);
            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 105 + 5);
        }

        [TestMethod]
        public void ReturnsFalseWhenContributedValueIsZero()
        {
            Helper.InitTransactionContext(HASHES[0], 0);
            var data = Helper.service.storageContext.data;
            Assert.AreEqual(Helper.MintTokens(), false);
        }
        
        
        [TestMethod]
        public void CanTransferToken()
        {
            var data = Helper.service.storageContext.data;
            string key;
            byte[] ba;

            Helper.InitTransactionContext(HASHES[1], 10);
            Assert.AreEqual(Helper.MintTokens(), true);

            Assert.AreEqual(Helper.TransferToken(HASHES[1], HASHES[0], 3), true);
            
            key = Helper.GetKey(HASHES[1]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 10 - 3);
            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 0 + 3);

            /**SENDING TO YOURSELF*/
            Assert.AreEqual(Helper.TransferToken(HASHES[1], HASHES[1], 2), true);
            key = Helper.GetKey(HASHES[1]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 7);

            Helper.InitTransactionContext(HASHES[2], 10);
            Assert.AreEqual(Helper.MintTokens(), true);
            Assert.AreEqual(Helper.TransferToken(HASHES[2], HASHES[1], 4), true);
            key = Helper.GetKey(HASHES[2]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 10 - 4);
            key = Helper.GetKey(HASHES[1]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 7 + 4);
        }

        [TestMethod]
        public void CanTransferTokenWhenInsufficient()
        {
            Helper.InitTransactionContext(HASHES[1], 10);
            Assert.AreEqual(Helper.MintTokens(), true);
            Assert.AreEqual(Helper.TransferToken(HASHES[1], HASHES[0], 14), false);
        }
        
        [TestMethod]
        public void CanSendAllTokens()
        {
            var data = Helper.service.storageContext.data;
            string key;
            byte[] ba;

            Helper.InitTransactionContext(HASHES[1], 10);
            Assert.AreEqual(Helper.MintTokens(), true);
            Assert.AreEqual(Helper.TransferToken(HASHES[1], HASHES[0], 10), true);

            key = Helper.GetKey(HASHES[1]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 10 - 10);

            key = Helper.GetKey(HASHES[0]);
            ba = (byte[])data[key];
            Assert.AreEqual((int)ba[0], 0 + 10);
        }

        [TestMethod]
        public void ReturnsFalseWhenReplaceNotExistingPromise()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            ExecutionEngine engine = new ExecutionEngine(Helper.scriptContainer, Crypto.Default, null, Helper.service);
            engine.LoadScript(File.ReadAllBytes(Helper.CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(10);
                sb.EmitPush("aaaaaaaaaaaaa");
                sb.EmitPush("notExistingPromise");
                sb.EmitPush(3);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(Helper.OPERATION_REPLACE_PROMISE);
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
            
            var data = Helper.service.storageContext.data;
            byte[] promiseBytes = null;
            Assert.AreEqual(data.Count, 0);

            for (int i = 0; i < HASHES.Length; ++i)
            {
                /**CHANGE USER SCRIPT HASH*/
                Helper.InitTransactionContext(HASHES[i], 10);
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

                Assert.AreEqual(Helper.AddPromise(HASHES[i], promise), true);
                /**ONE RECORD FOR PROMISE DATA AND ONE RECORD FOR PROMISES COUNTER*/
                Assert.AreEqual(data.Count, (i + 1) * 2);
                /**PROMISES ID NUMERATION STARTS WITH ONE*/
                string promiseId = Helper.GetPromiseKey(HASHES[i], 1);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseId]), true);
            }
        }
        
        [TestMethod]
        public void CanCountPromisesProperly()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            var data = Helper.service.storageContext.data;
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

            Assert.AreEqual(Helper.AddPromise(HASHES[0], promise), true);
            Assert.AreEqual(data.Count, 2);

            /** ANOTHER USER INVOKED CONTRACT*/
            Helper.InitTransactionContext(HASHES[1], 10);
            Assert.AreEqual(Helper.AddPromise(HASHES[1], promise), true);
            Assert.AreEqual(data.Count, 2 + 2);

            Assert.AreEqual(Helper.AddPromise(HASHES[1], promise), true);
            Assert.AreEqual(data.Count, 4 + 1);

            Assert.AreEqual(Helper.AddPromise(HASHES[1], promise), true);
            Assert.AreEqual(data.Count, 5 + 1);

            /** ONE MORE USER INVOKED CONTRACT*/
            Helper.InitTransactionContext(HASHES[2], 10);
            Assert.AreEqual(Helper.AddPromise(HASHES[2], promise), true);
            Assert.AreEqual(data.Count, 6 + 2);

            /** PREVIOUS USER INVOKED CONTRACT*/
            Helper.InitTransactionContext(HASHES[0], 10);
            Assert.AreEqual(Helper.AddPromise(HASHES[0], promise), true);
            Assert.AreEqual(data.Count, 8 + 1);

            /** ONE MORE USER INVOKED CONTRACT*/
            Helper.InitTransactionContext(HASHES[3], 10);
            Assert.AreEqual(Helper.AddPromise(HASHES[3], promise), true);
            Assert.AreEqual(data.Count, 9 + 2);
        }
        
        [TestMethod]
        public void CanAddMultiplePromisesToOwner()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            var data = Helper.service.storageContext.data;
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

                Assert.AreEqual(Helper.AddPromise(HASHES[0], promise), true);
                /** EVERY NEW PROMISE CREATES NEW RECORD IN STORAGE PLUS ONE RECORD TO STORE PROMISES COUNT*/
                Assert.AreEqual(data.Count, i + 1);
                string promiseId = Helper.GetPromiseKey(HASHES[0], i);
                promiseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(promise));
                Assert.AreEqual(promiseBytes.SequenceEqual((byte[])data[promiseId]), true);
            }
        }

        [TestMethod]
        public void ReturnsFalseWhenInvalidOperation()
        {
            Helper.InitTransactionContext(HASHES[0], 10);
            ExecutionEngine engine = new ExecutionEngine(Helper.scriptContainer, Crypto.Default, null, Helper.service);
            engine.LoadScript(File.ReadAllBytes(Helper.CONTRACT_ADDRESS));

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
