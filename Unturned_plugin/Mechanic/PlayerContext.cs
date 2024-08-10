using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Movement.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMod.Unturned.Players;
using SDG.Unturned;
using OpenMod.Unturned.Players.Stats.Events;
using Nekos.SpecialtyPlugin.Misc;

namespace Nekos.SpecialtyPlugin.Mechanic {

  /// <summary>
  /// This class will keep track of player status (members of PlayerContext). 
  /// </summary>
  public class PlayerContext: IDisposable {
    private UnturnedPlayer player;
    private EEnvironment eEnvironment;
    public EEnvironment EEnvironment {
      get {
        return eEnvironment;
      }
    }

    private void _onRadiationChanged(bool isDead) {
      if(isDead)
        eEnvironment |= EEnvironment.DEADZONE;
      else
        eEnvironment &= ~EEnvironment.DEADZONE;
    }

    private void _onSafeChanged(bool isSafe) {
      if(isSafe)
        eEnvironment |= EEnvironment.SAFEZONE;
      else
        eEnvironment &= ~EEnvironment.SAFEZONE;
    }

    private void _onFullMoonChanged(bool isFull) {
      if(isFull)
        eEnvironment |= EEnvironment.FULLMOON;
      else
        eEnvironment &= ~EEnvironment.FULLMOON;
    }

    private void _onTimeChanged(bool isDay) {
      eEnvironment &= ~EEnvironment._time_flags;
      if(isDay)
        eEnvironment |= EEnvironment.DAYTIME;
      else
        eEnvironment |= EEnvironment.NIGHTTIME;
    }

    private void _onPlayerTemperatureChanged(EPlayerTemperature temp) {
      eEnvironment &= ~EEnvironment._temp_flags;
      switch(temp) {
        case EPlayerTemperature.BURNING:
          eEnvironment |= EEnvironment.BURNING;
          break;

        case EPlayerTemperature.COLD:
          eEnvironment |= EEnvironment.COLD;
          break;

        case EPlayerTemperature.FREEZING:
          eEnvironment |= EEnvironment.FREEZING;
          break;
      }
    }

    private void _onPlayerStanceChanged() {
      if(player.Player.stance.isBodyUnderwater)
        eEnvironment |= EEnvironment.UNDERWATER;
      else
        eEnvironment &= ~EEnvironment.UNDERWATER;
    }


    private void _subscribeToEvents() {
      player.Player.movement.onRadiationUpdated -= _onRadiationChanged;
      player.Player.movement.onRadiationUpdated += _onRadiationChanged;

      player.Player.movement.onSafetyUpdated -= _onSafeChanged;
      player.Player.movement.onSafetyUpdated += _onSafeChanged;

      player.Player.life.onTemperatureUpdated -= _onPlayerTemperatureChanged;
      player.Player.life.onTemperatureUpdated += _onPlayerTemperatureChanged;

      player.Player.stance.onStanceUpdated -= _onPlayerStanceChanged;
      player.Player.stance.onStanceUpdated += _onPlayerStanceChanged;

      LightingManager.onMoonUpdated -= _onFullMoonChanged;
      LightingManager.onMoonUpdated += _onFullMoonChanged;

      LightingManager.onDayNightUpdated -= _onTimeChanged;
      LightingManager.onDayNightUpdated += _onTimeChanged;
    }

    private void _unsubscribeToEvents() {
      player.Player.movement.onRadiationUpdated -= _onRadiationChanged;
      player.Player.movement.onSafetyUpdated -= _onSafeChanged;
      player.Player.life.onTemperatureUpdated -= _onPlayerTemperatureChanged;
      player.Player.stance.onStanceUpdated -= _onPlayerStanceChanged;

      LightingManager.onMoonUpdated -= _onFullMoonChanged;
      LightingManager.onDayNightUpdated -= _onTimeChanged;
    }


    private void _checkEnvironment() {
      _onPlayerTemperatureChanged(player.Player.life.temperature);
      _onRadiationChanged(player.Player.movement.isRadiated);
      _onSafeChanged(player.Player.movement.isSafe);
      _onPlayerStanceChanged();

      _onFullMoonChanged(LightingManager.isFullMoon);
      _onTimeChanged(LightingManager.isDaytime);
    }


    public PlayerContext(UnturnedPlayer player) {
      this.player = player;

      _checkEnvironment();
      _subscribeToEvents();
    }

    public void Dispose() {
      _unsubscribeToEvents();
    }


    public void IterateEnvironmentFlag(Action<EEnvironment> callback) {
      HashSet<EEnvironment> _exclude = new(){
        EEnvironment._temp_flags
      };

      EnumHelper.IterateEnum((EEnvironment _currentEnv) => {
        if(!_exclude.Contains(_currentEnv) && (int)(_currentEnv & eEnvironment) > 1) 
          callback.Invoke(_currentEnv);
      });
    }

    public bool HasFlag(EEnvironment env) {
      return (int)(env & eEnvironment) > 1;
    }
  }
}
