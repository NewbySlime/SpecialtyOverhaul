using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// Command for when an admin want to give level(s) to a player
  /// </summary>
  [Command("GiveLevel")]
  [CommandDescription("Giving level (or exp by using fractions) to level up. Player will be notified upon receiving level. (Admin Only)")]
  [CommandSyntax("[name or id]/<specialty>/<skill>/<amount>")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandActor(typeof(ConsoleActor))]
  public class GiveLevelCommand: PromptableCommand {
    private struct _onConfirmData {
      public EPlayerSpeciality spec;
      public float level;
      public UnturnedUser user;
    }


    private readonly SpecialtyOverhaul _plugin;

    public GiveLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base("GiveLevel", provider) { _plugin = plugin; }


    protected override async Task OnConfirm(object obj) {
      _onConfirmData? data = obj as _onConfirmData?;
      if(data.HasValue) {
        await _plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(data.Value.user, async (ISkillModifier editor) => {
          SpecialtyExpData.IterateArray(data.Value.spec, (byte skillidx) => {
            float _currentlvl = editor.Level(data.Value.spec, skillidx);
            editor.Level(data.Value.spec, skillidx, _currentlvl+data.Value.level);
          });

          if(Context.Actor is ConsoleActor || (Context.Actor is UnturnedUser && data.Value. user.SteamId != (Context.Actor as UnturnedUser)?.SteamId))
            await data.Value.user.PrintMessageAsync(string.Format("Level for all of {0} given from admin.", SkillConfig.specskill_indexer_inverse[data.Value.spec].Key), System.Drawing.Color.Yellow);

          await Context.Actor.PrintMessageAsync("Success.");
        });
      }
    }

    protected override async UniTask OnExecuteAsync() {
      if(await checkPrompt())
        return;

      await ParseParameter_Type2(_plugin, Context, true, async (UnturnedUser user, EPlayerSpeciality spec, bool isAllSkill, byte skillidx, float level, string nextParam) => {
        if(isAllSkill) {
          _onConfirmData data = new() {
            spec = spec,
            level = level,
            user = user
          };

          await askPrompt(string.Format("This will give levels for {0}.", SkillConfig.specskill_indexer_inverse[spec].Key), data);
        }
        else {
          _plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user, (ISkillModifier editor) => {
            float _currentlvl = editor.Level(spec, skillidx);
            editor.Level(spec, skillidx, _currentlvl+level);
          });

          if(Context.Actor is ConsoleActor || (Context.Actor is UnturnedUser && user.SteamId != (Context.Actor as UnturnedUser)?.SteamId))
            await user.PrintMessageAsync(string.Format("Level for {0} given from admin.", SkillConfig.specskill_indexer_inverse[spec].Value[skillidx]), System.Drawing.Color.Yellow);

          await Context.Actor.PrintMessageAsync("Success.");
        }
      });
    }
  }
}
