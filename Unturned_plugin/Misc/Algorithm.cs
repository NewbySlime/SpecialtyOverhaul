using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Misc {
  public static class Algorithm {
    /// <summary>
    /// Grabbed from <see cref="UnturnedUserProvider"/>
    /// </summary>
    /// <param name="name">Current name</param>
    /// <param name="searchName">The name that going to be searched</param>
    /// <param name="currentConfidence">Current confidence in between strings</param>
    /// <returns>The new confidence in this searchName</returns>
    public static int NameConfidence(string name, string searchName, int currentConfidence = -1) {
      switch(currentConfidence) {
        case 2:
          if(name.Equals(searchName, StringComparison.OrdinalIgnoreCase))
            return 3;
          goto case 1;

        case 1:
          if(name.StartsWith(searchName, StringComparison.OrdinalIgnoreCase))
            return 2;
          goto case 0;

        case 0:
          if(name.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) != -1)
            return 1;
          break;

        default:
          goto case 2;
      }

      return -1;
    }
  }
}
