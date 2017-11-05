/*
 * 
 * Thanks to user tekn (https://neosmarteconomy.slack.com) for helping me!
 * This code took from there https://github.com/aphtoken/NeoContractTester/tree/e8efbeccb836ea2e8252585da7bdcd56dfad042a
 * 
 */
using Neo.Core;

namespace SmartPromise.Test
{
    class CustomTransaction : Transaction
    {
        public CustomTransaction(TransactionType type) : base(type)
        {
            Version = 1;
            Inputs = new CoinReference[0];
            Outputs = new TransactionOutput[0];
            Attributes = new TransactionAttribute[0];
        }
    }
}
