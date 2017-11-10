using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace SmartPromise
{
    public class SmartPromise : SmartContract
    {
        
        public static string KEY_PREFIX_COUNT() => "C";
        private static string KEY_PREFIX_PROMISE() => "P";
        
        public static string GetPromiseCountKey(string ownerKey) => KEY_PREFIX_COUNT() + ownerKey;
        public static string GetPromiseKey(string ownerKey, BigInteger index) => 
            KEY_PREFIX_PROMISE() + ownerKey + index; 
            
        private static BigInteger GetPromiseCount(string ownerKey)
        {
            string promiseCountKey = GetPromiseCountKey(ownerKey);
            byte[] res = Storage.Get(Storage.CurrentContext, promiseCountKey);
            return (res == null) ? 0 : res.AsBigInteger();
        }
        
        public static bool Main(string operation, params object[] args)
        {
            switch (operation)
            {
                case "replace":
                    return Replace((string)args[0], (string)args[1], (BigInteger)args[2]); 
                case "add":
                    return Add((string)args[0], (string)args[1]);
                default:
                    return false;
            }
        }
        
        private static bool Replace(string ownerKey, string promise, BigInteger index)
        {
            string promiseKey = GetPromiseKey(ownerKey, index);

            byte[] res = Storage.Get(Storage.CurrentContext, promiseKey);

            if (res == null)
                return false;

            Storage.Put(Storage.CurrentContext, promiseKey, promise);
            return true;
        }
        
        private static bool Add(string ownerKey, string promiseJson)
        {
            BigInteger count = GetPromiseCount(ownerKey);

            string promiseKey = GetPromiseKey(ownerKey, count);
            Storage.Put(Storage.CurrentContext, promiseKey, promiseJson);

            if (count == 0)
                count += 1;
            
            return true;
        }
    }
}
