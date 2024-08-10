using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Timer {
  /// <summary>
  /// This class use looped timer to generate ticks, or in another term, always updating for each interval time
  /// </summary>
  public class TickTimer {
    private System.Timers.Timer _timer;

    /// <summary>
    /// Event that invoked when a tick happens
    /// </summary>
    public event EventHandler? OnTick;

    /// <summary>
    /// A function that handles each ticks <br/>
    /// NOTE: should run on seperate Task
    /// </summary>
    private void _tickHandler(object source, EventArgs e) {
      OnTick?.Invoke(this, e);
    }

    /// <param name="tickIntervalS">Interval time in seconds</param>
    public TickTimer(float tickIntervalS) {
      _timer = new(tickIntervalS*1000);
      _timer.AutoReset = true;
      _timer.Elapsed += _tickHandler;
    }

    ~TickTimer() {
      StopTick();
    }

    /// <summary>
    /// Changing the interval timing
    /// </summary>
    /// <param name="tickIntervalS">Interval time in seconds</param>
    public void ChangeTickInterval(float tickIntervalS) {
      _timer.Stop();
      _timer.Interval = tickIntervalS * 1000;
      _timer.Start();
    }

    public void StartTick() {
      _timer.Start();
    }

    public void StopTick() {
      _timer.Stop();
    }
  }
}
