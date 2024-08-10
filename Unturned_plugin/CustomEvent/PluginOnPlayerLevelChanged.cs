using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Unturned.Players;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.CustomEvent{
  public class PluginOnPlayerLevelChanged: IPluginEvent {
    public class Param {
      public UnturnedPlayer player;
      public byte lastLevel;
      public byte newLevel;
      public (EPlayerSpeciality, byte) skill;

      public Param(UnturnedPlayer player, byte lastLevel, byte newLevel, (EPlayerSpeciality, byte) skill) {
        this.player = player;
        this.lastLevel = lastLevel;
        this.newLevel = newLevel;
        this.skill = skill;
      }
    }


    private Param? _param;
    public Param? param {
      get {
        return _param;
      }
    }

    
    public static void Invoke(Object? obj, Param levelChanged) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginOnPlayerLevelChanged @event = new PluginOnPlayerLevelChanged { _param = levelChanged };
        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
      }
    }
  }
}
