using Cysharp.Threading.Tasks;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Same as GetLevel command, but with user name/id for when an admin wants to know a player level(s)
  /// </summary>
  [Command("GetPlayerLevel")]
  [CommandDescription("Get another player level status (Admin only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public class GetPlayerLevelCommand: UnturnedCommand {

    private readonly SpecialtyOverhaul _plugin;

    public GetPlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      if(Context.Parameters.Length > 0) {
        try {
          string _param = await Context.Parameters.GetAsync<string>(0);
          int _idx = _param.IndexOf('/');
          string userstr = _param.Substring(0, _idx);

          UnturnedUser? user = await _plugin.UnturnedUserProviderInstance.FindUserAsync("", userstr, UserSearchMode.FindByNameOrId) as UnturnedUser;
          if(user != null)
            await CommandParameterParser.ParseToMessage_GetLevel(_plugin, Context, user, _param.Substring(_idx + 1));
          else
            await Context.Actor.PrintMessageAsync("User name/id not found.", System.Drawing.Color.Red);
        }
        catch(Exception e) {
          _plugin.PrintToError("Something went wrong when calling command \"GetPlayerLevel\"");
          _plugin.PrintToError(e.ToString());
        }
      }
      else
        await Context.Actor.PrintMessageAsync("Parameter cannot be empty.", System.Drawing.Color.Red);
    }
  }
}
