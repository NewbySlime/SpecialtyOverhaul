using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
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
using UnityEngine.Rendering;
using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Command for when an admin want to set player level(s)
  /// </summary>
  [Command("SetLevel")]
  [CommandDescription("Set level. Player will be notified if level is edited. (Admin Only)")]
  [CommandSyntax("[name or id]/<specialty>/<skill>/<number>")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public class SetLevelCommand: PromptableCommand {
    private struct _onConfirmData {
      public EPlayerSpeciality spec;
      public float level;
      public UnturnedUser user;
    }

    private readonly SpecialtyOverhaul _plugin;
    
    

    public SetLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base("SetLevel", provider) { _plugin = plugin; }


    protected override async Task OnConfirm(Object obj) {
      _onConfirmData? data = obj as _onConfirmData?;
      if(data.HasValue) {
        await _plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(data.Value.user, async (ISkillModifier editor) => {
          SpecialtyExpData.IterateArray(data.Value.spec, (byte skillidx) => {
            editor.Level(data.Value.spec, skillidx, data.Value.level);
          });

          if(Context.Actor is ConsoleActor || (Context.Actor is UnturnedUser && data.Value.user.SteamId != (Context.Actor as UnturnedUser)?.SteamId))
            await data.Value.user.PrintMessageAsync(string.Format("Level for all of {0} edited by admin.", SkillConfig.specskill_indexer_inverse[data.Value.spec].Key), System.Drawing.Color.Yellow);

          await Context.Actor.PrintMessageAsync("Success.");
        });
      }
    }

    protected override async UniTask OnExecuteAsync() {
      if(await checkPrompt())
        return;

      await ParseParameter_Type2(_plugin, Context, true, async (UnturnedUser user, EPlayerSpeciality spec, bool isAllSkill, byte skill_idx, float lvl, string nextParam) => {
        if(isAllSkill) {
          _onConfirmData data = new() {
            spec = spec,
            level = lvl,
            user = user
          };

          await askPrompt(string.Format("This will overwrite skills in {0}.", SkillConfig.specskill_indexer_inverse[spec].Key), data);
        }
        else {
          _plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user, (ISkillModifier editor) => {
            editor.Level(spec, skill_idx, lvl);
          });

          if(Context.Actor is ConsoleActor || (Context.Actor is UnturnedUser && user.SteamId != (Context.Actor as UnturnedUser)?.SteamId))
            await user.PrintMessageAsync(string.Format("Level for {0} edited by admin.", SkillConfig.specskill_indexer_inverse[spec].Value[skill_idx]), System.Drawing.Color.Yellow);

          await Context.Actor.PrintMessageAsync("Success.");
        }
      });
    }
  }
}
