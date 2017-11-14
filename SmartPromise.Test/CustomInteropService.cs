using Neo;
using Neo.Core;
using Neo.VM;
using System.Collections;
using System.Linq;
using System.Text;

namespace SmartPromise.Test
{
    class CustomInteropService : InteropService
    {

        public CustomStorageContext storageContext;
        public Hashtable transactions;
        
        public CustomInteropService()
        {
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("Neo.Storage.Put", Storage_Put);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            storageContext = new CustomStorageContext();
            transactions = new Hashtable();
        }


        protected virtual bool Runtime_CheckWitness(ExecutionEngine engine)
        {          
            StackItem si = engine.EvaluationStack.Pop();
            engine.EvaluationStack.Push(true);
            return true;
        }

        public bool Storage_GetContext(ExecutionEngine engine)
        {
            this.storageContext.ScriptHash = new UInt160(engine.CurrentContext.ScriptHash);
            engine.EvaluationStack.Push(StackItem.FromInterface(storageContext));
            return true;
        }

        protected bool Storage_Get(ExecutionEngine engine)
        {
            CustomStorageContext context = engine.EvaluationStack.Pop().GetInterface<CustomStorageContext>();
            var key = engine.EvaluationStack.Pop().GetByteArray().ToHexString();
            StorageItem item = new StorageItem
            {
                Value = (byte[])context.data[key]
            };
            engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
            return true;
        }

        protected bool Storage_Put(ExecutionEngine engine)
        {
            CustomStorageContext context = engine.EvaluationStack.Pop().GetInterface<CustomStorageContext>();
            var key = engine.EvaluationStack.Pop().GetByteArray().ToHexString();
            if (key.Length > 1024)
                return false;
            byte[] value = engine.EvaluationStack.Pop().GetByteArray();
          
            context.data[key] = value;
            return true;
        }

        protected bool Transaction_GetInputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            Transaction tx = (Transaction)this.transactions[hash];
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected virtual bool Input_GetHash(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        protected virtual bool Output_GetScriptHash(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null)
                return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }

        protected virtual bool Input_GetIndex(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        protected virtual bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Transaction_GetReferences(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null)
                return false;
            CustomTransaction ctx = tx as CustomTransaction;
            if (ctx == null)
                return false;
            engine.EvaluationStack.Push(ctx.CustomReferences.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Runtime_Notify(ExecutionEngine engine)
        {
            StackItem state = engine.EvaluationStack.Pop();
            return true;
        }

    }
}
