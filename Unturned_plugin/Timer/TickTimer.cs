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
    private Task? _tickTask;
    private bool _keepTick = false;
    private float _tickInterval;

    /// <summary>
    /// Event that invoked when a tick happens
    /// </summary>
    public event EventHandler? OnTick;

    /// <summary>
    /// A function that handles each ticks <br/>
    /// NOTE: should run on seperate Task
    /// </summary>
    private async void _tickHandler() {
      while(_keepTick) {
        Task _timerTask = Task.Delay((int)(_tickInterval * 1000));
        OnTick?.Invoke(this, EventArgs.Empty);
        await _timerTask;
      }
    }

    /// <param name="tickIntervalS">Interval time in seconds</param>
    public TickTimer(float tickIntervalS) {
      _tickInterval = tickIntervalS;
    }

    ~TickTimer() {
      StopTick();
    }

    /// <summary>
    /// Changing the interval timing
    /// </summary>
    /// <param name="tickIntervalS">Interval time in seconds</param>
    public void ChangeTickInterval(float tickIntervalS) {
      _tickInterval = tickIntervalS;
    }

    public void StartTick() {
      _keepTick = true;
      _tickTask = Task.Run(_tickHandler);
    }

    public void StopTick() {
      _keepTick = false;
      if(_tickTask != null) {
        _tickTask.Wait();
        _tickTask.Dispose();
        _tickTask = null;
      }
    }
  }
}
