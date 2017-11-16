using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPromise.Test
{
    public enum PROMISE_STATUS { COMPLTED = 0, NOT_COMPLTED, ERROR };
    
    /// <summary>
    /// THAT'S HOW OUR PROMISE DATA LOOKS LIKE
    /// </summary>
    [Serializable]
    public class Promise
    {
        static public int MAX_COMPLICITY = 5;

        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int Complicity { get; set; }
        public PROMISE_STATUS Status { get; set; }
        public DateTime Date { get; set; }
        public string Proof { get; set; }
    }
}
