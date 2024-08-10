#if DEBUG
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  public partial class DebugMode_LetDieCommand {
    private class StaticEventListener: IEventListener<UnturnedPlayerDyingEvent> {
      public async Task HandleEventAsync(Object? obj, UnturnedPlayerDyingEvent @event) {
        if(_constantReviveList.Contains(@event.Player.SteamId)) {
          @event.IsCancelled = true;
          
          // don't use PlayerLife.ReceiveLifeStats, a bit buggy
          @event.Player.PlayerLife.askHeal(100, true, true);
          @event.Player.PlayerLife.askEat(100);
          @event.Player.PlayerLife.askDrink(100);
          @event.Player.PlayerLife.askDisinfect(100);
        }
      }
    }
  }
}
#endif