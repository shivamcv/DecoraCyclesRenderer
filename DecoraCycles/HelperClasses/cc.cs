using ccl;
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


       internal static Transform stringToTransform(string cameraView)
       {
           Transform view = Transform.Identity();
           
           var temp = cameraView.Split(';');
           
           if(temp.Count() == 3)
           {
               if(!string.IsNullOrEmpty(temp[0]))
               {
                   var trans = temp[0].Split(',');
                   if(trans.Count() ==3)
                   view = Transform.Scale(float.Parse(trans[0]), float.Parse(trans[1]), float.Parse(trans[2]));
               }

               if (!string.IsNullOrEmpty(temp[1]))
               {
                   var trans = temp[1].Split(',');
                   if (trans.Count() == 4)
                   view *= Transform.Rotate(float.Parse(trans[0]), new float4(float.Parse(trans[1]), float.Parse(trans[2]), float.Parse(trans[3])));
               }

               if (!string.IsNullOrEmpty(temp[2]))
               {
                   var trans = temp[2].Split(',');
                   if (trans.Count() == 3)
                   view *= Transform.Translate(float.Parse(trans[0]), float.Parse(trans[1]), float.Parse(trans[2]));
               }
           }

           return view;
       }

       public static Transform CommaSeparateStringToTransform(string view)
       {
           view = view.Replace("f", "");
           view = view.Replace("\n", "");
           float[] f = new float[16];

           var t = view.Split(',');

           if (t.Length != 16) return Transform.Identity();

           for (int i = 0; i < 16; i++)
           {
               float g = 0;

               float.TryParse(t[i], out g);
               f[i] = g;
           }

           return new Transform(f);

       }
    }
}
