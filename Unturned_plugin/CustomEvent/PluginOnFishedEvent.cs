using Nekos.SpecialtyPlugin.Watcher;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class PluginOnFishedEvent: IPluginEvent{

    public struct Param {
      public UnturnedPlayer player;
    }


    public Param? param;

    protected static void _onFished(Object? obj, UnturnedPlayer player) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginOnFishedEvent @event = new PluginOnFishedEvent {
          param = new Param{
            player = player
          }
        };

        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
      }
    }


    protected override void Subscribe() {
      InventoryWatcher.OnPlayerFished -= _onFished;
      InventoryWatcher.OnPlayerFished += _onFished;
    }

    protected override void Unsubscribe() {
      InventoryWatcher.OnPlayerFished -= _onFished;
    }
  }
}
