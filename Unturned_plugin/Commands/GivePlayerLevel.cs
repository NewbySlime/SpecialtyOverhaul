using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Command for when an admin want to give level(s) to a player
  /// </summary>
  [Command("GivePlayerLevel")]
  [CommandDescription("Give player level, can set all skills in a specialty (Admin Only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>/<number>")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public class GivePlayerLevelCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GivePlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        await CommandParameterParser.ParseParameter1(_plugin, Context, (UnturnedUser user, EPlayerSpeciality spec, byte skillidx, int level) => { _plugin.SkillUpdaterInstance.GivePlayerLevel(user.Player, (byte)spec, skillidx, level); });
      }
      catch(Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GivePlayerLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }
}
