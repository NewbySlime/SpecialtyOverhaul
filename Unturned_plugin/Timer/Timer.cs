using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Timer {
  public class Timer: System.Timers.Timer {
    public event EventHandler<Object?>? OnFinished;

    private Object? _eventParameter = null;
    public Object EventParameter {
      set {
        _eventParameter = value;
      }
    }


    private void _onElapsed(Object? sender, System.EventArgs args) {
      OnFinished?.Invoke(this, _eventParameter);
    }

    public Timer() {
      Elapsed += _onElapsed;
      Enabled = true;
    }

    ~Timer() {
      Elapsed -= _onElapsed;
    }
  }
}
