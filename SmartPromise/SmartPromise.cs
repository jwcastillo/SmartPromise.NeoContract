using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace SmartPromise
{
    public class SmartPromise : SmartContract
    {

        /**TOKEN SETTINGS*/
        public static string Name() => "SmartCoin";
        public static string Symbol() => "Sc";
        public static readonly byte[] Owner = { 47, 60, 170, 33, 216, 40, 148, 2, 242, 150, 9, 84, 154, 50, 237, 160, 97, 90, 55, 183 };
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        
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
            return (res.Length == 0)? 0 : res.AsBigInteger();
        }

        /**PUTS PROMISE COUNTER IN STORAGE*/
        private static void PutPromiseCounter(string senderSH, BigInteger counter)
        {
            string key = GetPromiseCounterKey(senderSH);
            Storage.Put(Storage.CurrentContext, key, counter);
        }
        
        private static string GetSenderScriptHash()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            TransactionOutput firstReference = reference[0];
            return firstReference.ScriptHash.AsString();
        }

        private static BigInteger GetContributedNeo()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            TransactionOutput firstReference = reference[0];
            
            BigInteger a = firstReference.Value;
            if (firstReference.AssetId == neo_asset_id)
            {
                return firstReference.Value;
            }
            else
            {
                return 0;
            }
        }

        public static object Main(string operation, params object[] args)
        {
            /**ALL KEYS USED FOR DATA STORING IN BLOCKCHAIN WOULD BE BASED ON SENDER SCRIPT HASH*/
            string senderSH = GetSenderScriptHash();

            switch (operation)
            {
                case "replace":
                    return Replace(senderSH, (string)args[0], (BigInteger)args[1]); 
                case "add":
                    return Add(senderSH, (string)args[0]);
                case "mintTokens":
                    {
                        var s = GetSenderScriptHash();
                        return MintTokens(s);
                    }
                case "transfer":
                    {
                        string to = (string)args[0];
                        BigInteger value = (BigInteger)args[1];
                        return Transfer(senderSH, to, value);
                    }
                default:
                    return false;
            
            }
        }
        
        private static bool Transfer(string from, string to, BigInteger value)
        {
            Runtime.Notify("Transfer from ", from, " To ", to, " value ", value);
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
            return true;
        }
        
        public static bool MintTokens(string sender)
        {
            Runtime.Notify("Mint sender ", sender);
            BigInteger value = GetContributedNeo();
            Runtime.Notify("Amount ", value);

            if (value == 0)
            {
                return false;
            }

            byte[] ba = Storage.Get(Storage.CurrentContext, sender);

            BigInteger balance;
            if (ba.Length == 0)
            {
                balance = 0;
            }
            else
            {
                balance = ba.AsBigInteger();
            }

            Storage.Put(Storage.CurrentContext, sender, value + balance);
            return true;
        }


        /**
         * FINDS PROMISE BY INDEX AND REPLACE IT WITH NEW ONE
         * RETURNS FALSE, IF HAVEN'T FOUND PROMISE TO REPLACE
         */
        private static bool Replace(string senderSH, string promise, BigInteger index)
        {
            Runtime.Notify("Replace senderSH ", senderSH, " iNDEX ", index);
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
            Runtime.Notify("Add sender ", senderSH);
            

            BigInteger counter = GetPromiseCounter(senderSH);
            Runtime.Notify("Counter ", counter);
            counter += 1;

            string key = GetPromiseKey(senderSH, counter);
            Storage.Put(Storage.CurrentContext, key, promiseJson);
            
            PutPromiseCounter(senderSH, counter);
            return true;
        }
    }
}
