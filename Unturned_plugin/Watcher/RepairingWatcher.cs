using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Vehicles.Events;
using SDG.Unturned;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class RepairingWatcher: IEventListener<UnturnedVehicleRepairingEvent> {
    public async Task HandleEventAsync(object? obj, UnturnedVehicleRepairingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.PrintToOutput(string.Format("healing {0}", @event.PendingTotalHealing));
        UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(@event.Instigator);

        if(user != null) {
          // mechanic
          plugin.SkillUpdaterInstance.SumSkillExp(user.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.MECHANIC_REPAIR_HEALTH) * @event.PendingTotalHealing), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.MECHANIC);

          // engineer
          plugin.SkillUpdaterInstance.SumSkillExp(user.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.ENGINEER_REPAIR_HEALTH) * @event.PendingTotalHealing), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.ENGINEER);
        }
      }
    }
  }
}
