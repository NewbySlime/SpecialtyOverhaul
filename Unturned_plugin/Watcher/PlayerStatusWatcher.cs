using System;
using System.Threading.Tasks;
using OpenMod.Unturned.Players;
using Cysharp.Threading.Tasks;
using SDG.Unturned;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using System.Collections.Generic;
using OpenMod.Unturned.Players.Stats.Events;
using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.Unturned.Users.Events;

using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Plugins.Events;
using OpenMod.Core.Eventing;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class PlayerStatusWatcher:
    IEventListener<UnturnedPlayerOxygenUpdatedEvent>,
    IEventListener<UnturnedPlayerStaminaUpdatedEvent>,
    IEventListener<UnturnedPlayerFoodUpdatedEvent>,
    IEventListener<UnturnedPlayerWaterUpdatedEvent>,
    IEventListener<UnturnedPlayerVirusUpdatedEvent>,
    IEventListener<UnturnedPlayerHealthUpdatedEvent>,
    IEventListener<UnturnedPlayerTemperatureUpdatedEvent>,
    IEventListener<UnturnedPlayerBleedingUpdatedEvent>,
    IEventListener<UnturnedPlayerBrokenUpdatedEvent>,
    IEventListener<UnturnedPlayerFallDamagingEvent>,

    IEventListener<UnturnedPlayerDeathEvent>,

    IEventListener<UnturnedUserDisconnectedEvent>,
    
    IEventListener<PluginUnloadedEvent>

    IEventListener<PluginUserRecheckEvent>,
    IEventListener<UnturnedPlayerSpawnedEvent> {

    private class Byteref {
      public byte data;
    }

    private class ValueMaintainer {
      public UnturnedPlayer player;
      public float value;
      public SkillConfig.ESkillEvent eSkillEvent_persec;
      public EPlayerSpeciality spec;
      public byte idx;

      public ValueMaintainer(UnturnedPlayer player, float value, SkillConfig.ESkillEvent eSkillEvent_persec, EPlayerSpeciality spec, byte idx) {
        this.player = player;
        this.value = value;
        this.eSkillEvent_persec = eSkillEvent_persec;
        this.spec = spec;
        this.idx = idx;
      }
    }

    private readonly static byte _foodflag = 1;
    private readonly static byte _thirstflag = 2;

    private static Dictionary<ulong, Byteref> _survivalValueMaintain = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _vitalityValueMaintain = new Dictionary<ulong, Byteref>();

    private static Dictionary<ulong, Byteref> _lastHealth = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastStamina = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastOxygen = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastVirus = new Dictionary<ulong, Byteref>();

    private static HashSet<ulong> _isAided = new HashSet<ulong>();

    private void _addPlayerToWatch(UnturnedPlayer player) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;

      if(plugin != null) {
        // the code below should be for events that uses ticks
        Task.Run(async () => {
          await HandleEventAsync(null, new UnturnedPlayerOxygenUpdatedEvent(player, player.PlayerLife.oxygen));
          await HandleEventAsync(null, new UnturnedPlayerStaminaUpdatedEvent(player, player.PlayerLife.stamina));
          await HandleEventAsync(null, new UnturnedPlayerFoodUpdatedEvent(player, player.PlayerLife.food));
          await HandleEventAsync(null, new UnturnedPlayerWaterUpdatedEvent(player, player.PlayerLife.water));
          await HandleEventAsync(null, new UnturnedPlayerVirusUpdatedEvent(player, player.PlayerLife.virus));
          await HandleEventAsync(null, new UnturnedPlayerHealthUpdatedEvent(player, player.PlayerLife.health));
          await HandleEventAsync(null, new UnturnedPlayerTemperatureUpdatedEvent(player, player.PlayerLife.temperature));
        });
      }
    }

    private static void _stopTickWatch(UnturnedPlayer player) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
      }
    }

    private static void _onTick_valueMaintainer(Object obj, ref bool removeObj) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        ValueMaintainer? maintainer = obj as ValueMaintainer;
        if(maintainer != null) {
          maintainer.value += plugin.SkillConfigInstance.GetTickInterval() * plugin.SkillConfigInstance.GetEventUpdate(maintainer.eSkillEvent_persec);
          plugin.PrintToOutput(string.Format("val {0}", maintainer.value));
          int nval = (int)Math.Floor(maintainer.value);
          if(nval > 0) {
            maintainer.value -= nval;
            plugin.SkillUpdaterInstance.SumSkillExp(maintainer.player, nval, (byte)maintainer.spec, maintainer.idx);
          }
        }
        else
          removeObj = true;
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerOxygenUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        if(_lastOxygen.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref))
          await Task.Run(() => {
            int _delta = (int)@event.Oxygen - dataref.data;
            plugin.PrintToOutput(string.Format("delta {0}", _delta));
            if(_delta < 0) {
              // diving
              SkillConfig.ESkillEvent eSkillEvent = @event.Player.Player.stance.isBodyUnderwater ? SkillConfig.ESkillEvent.DIVING_OXYGEM_USE_IFSWIMMING : SkillConfig.ESkillEvent.DIVING_OXYGEN_USE;
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(eSkillEvent) * _delta * -1), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DIVING);
            }
            else if(_delta > 0) {
              // cardio
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.CARDIO_OXYGEN_REGEN) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.CARDIO);
            }

            dataref.data = @event.Oxygen;
          });
        else
          _lastOxygen[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerStaminaUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        if(_lastStamina.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref))
          await Task.Run(() => {
            int _delta = (int)@event.Stamina - dataref.data;

            if(_delta < 0) {
              _delta *= -1;

              // exercise
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.EXERCISE_STAMINA_USE) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.EXERCISE);

              if(@event.Player.Player.stance.stance == EPlayerStance.SPRINT) {
                plugin.PrintToOutput("sprinting");
                // parkour (sprint)
                plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.PARKOUR_STAMINA_USE_SPRINTING) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.PARKOUR);
              }
            }
            else if(_delta > 0) {
              // cardio
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.CARDIO_STAMINA_REGEN) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.CARDIO);
            }

            dataref.data = @event.Stamina;
          });
        else
          _lastStamina[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerFoodUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        Byteref dataref;
        // survival
        if(_survivalValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if(@event.Food < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SURVIVAL_MAINTAIN_HUNGER_BELOW))
            dataref.data |= _foodflag;
          else
            dataref.data &= (byte)~_foodflag;

          if(plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL)) {
            if(dataref.data == 0)
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
          }
          else if(dataref.data > 0) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.SURVIVAL_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);

            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL, _onTick_valueMaintainer, maintainer);
          }
        }
        else
          _survivalValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };

        // vitality
        if(_vitalityValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if(plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY)) {
            if(@event.Food <= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_HUNGER_ABOVE)) {
              dataref.data &= (byte)~_foodflag;
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
            }
          }
          else {
            if(@event.Food > plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_HUNGER_ABOVE)) {
              dataref.data |= _foodflag;
              if(dataref.data == (_foodflag | _thirstflag)) {
                ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.VITALITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);

                plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY, _onTick_valueMaintainer, maintainer);
              }
            }
            else
              dataref.data &= (byte)~_foodflag;
          }
        }
        else
          _vitalityValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerWaterUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        Byteref dataref;
        // survival
        if(_survivalValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if(@event.Water < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SURVIVAL_MAINTAIN_THIRST_BELOW))
            dataref.data |= _thirstflag;
          else
            dataref.data &= (byte)~_thirstflag;

          if(plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL)) {
            if(dataref.data == 0)
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
          }
          else if(dataref.data > 0) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.SURVIVAL_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);

            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL, _onTick_valueMaintainer, maintainer);
          }
        }
        else
          _survivalValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };

        // vitality
        if(_vitalityValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if(plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY)) {
            if(@event.Water <= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_THIRST_ABOVE)) {
              dataref.data &= (byte)~_thirstflag;
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
            }
          }
          else {
            if(@event.Water > plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_THIRST_ABOVE)) {
              dataref.data |= _thirstflag;
              if(dataref.data == (_foodflag | _thirstflag)) {
                ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.VITALITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);

                plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY, _onTick_valueMaintainer, maintainer);
              }
              else
                dataref.data &= (byte)~_thirstflag;
            }
          }
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerVirusUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        if(_lastVirus.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref)) {
          int _delta = (int)@event.Virus - dataref.data;
          if(_delta < 0) {
            _delta *= -1;
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_VIRUS_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
          }
          else if(_delta > 0) {
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_VIRUS_INCREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
          }
        }
        else
          _lastVirus[@event.Player.SteamId.m_SteamID] = new Byteref { data = @event.Player.Player.life.virus };

        // immunity
        if(!plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY)) {
          if(@event.Virus < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_MAINTAIN_VIRUS_BELOW)) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.IMMUNITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY, _onTick_valueMaintainer, maintainer);
          }
        }
        else if(@event.Virus >= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_MAINTAIN_VIRUS_BELOW))
          plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerHealthUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        if(_lastHealth.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref)) {
          int _delta = (int)@event.Health - dataref.data;
          if(_delta < 0) {
            _delta *= -1;

            // toughness
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_HEALTH_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

            // strength
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_HEALTH_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
          }
          else if(_delta > 0) {
            // healing
            if(_isAided.Contains(@event.Player.SteamId.m_SteamID))
              _isAided.Remove(@event.Player.SteamId.m_SteamID);
            else {
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_HEALTH_MULT) * _delta), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.HEALING);
            }
          }

          dataref.data = @event.Health;
        }
        else
          _lastHealth[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerTemperatureUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.PrintToOutput("temperature update");
        if(plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED)) {
          switch(@event.Temperature) {
            case EPlayerTemperature.WARM:
            case EPlayerTemperature.BURNING:
            case EPlayerTemperature.NONE:
            case EPlayerTemperature.COVERED:
            case EPlayerTemperature.ACID:
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
              break;
          }
        }
        else {
          SkillConfig.ESkillEvent eSkillEvent = SkillConfig.ESkillEvent.__len;
          switch(@event.Temperature) {
            case EPlayerTemperature.COLD:
              eSkillEvent = SkillConfig.ESkillEvent.WARMBLOODED_ON_COLD_PERSEC_MULT;
              break;

            case EPlayerTemperature.FREEZING:
              eSkillEvent = SkillConfig.ESkillEvent.WARMBLOODED_ON_FREEZING_PERSEC_MULT;
              break;
          }

          // warmblooded
          if(eSkillEvent != SkillConfig.ESkillEvent.__len) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, eSkillEvent, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED, _onTick_valueMaintainer, maintainer);
          }
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerBleedingUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null && @event.IsBleeding) {
        // toughness
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_BLEEDING), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_BLEEDING), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerBrokenUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null && @event.IsBroken) {
        // toughness
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_FRACTURED), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_FRACTURED), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerFallDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_HEALTH_DECREASE_FALL_DAMAGE_MULT) * @event.DamageAmount), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    // this is to reset all the variables in this class
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDeathEvent @event) {
      _stopTickWatch(@event.Player);
    }

    public async Task HandleEventAsync(Object? obj, UnturnedUserDisconnectedEvent @event) {
      _stopTickWatch(@event.User.Player);
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerSpawnedEvent @event) {
      _addPlayerToWatch(@event.Player);
    }

    public async Task HandleEventAsync(Object? obj, PluginUserRecheckEvent @event) {
      SpecialtyOverhaul.Instance?.PrintToOutput("user rechecking");
      if(@event.param.HasValue)
        _addPlayerToWatch(@event.param.Value.user.Player);
      SpecialtyOverhaul.Instance?.PrintToOutput("done processing");
    }

    public async Task HandleEventAsync(Object? obj, PluginUnloadedEvent @event) {
      _survivalValueMaintain.Clear();
      _vitalityValueMaintain.Clear();
      _lastHealth.Clear();
      _lastStamina.Clear();
      _lastOxygen.Clear();
      _lastVirus.Clear();
      _isAided.Clear();
    }

    public static void AddIsAided(ulong player) {
      _isAided.Add(player);
    }
  }
}
