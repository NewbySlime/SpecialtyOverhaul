using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Misc {
  public static class EnumHelper {

    public static void IterateEnum<EnumType>(Action<EnumType> action) {
      EnumType[]? _enumvalues = typeof(EnumType).GetEnumValues() as EnumType[];

      if(_enumvalues != null)
        foreach(EnumType e in _enumvalues)
          action.Invoke(e);
    }
  }
}
