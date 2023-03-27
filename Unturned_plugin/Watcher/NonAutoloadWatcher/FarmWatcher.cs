using Nekos.SpecialtyPlugin.Mechanic.Skill;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher{
  public partial class NonAutoloadWatcher {
    public class FarmWatcher {
      private void _onHarvesting(InteractableFarm harvestable, SteamPlayer player, ref bool shouldAllow) {
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        if(plugin != null) {
          plugin.PrintToOutput("player harvesting");
          plugin.SkillUpdaterInstance.SumSkillExp(player.playerID.steamID, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.AGRICULTURE_ONFARM), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.AGRICULTURE);
        }
      }

      public FarmWatcher() {
        InteractableFarm.OnHarvestRequested_Global += _onHarvesting;
      }

      ~FarmWatcher() {
        InteractableFarm.OnHarvestRequested_Global -= _onHarvesting;
      }
    }
  }
}
