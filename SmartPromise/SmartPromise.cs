using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;

namespace SmartPromise
{
    public class SmartPromise : SmartContract
    {
        
        private static string KEY_PREFIX_COUNT() => "C";
        private static string KEY_PREFIX_PROMISE() => "P";
        
        private static string GetPromiseCountKey(string ownerKey) => KEY_PREFIX_COUNT() + ownerKey;
        private static string GetPromiseKey(string ownerKey, BigInteger index) => 
            KEY_PREFIX_PROMISE() + ownerKey + index; 
            
        private static BigInteger GetPromiseCount(string ownerKey)
        {
            string promiseCountKey = GetPromiseCountKey(ownerKey);
            byte[] res = Storage.Get(Storage.CurrentContext, promiseCountKey);
            return (res == null) ? 0 : res.AsBigInteger();
        }
        
        public static bool Main(string operation, string ownerKey, string promiseJson)
        {
            switch (operation)
            {
                case "replace":
                    return Replace(ownerKey, promiseJson); 
                case "add":
                    return Add(ownerKey, promiseJson);
                default:
                    return false;
            }
        }
        
        private static bool Replace(string ownerKey, string promise)
        {
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
