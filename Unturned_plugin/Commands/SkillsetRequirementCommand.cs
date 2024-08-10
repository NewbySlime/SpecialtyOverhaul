using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;

using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands
{
    [Command("SkillsetRequirement")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandDescription("To list a skillset requirements.")]
  [CommandSyntax("[username or id]/<name of skillset>")]
  public class SkillsetRequirementCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public SkillsetRequirementCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }


    protected override async UniTask OnExecuteAsync() {
      bool _success = false;
      await ParseParameter_Type3(_plugin, Context, true, async (UnturnedUser user, EPlayerSkillset skillset, string nextParam) => {
        await _plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user, async (ISkillModifier editor) => {
          var _lists = editor.GetSkillsetRequirement(skillset);

          bool _isEmpty = true;
          string _msg = "";
          foreach(var _req in _lists) {
            switch(_req.Item1) {
              case ESkillsetRequirementType.SKILL_LEVEL: {
                RequirementLevel? _reqlevel = _req.Item2 as RequirementLevel?;
                if(_reqlevel.HasValue) {
                  var _spec = SkillConfig.specskill_indexer_inverse[_reqlevel.Value.spec];
                  var _skill = _spec.Value[_reqlevel.Value.skill_idx];

                  _msg += string.Format("{0}.{1} Level Requirement\t({2}/{3})\n", _spec.Key, _skill, _reqlevel.Value.currentlevel, _reqlevel.Value.level);
                }

                break;
              }
            }

            _isEmpty = false;
          }

          if(_isEmpty)
            _msg = string.Format("You are eligible to become {0}.", SkillConfig.skillset_indexer_inverse[(byte)skillset]);

          await Context.Actor.PrintMessageAsync(_msg, System.Drawing.Color.YellowGreen);
        });
      });

      if(_success)
        await Context.Actor.PrintMessageAsync("Success.");
    }
  }
}
