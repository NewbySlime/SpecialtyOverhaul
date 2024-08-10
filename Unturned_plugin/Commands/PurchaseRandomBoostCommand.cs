using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("PurchaseRandomBoost")]
  [CommandAlias("PurchaseRandom")]
  [CommandAlias("RandomBoost")]
  [CommandActor(typeof(UnturnedUser))]
  [CommandDescription("This command is used for purchasing random skills.")]
  public class PurchaseRandomBoostCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul plugin;

    public PurchaseRandomBoostCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base(provider) {
      this.plugin = plugin;
    }

    protected override async UniTask OnExecuteAsync() {
      UnturnedUser? user = Context.Actor as UnturnedUser;
      if(user != null) {
        await plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user.Player.SteamPlayer.playerID, async (ISkillModifier editor) => {
          if(editor.PurchaseRandomBoost()) {
            await user.PrintMessageAsync(string.Format("Purchase succeed. Excess exp: {0}", editor.ExcessExp), System.Drawing.Color.LightGreen);
            await user.PrintMessageAsync(string.Format("Your new boost; {0}", user.Player.Player.skills.boost.ToString()), System.Drawing.Color.Aqua, false, "");
          }
          else
            await user.PrintMessageAsync(string.Format("Excess Exp insufficient. Current: {0}/{1}", editor.ExcessExp, plugin.SkillConfigInstance.GetRandomBoostCost()), System.Drawing.Color.Red);
        });
      }
    }
  }
}
