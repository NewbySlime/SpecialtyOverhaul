using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Skills.Events;
using System;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  // This listener only for resetting experience
  public class ExperienceWatcher: IEventListener<UnturnedPlayerExperienceUpdatedEvent> {
    public ExperienceWatcher() {
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerExperienceUpdatedEvent @event) {
      @event.Player.Player.skills.ServerSetExperience(0);
    }
  }
}
