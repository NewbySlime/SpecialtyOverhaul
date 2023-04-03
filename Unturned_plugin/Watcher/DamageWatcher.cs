using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Animals.Events;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Resources.Events;
using OpenMod.Unturned.Vehicles.Events;
using OpenMod.Unturned.Zombies.Events;
using OpenMod.Unturned.Zombies;
using SDG.Unturned;
using Steamworks;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class DamageWatcher:
    IEventListener<UnturnedZombieDamagingEvent>,
    IEventListener<UnturnedPlayerDamagingEvent>,
    IEventListener<UnturnedVehicleDamagingTireEvent>,
    IEventListener<UnturnedAnimalDamagingEvent>,
    IEventListener<UnturnedResourceDamagingEvent>
    {

    public static float CalculateDistExp(float base_exp, float dist, float min_dist, float div) {
      if(dist >= min_dist)
        return (dist - min_dist) / div * base_exp;

      return base_exp;
    }

    public async Task HandleEventAsync(Object? obj, UnturnedZombieDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        SkillUpdater updater = plugin.SkillUpdaterInstance;
        SkillConfig config = plugin.SkillConfigInstance;

        UnturnedPlayer? player = @event.Instigator;
        UnturnedZombie? zombie = @event.Zombie;
        if(player != null && zombie != null)
          await Task.Run(() => {
            bool _usemelee = false;
            if(player.Player.equipment.asset != null) {
              switch(player.Player.equipment.asset.type) {
                case EItemType.GUN: {
                  float dist = Vector3.Distance(player.Transform.Position, zombie.Transform.Position);
                  SkillConfig.ESkillEvent eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER_CRIT : SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER;

                  // sharpshooter
                  updater.SumSkillExp(player, CalculateDistExp(config.GetEventUpdate(eSkillEvent), dist, config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START), config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)), (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER);
                }
                break;

                case EItemType.MELEE: {
                  _usemelee = true;
                }
                break;
              }
            }
            else
              _usemelee = true;

            if(_usemelee) {
              plugin.PrintToOutput("checking melee");
              SkillConfig.ESkillEvent eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.OVERKILL_MELEE_ZOMBIE_CRIT : SkillConfig.ESkillEvent.OVERKILL_MELEE_ZOMBIE;

              float _expval = config.GetEventUpdate(eSkillEvent);
              if((int)config.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // overkill
              updater.SumSkillExp(player, _expval, (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.OVERKILL);
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        SkillUpdater updater = plugin.SkillUpdaterInstance;
        SkillConfig config = plugin.SkillConfigInstance;
        UnturnedPlayer? player = plugin.UnturnedUserProviderInstance.GetUser(@event.Killer)?.Player;

        if(player != null)
          await Task.Run(() => {
            switch(@event.Cause) {
              case EDeathCause.GUN: {
                float dist = Vector3.Distance(player.Transform.Position, @event.Player.Transform.Position);
                SkillConfig.ESkillEvent eSkillEvent;
                if(player.PlayerLife.isDead)
                  eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER_CRIT : SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER;
                else
                  eSkillEvent = SkillConfig.ESkillEvent.SHARPSHOOTER_PLAYER_KILLED_GUN;

                // sharpshooter
                updater.SumSkillExp(player, CalculateDistExp(config.GetEventUpdate(eSkillEvent), dist, config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START), config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)), (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER);
              }
              break;

              case EDeathCause.PUNCH:
              case EDeathCause.MELEE: {
                SkillConfig.ESkillEvent eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.OVERKILL_MELEE_PLAYER_CRIT : SkillConfig.ESkillEvent.OVERKILL_MELEE_PLAYER;

                float _expval = config.GetEventUpdate(eSkillEvent);
                if((int)config.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                  _expval *= @event.DamageAmount;

                // overkill
                updater.SumSkillExp(player, _expval, (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.OVERKILL);
              }
              break;
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedAnimalDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;
        UnturnedPlayer? player = plugin.UnturnedUserProviderInstance.GetUser(@event.Instigator)?.Player;

        if(player != null)
          await Task.Run(() => {
            bool _usemelee = false;
            if(player.Player.equipment.asset != null) {
              switch(player.Player.equipment.asset.type) {
                case EItemType.GUN: {
                  if(@event.Animal.IsAlive) {
                    // outdoors
                    skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_ANIMAL_KILLED), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.OUTDOORS);
                  }

                  // sharpshooter
                  SkillConfig.ESkillEvent eSkillEvent = @event.Animal.IsAlive ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_ANIMAL : SkillConfig.ESkillEvent.SHARPSHOOTER_ANIMAL_KILLED_GUN;
                  skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(eSkillEvent), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.SHARPSHOOTER);
                }
                break;

                case EItemType.MELEE: {
                  _usemelee = true;
                }
                break;
              }
            }
            else
              _usemelee = true;

            if(_usemelee) {
              SkillConfig.ESkillEvent eSkillEvent = SkillConfig.ESkillEvent.OVERKILL_MELEE_ANIMAL;
              float _expval = skillConfig.GetEventUpdate(eSkillEvent);
              if((int)skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // overkill
              skillUpdater.SumSkillExp(player, _expval, (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.OVERKILL);
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedVehicleDamagingTireEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

        UnturnedPlayer? player = null;
        CSteamID? steamID = @event.Instigator;
        if(steamID != null)
          player = plugin.UnturnedUserProviderInstance.GetUser(steamID.Value)?.Player;

        if(player != null && player.Player.equipment.asset != null && player.Player.equipment.asset.type == EItemType.GUN)
          await Task.Run(() => {
            // sharpshooter
            skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_TIRE), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.SHARPSHOOTER);
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedResourceDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

        if(@event.Instigator != null)
          await Task.Run(() => {
            if(@event.Instigator.Player.equipment.asset == null || @event.Instigator.Player.equipment.asset.type == EItemType.MELEE) {
              float _expval = skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_RESOURCE_DAMAGING);
              if((int)skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_RESOURCE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // outdoors
              skillUpdater.SumSkillExp(@event.Instigator, _expval, (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.OUTDOORS);
            }
          });
      }
    }
  }
}
