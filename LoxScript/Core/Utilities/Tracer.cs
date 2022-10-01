using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XPT.Core.Utilities {
    internal static class Tracer {
        /// <summary>
        /// Will show error message but attempt to keep running.
        /// </summary>
        /// <param name="text"></param>
        internal static void Warn(string text) {
            Console.WriteLine(text);
        }
    }
}
