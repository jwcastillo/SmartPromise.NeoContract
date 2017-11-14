using Neo.Core;

namespace SmartPromise.Test
{
    class CustomTransaction : Transaction
    {
        public TransactionOutput[] CustomReferences { get; set; }
        public CustomTransaction(TransactionType type) : base(type)
        {
            Version = 1;
            Inputs = new CoinReference[0];
            Outputs = new TransactionOutput[0];
            Attributes = new TransactionAttribute[0];
        }
    }
}
