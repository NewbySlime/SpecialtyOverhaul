using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("CurrentSkillset")]
  [CommandDescription("To get the current skillset.")]
  [CommandSyntax("CurrentSkillset [username or id]")]
  [CommandActor(typeof(UnturnedUser))]
  public class CurrentSkillsetCommand: UnturnedCommand {
    private SpecialtyOverhaul plugin;

    public CurrentSkillsetCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) {
      this.plugin = plugin;
    }


    protected override async UniTask OnExecuteAsync() {
      UnturnedUser? user = null;

      if(Context.Parameters.Length > 0)
        user = await plugin.UnturnedUserProviderInstance.FindUserAsync("", await Context.Parameters.GetAsync<string>(0), OpenMod.API.Users.UserSearchMode.FindByNameOrId) as UnturnedUser;

      if(user == null)
        user = Context.Actor as UnturnedUser;

      if(user != null) {
        await plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user.Player.SteamPlayer.playerID, async (ISkillModifier editor) => {
          EPlayerSkillset ePlayerSkillset = editor.GetSkillset();
          await Context.Actor.PrintMessageAsync(string.Format("Your current skillset: {0}.", SkillConfig.skillset_indexer_inverse[(byte)ePlayerSkillset]), System.Drawing.Color.Aqua);
        });
      }
    }
  }
}
