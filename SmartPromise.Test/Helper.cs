using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.Core;
using Neo.Cryptography;
using Neo.VM;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace SmartPromise.Test
{
    static class Helper
    {
        /// <summary>
        /// LIST OF AVAILABLE OPERATIONS IN SMART CONTRACT
        /// </summary>
        public static string OPERATION_ADD_PROMISE = "add";
        public static string OPERATION_REPLACE_PROMISE = "replace";
        public static string OPERATION_MINT_TOKENS = "mintTokens";
        public static string OPERATION_TRANSFER = "transfer";

        /// <summary>
        /// DEFINES WHERE TO GET SMART PAOMISE CONTRACT GENERATED SCRIPT
        /// </summary>
        public static string CONTRACT_ADDRESS = @"..\..\..\SmartPromise\bin\Debug\SmartPromise.avm";
        public static IScriptContainer scriptContainer;
        public static CustomInteropService service;

        private const string NEO_ASSET_ID = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";

        /// <summary>
        /// INITIALIZATION
        /// </summary>
        public static void Init()
        {
            service = new CustomInteropService();
            service.storageContext.data = new Hashtable();
        }

        /// <summary>
        /// GIVES KEY, THAT ALLOWS YOU TO ACCESS PROMISE DATA IN BLOCKCHAIN STORAGE
        /// </summary>
        /// <param name="ownerKey"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string GetPromiseKey(string ownerKey, int i)
        {
            const char PROMISE_PREFIX = 'P';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = GetKey(ownerKey);
            var postfix = Convert.ToByte(i).ToString("x2");
            return prefix + main + postfix;
        }

        /// <summary>
        /// GIVES KEY, USED FOR GENERATING PROMISE AND PROMISE COUNTER KEYS
        /// </summary>
        /// <param name="ownerKey"></param>
        /// <returns></returns>
        public static string GetKey(string ownerKey)
        {
            return UInt160.Parse(ownerKey).ToArray().ToHexString();
        }

        /// <summary>
        /// GIVES PROMISE COUNTER KEY, USED FOR ACCESING PROMISE COUNTER IN BLOCKCHAIN
        /// </summary>
        /// <param name="ownerKey"></param>
        /// <returns></returns>
        public static string GetPromiseCounterKey(string ownerKey)
        {
            const char PROMISE_PREFIX = 'C';
            var prefix = Convert.ToByte(PROMISE_PREFIX).ToString("x2");
            var main = GetKey(ownerKey);
            return prefix + main;
        }

        /// <summary>
        /// TRANSFERS PARTICULAR AMOUNT OF TOKENS BETWEEN SCRIPT HASHES
        /// </summary>
        /// <param name="fromSH"></param>
        /// <param name="toSH"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TransferToken(string fromSH, string toSH, int value)
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

        /// <summary>
        /// ADD A PROMISE TO BLOCKCHAIN
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="promise"></param>
        /// <returns></returns>
        public static bool AddPromise(string hash, Promise promise)
        {
            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {

                sb.EmitPush(jsonPromise);
                sb.EmitPush(UInt160.Parse(hash));
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(OPERATION_ADD_PROMISE);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();

            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.EvaluationStack.Peek().GetBoolean();
            return result;
        }

        /// <summary>
        /// REPLACES PROMISE
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="promise"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool ReplacePromise(string hash, Promise promise, int index)
        {
            var jsonPromise = JsonConvert.SerializeObject(promise);

            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(index);
                sb.EmitPush(jsonPromise);
                sb.EmitPush(UInt160.Parse(hash));
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

        /// <summary>
        /// MINT TOKENS
        /// </summary>
        /// <returns></returns>
        public static bool MintTokens()
        {
            ExecutionEngine engine = new ExecutionEngine(scriptContainer, Crypto.Default, null, service);
            engine.LoadScript(File.ReadAllBytes(Helper.CONTRACT_ADDRESS));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(0);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(Helper.OPERATION_MINT_TOKENS);
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);
            return engine.EvaluationStack.Peek().GetBoolean();
        }

        /// <summary>
        /// INIT HISTORY OF TRANSACTIONS
        /// </summary>
        /// <param name="scriptHash"></param>
        /// <param name="value"></param>
        /// <param name="inputAmount"></param>
        public static void InitTransactionContext(string scriptHash, int value, ushort inputAmount = 1)
        {
            Transaction initialTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            Transaction currentTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            initialTransaction.Outputs = new TransactionOutput[inputAmount];
            currentTransaction.Inputs = new CoinReference[inputAmount];

            for (ushort i = 0; i < inputAmount; ++i)
            {
                /** CREATE FAKE PREVIOUS TRANSACTION */
                var transactionOutput = new TransactionOutput
                {
                    ScriptHash = UInt160.Parse(scriptHash),
                    Value = new Fixed8(value),
                    AssetId = UInt256.Parse(NEO_ASSET_ID)
                };

                initialTransaction.Outputs[i] = transactionOutput;
                /** CREATE FAKE CURRENT TRANSACTION */
                var coinRef = new CoinReference
                {
                    PrevHash = initialTransaction.Hash,
                    PrevIndex = i
                };


                currentTransaction.Inputs[i] = coinRef;
            }


            /**INIT CONTEXT*/
            service.transactions[initialTransaction.Hash] = initialTransaction;
            scriptContainer = currentTransaction;
        }
    }
}
