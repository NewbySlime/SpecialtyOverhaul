using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.CustomEvent;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMod.API.Eventing;

namespace Nekos.SpecialtyPlugin.Watcher{
  public class FarmWatcher: IEventListener<PluginOnFarmEvent>{
    public async Task HandleEventAsync(Object? obj, PluginOnFarmEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null && @event.param.HasValue) {
        plugin.PrintToOutput("player harvesting");
        plugin.SkillUpdaterInstance.SumSkillExp(@event.param.Value.player.playerID.steamID, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.AGRICULTURE_ONFARM), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.AGRICULTURE);
      }
    }
  }
}
