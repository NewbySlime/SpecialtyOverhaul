using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Error.Helper {
  public class NestedParameterData {
    private readonly string _splitter;
    private string _currentNest;

    public NestedParameterData(string currentNest, string splitter = ".") {
      _currentNest = currentNest;
      _splitter = splitter;
    }


    public override string ToString() {
      return _currentNest;
    }

    public string ToString(string param) {
      return string.Format("(in: {0}), {1}", _currentNest, param);
    }

    public NestedParameterData Nest(string newParam) {
      return new NestedParameterData(string.Format("{0}{1}{2}", _currentNest, _splitter, newParam), _splitter);
    }
  }
}
