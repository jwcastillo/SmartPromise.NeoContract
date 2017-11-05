/*
 * 
 * Thanks to user tekn (https://neosmarteconomy.slack.com) for helping me!
 * This code took from there https://github.com/aphtoken/NeoContractTester/tree/e8efbeccb836ea2e8252585da7bdcd56dfad042a
 * 
 */
using Neo;
using Neo.VM;
using System;
using System.Collections;

namespace SmartPromise.Test
{
    class CustomStorageContext : IInteropInterface
    {
        public UInt160 ScriptHash;

        public Hashtable data;

        public CustomStorageContext()
        {
            data = new Hashtable();
        }

        public byte[] ToArray()
        {
            return ScriptHash.ToArray();
        }
    }
}
