using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("ListSkillset")]
  [CommandDescription("To list all available skillset.")]
  [CommandActor(typeof(UnturnedUser))]
  public class ListSkillsetCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public ListSkillsetCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      foreach(var _skillset in SkillConfig.skillset_indexer) {
        string _msg = string.Format("{0} ({1})", _skillset.Key, SkillConfig.skillset_indexer_inverse[_skillset.Value]);
        await Context.Actor.PrintMessageAsync(_msg, System.Drawing.Color.Aqua);
      }
    }
  }
}
