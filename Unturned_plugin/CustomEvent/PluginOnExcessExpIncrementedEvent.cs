using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class PluginOnExcessExpIncrementedEvent : IPluginEvent {
    public class Param {
      public readonly UnturnedUser User;
      public readonly int Excess;
      public readonly EPlayerSpeciality Spec;
      public readonly byte Skillidx;


      public Param(UnturnedUser user, int excess, EPlayerSpeciality spec, byte skillidx) {
        User = user;
        Excess = excess;
        Spec = spec;
        Skillidx = skillidx;
      }
    }


    private Param? _param;
    public Param? param {
      get {
        return _param;
      }
    }


    public static void Invoke(Object? obj, Param excessChanged) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginOnExcessExpIncrementedEvent @event = new() { _param = excessChanged };
        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
      }
    }
  }
}
