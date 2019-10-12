using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LittleHelper
{
    class Text
    {
        public static string[] Split(string text)
        {
            var separator = new char[] { ',', '，', '。', '？', '；', '\n'};
            return text.Split(separator).Select(i => i.Trim()).Where(i => i != string.Empty).ToArray();
        }
    }
}
