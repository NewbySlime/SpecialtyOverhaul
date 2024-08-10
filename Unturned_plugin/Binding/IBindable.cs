using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Binding {
  public interface IBindable {
    public void Bind(Binder binder);
    public void Unbind(Binder binder);
  }
}
