using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System.Numerics;

namespace SmartPromise
{
    public class SmartPromise : SmartContract
    {

        /**
         * TOKEN SETTINGS
         */
        public static string Name() => "SmartCoin";
        public static string Symbol() => "SC";
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        
        /**
         * DIFFERENT TYPES OF DATA IN STORAGE HAVE DIFFERENT PREFIXES
           'P' - FOR STORING PROMISE DATA
           'C' - FOR STORING PROMISES COUNTER
         */
        public static char KEY_PREFIX_COUNT() => 'C';
        private static char KEY_PREFIX_PROMISE() => 'P';

        private static string GetPromiseCounterKey(string senderSH) => KEY_PREFIX_COUNT() + senderSH;
        
        private static string GetPromiseKey(string senderSH, BigInteger index) {
            /**
             * WHEN CONCATINATING MORE THAN TWO STRINGS WITHIN ONE EXPRESSION, 
             * ONLY TWO OF THEM CONCATINATES
             */
            string part = KEY_PREFIX_PROMISE() + senderSH;
            return part + index;
        }
            
        private static BigInteger GetPromiseCounter(string senderSH)
        {
            string promiseCounterKey = GetPromiseCounterKey(senderSH);
            var res = Storage.Get(Storage.CurrentContext, promiseCounterKey);
            return (res.Length == 0)? 0 : res.AsBigInteger();
        }


        /// <summary>
        /// PUTS PROMISE COUNTER IN STORAGE
        /// </summary>
        /// <param name="senderSH"></param>
        /// <param name="counter"></param>
        private static void PutPromiseCounter(string senderSH, BigInteger counter)
        {
            string key = GetPromiseCounterKey(senderSH);
            Storage.Put(Storage.CurrentContext, key, counter);
        }

        /// <summary>
        /// MAIN FUNCTION
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Main(string operation, params object[] args)
        {
            switch (operation)
            {
                case "replace":
                        return Replace((string)args[0], (string)args[1], (BigInteger)args[2]);
                case "add":
                        return Add((string)args[0], (string)args[1]);
                case "mintTokens":
                    return MintTokens();
                case "transfer":
                    {
                        string from = (string)args[0];
                        string to = (string)args[1];
                        BigInteger value = (BigInteger)args[2];
                        return Transfer(from, to, value);
                    }
                case "name":
                    return Name();
                case "symbol":
                    return Symbol();
                default:
                    return false;
            }
        }

        /// <summary>
        /// TRANSFERS SMART COIN TOKEN BETWEEN ADDRESSES
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool Transfer(string from, string to, BigInteger value)
        {
            if (!Runtime.CheckWitness(from.AsByteArray()))
                return false;
            
            if (value <= 0)
                return false;
            
            if (from == to)
                return true;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();

            if (from_value < value)
                return false;
            
            Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Runtime.Notify("TRANSFERED", from, to, value);
            return true;
        }

        /// <summary>
        /// EXCHANGE NEO ASSET ON SMART COIN TOKEN
        /// </summary>
        /// <returns></returns>
        public static bool MintTokens()
        {
            string senderSH = GetSender().AsString();

            if (senderSH.Length == 0)
                return false;
            BigInteger value = GetContributeValue();
            if (value == 0)
                return false;
            byte[] ba = Storage.Get(Storage.CurrentContext, senderSH);

            BigInteger balance;
            if (ba.Length == 0)
                balance = 0;
            else
                balance = ba.AsBigInteger();
            Storage.Put(Storage.CurrentContext, senderSH, value + balance);
            
            Runtime.Notify("MINTED", value + balance, senderSH);
            return true;
        }


        /// <summary>
        /// FINDS PROMISE BY INDEX AND REPLACE IT WITH NEW ONE
        /// RETURNS FALSE, IF HAVEN'T FOUND PROMISE TO REPLACE
        /// </summary>
        /// <param name="senderSH"></param>
        /// <param name="promise"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool Replace(string senderSH, string promise, BigInteger index)
        {
            if (!Runtime.CheckWitness(senderSH.AsByteArray()))
                return false;
            string key = GetPromiseKey(senderSH, index);

            byte[] res = Storage.Get(Storage.CurrentContext, key);

            if (res == null)
                return false;

            Storage.Put(Storage.CurrentContext, key, promise);
            Runtime.Notify("REPLACED", senderSH, key);
            return true;
        }
        
        /// <summary>
        /// PUTS NEW PROMISE IN USER'S STORAGE
        /// UPDATES PROMISES COUNTER AND PUTS IT IN STORAGE
        /// PROMISES ARE NUMERATED FROM 1
        /// </summary>
        /// <param name="senderSH"></param>
        /// <param name="promiseJson"></param>
        /// <returns></returns>
        private static bool Add(string senderSH, string promiseJson)
        {
            if (!Runtime.CheckWitness(senderSH.AsByteArray()))
                return false;
            BigInteger counter = GetPromiseCounter(senderSH);
            counter += 1;

            string key = GetPromiseKey(senderSH, counter);
            Storage.Put(Storage.CurrentContext, key, promiseJson);
            
            PutPromiseCounter(senderSH, counter);
            Runtime.Notify("ADDED", senderSH, counter);
            return true;
        }
        
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
            }
            return new byte[0];
        }
        
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }
        
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == neo_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }
    }
}
