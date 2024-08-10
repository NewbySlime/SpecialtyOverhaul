using System;
using OpenMod.Unturned.Users;

namespace Nekos.SpecialtyPlugin.CustomEvent
{
    public class PluginUserRecheckEvent: IPluginEvent {
    public struct Param {
      public UnturnedUser user;
    }

    public Param? param;


    protected static void _userRecheck(Object? obj, UnturnedUser user) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginUserRecheckEvent @event = new PluginUserRecheckEvent {
          param = new Param {
            user = user
          }
        };

        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
      }
    }


    protected override void Subscribe() {
      SpecialtyOverhaul.OnUserRecheck -= _userRecheck;
      SpecialtyOverhaul.OnUserRecheck += _userRecheck;
    }

    protected override void Unsubscribe() {
      SpecialtyOverhaul.OnUserRecheck -= _userRecheck;
    }

    public PluginUserRecheckEvent() {}
  }
}
