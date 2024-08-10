using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Misc {
  public static class IndexerHelper {
    /// <summary>
    /// Converting enum to lower-cased indexer
    /// </summary>
    /// <typeparam name="T">Generic type of enum</typeparam>
    /// <param name="len">Enum count</param>
    /// <param name="append">Dictionary to append from before</param>
    /// <returns>An indexer dictionary</returns>
    public static Dictionary<string, T> CreateIndexerByEnum<T>(HashSet<T>? exclude = null, Dictionary<string, T>? append = null) {
      Dictionary<string, T> _res;
      if(append != null)
        _res = new Dictionary<string, T>(append);
      else
        _res = new Dictionary<string, T>();

      T[]? _enumvalues = typeof(T).GetEnumValues() as T[];
      if(_enumvalues != null)
        for(int i = 0; i < _enumvalues.Length; i++)
          if(exclude == null || !exclude.Contains(_enumvalues[i]))
            _res[typeof(T).GetEnumName(_enumvalues[i]).ToLower()] = _enumvalues[i];

      return _res;
    }

    public static Dictionary<string, byte> CreateIndexerByEnum_ParseToByte<enumType>(HashSet<enumType>? exclude = null, Dictionary<string, enumType>? append = null) {
      var _indexer = CreateIndexerByEnum(exclude, append);

      Dictionary<string, byte> _res = new Dictionary<string, byte>();
      foreach(var item in _indexer)
        _res[item.Key] = (byte)Convert.ToUInt32(item.Value);

      return _res;
    }

    public static string GetIndexerEnumName<enumType>(enumType @enum) {
      return typeof(enumType).GetEnumName(@enum).ToLower();
    }
  }
}
