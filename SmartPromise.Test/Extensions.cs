﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPromise.Test
{
    public static class Extensions
    {
        /// <summary>
        /// USED TO CONVERT BYTE ARRAY TO HEX STRING
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToHexString(this IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
