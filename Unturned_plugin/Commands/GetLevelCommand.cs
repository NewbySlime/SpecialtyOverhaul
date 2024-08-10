using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Serilog.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Command when a player wants to know their level(s)
  /// </summary>
  [Command("GetLevel")]
  [CommandAlias("Skill")]
  [CommandDescription("To get specialty level data")]
  [CommandSyntax("<specialty>/<skill>")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public class GetLevelCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GetLevelCommand(SpecialtyOverhaul plugin, IServiceProvider serviceProvider) : base(serviceProvider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      _plugin.PrintToOutput("Parameters:");
      for(int i = 0; i < Context.Parameters.Length; i++)
        _plugin.PrintToOutput(await Context.Parameters.GetAsync<string>(i));

      string _message = "";
      int _message_idx = 0;
      await ParseParameter_Type1(_plugin, Context, true, async (UnturnedUser user, EPlayerSpeciality spec, bool isAllSkill, byte skill_idx, string nextParam) => {
        Action<byte> _callback = (byte skillidx) => {
          var strpair = _plugin.SkillUpdaterInstance.GetExp_AsProgressBar(user, spec, skillidx, true);

          _plugin.PrintToOutput(string.Format("skillidx {0}", skillidx));

          if(_message_idx > 0)
            _message += '\n';

          _message += string.Format("{0}\n{1}", strpair.Key, strpair.Value);
          _message_idx++;
        };

        if(isAllSkill)
          SpecialtyExpData.IterateArray(spec, _callback);
        else
          _callback.Invoke(skill_idx);

        await Context.Actor.PrintMessageAsync(_message);
      });
    }
  }
}
