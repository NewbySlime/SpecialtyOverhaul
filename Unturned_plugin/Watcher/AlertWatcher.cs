using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Animals.Events;
using OpenMod.Unturned.Zombies.Events;
using SDG.Unturned;
using System;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class AlertWatcher: IEventListener<UnturnedZombieAlertingPlayerEvent>, IEventListener<UnturnedAnimalAttackingPlayerEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedZombieAlertingPlayerEvent @event) {
      if(@event.Player == null)
        return;

      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null)
        await Task.Run(() => {
          float _dist = System.Numerics.Vector3.Distance(@event.Player.Transform.Position, @event.Zombie.Transform.Position);
          float _maxdist = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_MAX_DIST);
          if(_dist < _maxdist) {
            float _div = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_DIST_DIV);
            // sneakybeaky
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (_maxdist - _dist) / _div, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
          }
        });
    }

    public async Task HandleEventAsync(object? obj, UnturnedAnimalAttackingPlayerEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        float _dist = System.Numerics.Vector3.Distance(@event.Player.Transform.Position, @event.Animal.Transform.Position);
        float _maxdist = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_MAX_DIST);
        if(_dist < _maxdist) {
          float _div = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_DIST_DIV);
          // sneakybeaky
          plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (_maxdist - _dist) / _div, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
        }
      }
    }
  }
}
