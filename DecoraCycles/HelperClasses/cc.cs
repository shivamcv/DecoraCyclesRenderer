using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.HelperClasses
{
   public static class cc
    {
       public static int ColorClamp(int ch)
       {
           if (ch < 0) return 0;
           return ch > 255 ? 255 : ch;
       }

       static string GetString(byte[] bytes)
       {
           char[] chars = new char[bytes.Length / sizeof(char)];
           System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
           return new string(chars);
       }

    }
}
