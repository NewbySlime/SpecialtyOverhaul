using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMod.Unturned.Building.Events;
using OpenMod.Unturned.Players;
using Steamworks;
using OpenMod.Unturned.Users;
using NuGet.Protocol.Plugins;
using SDG.Unturned;
using OpenMod.API.Eventing;
using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.Unturned.Users.Events;
using OpenMod.Unturned.Players.Movement.Events;

namespace Nekos.SpecialtyPlugin.Mechanic.Autoload{

  /// <summary>
  /// The class purpose is to "lookout" the class, keep track of the classes for each player, instantiating or deleting and such. But it will not keep track of the status of the class (the members of it).
  /// </summary>
  public class PlayerLookoutContext: IPluginAutoload,
    IEventListener<PluginUserRecheckEvent>,
    IEventListener<UnturnedUserConnectedEvent>,
    IEventListener<UnturnedUserDisconnectedEvent>{

    private static Dictionary<CSteamID, PlayerContext> _playerContexts = new();


    private static void _addPlayersContext(UnturnedPlayer player) {
      _playerContexts[player.SteamId] = new(player);
    }


    protected override void Instantiate(SpecialtyOverhaul plugin) {
      _playerContexts.Clear();
    }

    protected override void Destroy(SpecialtyOverhaul plugin) {
      _playerContexts.Clear();
    }


    public async Task HandleEventAsync(Object? obj, PluginUserRecheckEvent @event) {
      if(@event.param.HasValue) 
        _addPlayersContext(@event.param.Value.user.Player);
    }

    public async Task HandleEventAsync(Object? obj, UnturnedUserConnectedEvent @event) {
      _addPlayersContext(@event.User.Player);
    }

    public async Task HandleEventAsync(Object? obj, UnturnedUserDisconnectedEvent @event) {
      _playerContexts.Remove(@event.User.SteamId);
    }



    public static PlayerContext? GetPlayerContext(UnturnedPlayer player) {
      if(_playerContexts.TryGetValue(player.SteamId, out PlayerContext val))
        return val;

      return null;
    }
  }
}
