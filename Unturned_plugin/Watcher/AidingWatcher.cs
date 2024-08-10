using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Useables.Events;
using SDG.Unturned;
using System;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class AidingWatcher: IEventListener<UnturnedPlayerPerformingAidEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerPerformingAidEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(
          plugin.UnturnedUserProviderInstance.GetUser(@event.Player.Player),
          (ISkillModifier editor) => {
            // to prevent leveling up by get aided by someone
            PlayerStatusWatcher.AddIsAided(@event.Target.SteamId.m_SteamID);

            // healing
            editor.ExpFractionIncrement(
              EPlayerSpeciality.SUPPORT,
              (byte)EPlayerSupport.HEALING, 
              plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_ON_AIDING)
            );
          }
        );
      }
    }
  }
}
