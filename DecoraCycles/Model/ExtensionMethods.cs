using ccl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.Model
{
  public static  class ExtensionMethods
    {
       public static Transform XnaToBlenderMatrix(this Transform t)
      {
           Transform temp = new Transform(t.x.x, t.x.y, t.x.z, t.w.x,
                                          t.y.x, t.y.y, -t.y.z, t.w.y,
                                          t.z.x, t.z.y, t.z.z, t.w.z,
                                          t.x.w, t.y.w, t.z.w, t.w.w);
           return temp;
      }
    }
}
