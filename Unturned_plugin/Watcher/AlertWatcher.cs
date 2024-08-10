using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Animals.Events;
using OpenMod.Unturned.Zombies.Events;
using SDG.Unturned;
using System;
using System.Threading.Tasks;
using System.Numerics;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class AlertWatcher: IEventListener<UnturnedZombieAlertingPlayerEvent>, IEventListener<UnturnedAnimalAttackingPlayerEvent> {
    /// <summary>
    /// Calculating exp for sneakybeaky, based on distance
    /// </summary>
    /// <param name="config">The current skill configuration</param>
    /// <param name="maxdist">Enum that represents maxdist in Sneakybeaky context</param>
    /// <param name="div">Enum that represents div in Sneakybeaky context</param>
    /// <param name="_val">Reference to distance in float. Upon return, it will be exp value</param>
    /// <returns>Returns true if skill should be summed</returns>
    public static bool CalculateSneakybeaky(SkillConfig config, SkillConfig.ESkillEvent maxdist, SkillConfig.ESkillEvent div, ref float _val) {
      float _maxdist = config.GetEventUpdate(maxdist);
      if(_val < _maxdist) {
        float _div = config.GetEventUpdate(div);
        _val = (_maxdist - _val) / _div;
        return true;
      }

      return false;
    }

    public async Task HandleEventAsync(Object? obj, UnturnedZombieAlertingPlayerEvent @event) {
      if(@event.Player == null)
        return;

      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null)
        await Task.Run(() => {
          float _value = Vector3.Distance(@event.Player.Transform.Position, @event.Zombie.Transform.Position);
          if(CalculateSneakybeaky(plugin.SkillConfigInstance, SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_MAX_DIST, SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_DIST_DIV, ref _value)) {
           plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(
              plugin.UnturnedUserProviderInstance.GetUser(@event.Player.Player),
              (ISkillModifier editor) => {
                // sneakybeaky
                editor.ExpFractionIncrement(EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY, _value);
              }
            );
          }
        });
    }

    public async Task HandleEventAsync(object? obj, UnturnedAnimalAttackingPlayerEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        float _value = Vector3.Distance(@event.Player.Transform.Position, @event.Animal.Transform.Position);
        if(CalculateSneakybeaky(plugin.SkillConfigInstance, SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_MAX_DIST, SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_DIST_DIV, ref _value)) {
          plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(
            plugin.UnturnedUserProviderInstance.GetUser(@event.Player.Player),
            (ISkillModifier editor) => {
              editor.ExpFractionIncrement(EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY, _value);
            }
           );
        }
      }
    }
  }
}
