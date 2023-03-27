using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using System;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class ConnectionWatcher: IEventListener<UnturnedPlayerConnectedEvent>, IEventListener<UnturnedPlayerDisconnectedEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerConnectedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.CallEvent_OnPlayerConnected(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDisconnectedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.CallEvent_OnPlayerDisconnected(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }
  }
}
