using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Skills.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  // This listener only for resetting experience
  public class ExperienceWatcher: IEventListener<UnturnedPlayerExperienceUpdatedEvent> {
    private static HashSet<ulong> _disableWatch = new HashSet<ulong>();

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerExperienceUpdatedEvent @event) {
      if(!_disableWatch.Contains(@event.Player.SteamId.m_SteamID)) {
        @event.Player.Player.skills.ServerSetExperience(0);
      }
    }

    public static void EnableWatch(ulong playerId) {
      _disableWatch.Remove(playerId);
    }

    public static void DisableWatch(ulong playerId) {
      _disableWatch.Add(playerId);
    }
  }
}
