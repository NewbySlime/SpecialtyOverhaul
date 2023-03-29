using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Animals.Events;
using OpenMod.Unturned.Zombies.Events;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Users;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using SDG.Unturned;
using OpenMod.Unturned.Players.Life.Events;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class DyingWatcher:
    IEventListener<UnturnedPlayerDyingEvent>,
    IEventListener<UnturnedAnimalDyingEvent>,
    IEventListener<UnturnedZombieDyingEvent>
    {

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDyingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      UnturnedUser? killer = plugin?.UnturnedUserProviderInstance.GetUser(@event.Killer);

      if(plugin != null && killer != null) {
        bool _usemelee = false;
        if(killer.Player.Player.equipment.asset != null) {
          switch(killer.Player.Player.equipment.asset.type) {
            case EItemType.GUN: {
              plugin.SkillUpdaterInstance.SumSkillExp(
                killer.Player,
                DamageWatcher.CalculateDistExp(
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_PLAYER_KILLED_GUN),
                  Vector3.Distance(killer.Player.Transform.Position, @event.Player.Transform.Position),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)
                ),
                (byte)EPlayerSpeciality.OFFENSE,
                (byte)EPlayerOffense.SHARPSHOOTER
              );
            }
            break;

            case EItemType.MELEE:
              _usemelee = true;
              break;
          }
        }
        else
          _usemelee = true;


        if(_usemelee) {
          plugin.SkillUpdaterInstance.SumSkillExp(killer.Player, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_PLAYER_KILLED_MELEE), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.OVERKILL);
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedAnimalDyingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      UnturnedUser? killer = plugin?.UnturnedUserProviderInstance.GetUser(@event.Instigator);

      if(plugin != null && killer != null) {
        bool _usemelee = false;
        if(killer.Player.Player.equipment.asset != null) {
          switch(killer.Player.Player.equipment.asset.type) {
            case EItemType.GUN: {
              plugin.SkillUpdaterInstance.SumSkillExp(
                killer.Player,
                DamageWatcher.CalculateDistExp(
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_ANIMAL_KILLED_GUN),
                  Vector3.Distance(killer.Player.Transform.Position, @event.Animal.Transform.Position),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)
                ),
                (byte)EPlayerSpeciality.OFFENSE,
                (byte)EPlayerOffense.SHARPSHOOTER
              );
            }
            break;

            case EItemType.MELEE:
              _usemelee = true;
              break;
          }
        }
        else
          _usemelee = true;


        if(_usemelee) {
          plugin.SkillUpdaterInstance.SumSkillExp(killer.Player, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_ANIMAL_KILLED_MELEE), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.OVERKILL);

          if(@event.Animal.Animal.checkAlert(killer.Player.Player)) {
            float _value = Vector3.Distance(killer.Player.Transform.Position, @event.Animal.Transform.Position);
            if(AlertWatcher.CalculateSneakybeaky(plugin.SkillConfigInstance, SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_MAX_DIST, SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_DIST_DIV, ref _value)) {
              plugin.SkillUpdaterInstance.SumSkillExp(killer.Player, _value, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
            }
          }
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedZombieDyingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;

      if(plugin != null && @event.Instigator != null) {
        bool _usemelee = false;
        if(@event.Instigator.Player.equipment.asset != null) {
          switch(@event.Instigator.Player.equipment.asset.type) {
            case EItemType.GUN: {
              plugin.SkillUpdaterInstance.SumSkillExp(
                @event.Instigator,
                DamageWatcher.CalculateDistExp(
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_ZOMBIE_KILLED_GUN),
                  Vector3.Distance(@event.Instigator.Transform.Position, @event.Zombie.Transform.Position),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START),
                  plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)
                ),
                (byte)EPlayerSpeciality.OFFENSE,
                (byte)EPlayerOffense.SHARPSHOOTER
              );
            }
            break;

            case EItemType.MELEE:
              _usemelee = true;
              break;
          }
        }
        else
          _usemelee = true;

        if(_usemelee) {
          plugin.SkillUpdaterInstance.SumSkillExp(@event.Instigator, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_ZOMBIE_KILLED_MELEE), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.OVERKILL);

          if(@event.Zombie.Zombie.checkAlert(@event.Instigator.Player)) {
            float _value = Vector3.Distance(@event.Instigator.Transform.Position, @event.Zombie.Transform.Position);
            if(AlertWatcher.CalculateSneakybeaky(plugin.SkillConfigInstance, SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_MAX_DIST, SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_DIST_DIV, ref _value)) {
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Instigator, _value, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
            }
          }
        }
      }
    }
  }
}
