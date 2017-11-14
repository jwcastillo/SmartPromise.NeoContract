using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace SmartPromise
{
    public class SmartPromise : SmartContract
    {
        /**
         * DIFFERENT TYPES OF DATA IN STORAGE HAVE DIFFERENT PREFIXES
           'P' - FOR STORING PROMISE DATA
           'C' - FOR STORING PROMISES COUNTER
         */
        public static char KEY_PREFIX_COUNT() => 'C';
        private static char KEY_PREFIX_PROMISE() => 'P';

        public static string GetPromiseCounterKey(string senderSH) => KEY_PREFIX_COUNT() + senderSH;
        
        public static string GetPromiseKey(string senderSH, BigInteger index) {
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
            return (res.Length == 0)? 1 : res.AsBigInteger();
        }

        /**PUTS PROMISE COUNTER IN STORAGE*/
        private static void PutPromiseCounter(string senderSH, BigInteger counter)
        {
            string key = GetPromiseCounterKey(senderSH);
            Storage.Put(Storage.CurrentContext, key, counter);
        }
        
        private static byte[] GetSenderScriptHash()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            TransactionOutput firstReference = reference[0];
            return firstReference.ScriptHash;
        }

        public static bool Main(string operation, params object[] args)
        {
            /**ALL KEYS USED FOR DATA STORING IN BLOCKCHAIN WOULD BE BASED ON SENDER SCRIPT HASH*/
            byte[] senderSH = GetSenderScriptHash();

            switch (operation)
            {
                case "replace":
                    return Replace(senderSH.AsString(), (string)args[0], (BigInteger)args[1]); 
                case "add":
                    return Add(senderSH.AsString(), (string)args[0]);
                default:
                    return false;
            }
        }
        
        /**
         * FINDS PROMISE BY INDEX AND REPLACE IT WITH NEW ONE
         * RETURNS FALSE, IF HAVEN'T FOUND PROMISE TO REPLACE
         */
        private static bool Replace(string senderSH, string promise, BigInteger index)
        {
            string key = GetPromiseKey(senderSH, index);

            byte[] res = Storage.Get(Storage.CurrentContext, key);

            if (res == null)
                return false;

            Storage.Put(Storage.CurrentContext, key, promise);
            return true;
        }
        
        /**
         * PUTS NEW PROMISE IN USER'S STORAGE
         * UPDATES PROMISES COUNTER AND PUTS IT IN STORAGE
         * (PROMISES ARE NUMERATED FROM 1)
         */
        private static bool Add(string senderSH, string promiseJson)
        {
            BigInteger counter = GetPromiseCounter(senderSH);
            
            string key = GetPromiseKey(senderSH, counter);
            Runtime.Notify("Add. PromiseKey : ", key, " Counter : ", counter);
            Storage.Put(Storage.CurrentContext, key, promiseJson);
            
            counter += 1;
            PutPromiseCounter(senderSH, counter);
            return true;
        }
    }
}
