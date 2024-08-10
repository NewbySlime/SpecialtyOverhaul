using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Misc {
  public static class ArrayHelper {
    /// <summary>
    /// Initiating a class of array, using a callback to create array based on the index is currently on
    /// </summary>
    /// <typeparam name="T">Generic type of the class/object</typeparam>
    /// <param name="len">The lenght of array</param>
    /// <param name="classCreate">Callback to constructing class</param>
    /// <returns>Array of the generic class</returns>
    public static T[] InitArrayTClass<T>(int len, Func<int, T> classCreate) {
      T[] res = new T[len];
      for(int i = 0; i < len; i++)
        res[i] = classCreate.Invoke(i);

      return res;
    }
  }
}
