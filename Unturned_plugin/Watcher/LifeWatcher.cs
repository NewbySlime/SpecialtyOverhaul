using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using System;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class LifeWatcher: IEventListener<UnturnedPlayerSpawnedEvent>, IEventListener<UnturnedPlayerRevivedEvent>, IEventListener<UnturnedPlayerDeathEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerSpawnedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.CallEvent_OnPlayerRespawned(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerRevivedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.CallEvent_OnPlayerRevived(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDeathEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.CallEvent_OnPlayerDied(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }
  }
}
