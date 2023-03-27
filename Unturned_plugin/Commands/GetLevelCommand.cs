using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Command when a player wants to know their level(s)
  /// </summary>
  [Command("GetLevel")]
  [CommandDescription("To get specialty level data")]
  [CommandSyntax("<specialty>/<skill>")]
  [CommandActor(typeof(UnturnedUser))]
  public class GetLevelCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GetLevelCommand(SpecialtyOverhaul plugin, IServiceProvider serviceProvider) : base(serviceProvider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        if(Context.Parameters.Length > 0) {
          UnturnedUser? user = _plugin.UnturnedUserProviderInstance.GetUser(new CSteamID(ulong.Parse(Context.Actor.Id)));
          if(user == null)
            throw new Exception(string.Format("Id of {0} is invalid.", Context.Actor.Id));

          await CommandParameterParser.ParseToMessage_GetLevel(_plugin, Context, user, await Context.Parameters.GetAsync<string>(0));
        }
        else
          await CommandParameterParser.Print_OnNoParam(Context);
      }
      catch(Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GetLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }
}
