using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
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

        /**PUTS PROMISE COUNTER IN STORAGE*/
        private static void PutPromiseCounter(string senderSH, BigInteger counter)
        {
            string key = GetPromiseCounterKey(senderSH);
            Storage.Put(Storage.CurrentContext, key, counter);
        }
        
        private static string GetSenderScriptHash()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            /**GETS ALL TRANSACTIONS OUTPUTS THAT POINTS TO THIS TRANSACTION*/
            TransactionOutput[] reference = tx.GetReferences();
            return reference[0].ScriptHash.AsString();
        }

        private static string GetNeoSenderScriptHash()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            /**GETS ALL TRANSACTIONS OUTPUTS THAT POINTS TO THIS TRANSACTION*/
            TransactionOutput[] reference = tx.GetReferences();
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id)
                    return output.ScriptHash.AsString();
            }
            return new byte[0].AsString();

        }

        /**SUMS AND RETURNS ALL NEO INPUT VALUES IN THIS TRANSACTION*/
        private static BigInteger GetContributeNeo()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] references = tx.GetReferences();

            BigInteger contributed = 0;
            foreach(var reference in references)
            {
                if (reference.AssetId == neo_asset_id)  {
                    contributed += reference.Value;
                }
            }
            return contributed;
        }

        public static object Main(string operation, params object[] args)
        {
            string senderSH;

            switch (operation)
            {
                case "replace":
                    {
                        senderSH = GetSenderScriptHash();
                        return Replace((string)args[0], (string)args[1], (BigInteger)args[2]);
                    }
                case "add":
                    {
                        senderSH = GetSenderScriptHash();
                        return Add((string)args[0], (string)args[1]);
                    }
                case "mintTokens":
                    return MintTokens();
                case "transfer":
                    {
                        string from = (string)args[0];
                        string to = (string)args[1];
                        BigInteger value = (BigInteger)args[2];
                        return Transfer(from, to, value);
                    }
                default:
                    return false;
            
            }
        }
        
        /**TRANSFERS SMART COIN TOKEN BETWEEN ADDRESSES*/
        private static bool Transfer(string from, string to, BigInteger value)
        {
            Runtime.Notify("Transfer from ", from, " To ", to, " value ", value);
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
            return true;
        }
        
        /**EXCHANGE NEO ASSET ON SMART COIN TOKEN*/
        public static bool MintTokens()
        {
            string senderSH = GetNeoSenderScriptHash();
            Runtime.Notify("Mint sender ", senderSH);
            BigInteger value = GetContributeNeo();
            Runtime.Notify("Amount ", value);

            if (value == 0)
            {
                return false;
            }

            byte[] ba = Storage.Get(Storage.CurrentContext, senderSH);

            BigInteger balance;
            if (ba.Length == 0)
            {
                balance = 0;
            }
            else
            {
                balance = ba.AsBigInteger();
            }

            Storage.Put(Storage.CurrentContext, senderSH, value + balance);
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
