using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Binding {
  public class Bindable: IBindable {
    private HashSet<Binder> _bindings = new HashSet<Binder>();


    protected virtual void OnNotBinded() {}
    

    public void Bind(Binder bind) {
      _bindings.Add(bind);
    }

    public void Unbind(Binder bind) {
      if(_bindings.Remove(bind) && _bindings.Count == 0)
        OnNotBinded();
    }
  }
}
