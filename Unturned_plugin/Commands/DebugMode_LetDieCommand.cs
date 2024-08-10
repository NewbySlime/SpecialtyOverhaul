#if DEBUG
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Core.Helpers;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("LetDie")]
  [CommandSyntax("/LetDie [flag]/[SteamID]")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public partial class DebugMode_LetDieCommand : UnturnedCommand {
    private static readonly Dictionary<string, bool> _bool_indexer = new() {
      {"true", true},
      {"false", false}
    };

    private readonly SpecialtyOverhaul plugin;
    private static HashSet<CSteamID> _constantReviveList = new HashSet<CSteamID>();


    public DebugMode_LetDieCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base(provider) {
      this.plugin = plugin;
    }



    protected async override UniTask OnExecuteAsync() {
      if(Context.Actor is UnturnedUser) {
        UnturnedUser? user = Context.Actor as UnturnedUser;
        if(user != null && !user.Player.SteamPlayer.isAdmin)
          return;
      }

      bool _search_flag = true, _search_steamid = true;

      bool _flag_found = false;
      bool _flag = true;
      bool _steamid_found = false;
      CSteamID _steamid = new CSteamID(0);

      if(Context.Parameters.Length > 0) {
        string[] _params = (await Context.Parameters.GetAsync<string>(0)).Split(param_splitter);

        for(int _param_index = 0; _param_index < _params.Length; _param_index++) {
          int _search_state = 0;

          int _current_confidence = 0;
          Object? _current_object = null;

          if(_search_flag) {
            var _res = SearchDictionaryConfidence(_bool_indexer, _params[_param_index], _current_confidence);
            if(_res.Item1 > _current_confidence) {
              _current_confidence = _res.Item1;
              _current_object = _res.Item2;
              _search_state = 1;
              _flag_found = true;
            }
          }

          if(_search_steamid) {
            if(ulong.TryParse(_params[_param_index], out ulong _val)) {
              CSteamID steamID = new(_val);
              var user = plugin.UnturnedUserProviderInstance.GetUser(steamID);
              if(user != null) {
                _current_confidence = int.MaxValue;
                _current_object = user;
                _search_state = 2;
                _steamid_found = true;
              }
            }
          }

          switch(_search_state) {
            case 0:
              throw new CommandWrongUsageException(Context);

            case 1: {
              bool? _currentflag = _current_object as bool?;
              if(_currentflag != null)
                _flag = _currentflag.Value;

              break;
            }

            case 2: {
              CSteamID? steamID = _current_object as CSteamID?;
              if(steamID != null)
                _steamid = steamID.Value;

              break;
            }
          }
        }
      }

      if(!_steamid_found) {
        if(Context.Actor is UnturnedUser) {
          UnturnedUser? _currentuser = Context.Actor as UnturnedUser;
          if(_currentuser != null)
            _steamid = _currentuser.SteamId;
        }
        else {
          plugin.PrintToError("Command Actor is not Unturned user.");
          throw new CommandWrongUsageException(Context);
        }
      }

      if(!_flag_found)
        _flag = _constantReviveList.Contains(_steamid);


      if(_flag) {
        _constantReviveList.Remove(_steamid);
        plugin.PrintToOutput(string.Format("PlayerID: {0} unset for constant reviving.", _steamid.m_SteamID));
        await Context.Actor.PrintMessageAsync("Constant reviving removed.", System.Drawing.Color.YellowGreen);
      }
      else {
        _constantReviveList.Add(_steamid);
        plugin.PrintToOutput(string.Format("PlayerID: {0} set for constant reviving.", _steamid.m_SteamID));
        await Context.Actor.PrintMessageAsync("Set for constant reviving.", System.Drawing.Color.Green);
      }
    }
  }
}
#endif