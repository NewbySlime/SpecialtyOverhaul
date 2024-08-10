using NuGet.Packaging.Signing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Binding {
  // TODO: this needs fixing, as this could be buggy when used for servers that last longer
  public class Binder {
    private static Mutex _binderCountMutex = new Mutex();
    private static ulong _binderCount = 0;


    private readonly ulong _id;

    public Binder() {
      _binderCountMutex.WaitOne();

      _id = _binderCount;
      _binderCount++;

      _binderCountMutex.ReleaseMutex();
    }

    public override int GetHashCode() {
      return _id.GetHashCode();
    }
  }
}
