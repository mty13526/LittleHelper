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
            var separator = new char[] { ',', '，', '。', '？', '；' };
            return text.Split(separator);
        }
    }
}
