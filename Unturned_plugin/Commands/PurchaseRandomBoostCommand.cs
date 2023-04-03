using Cysharp.Threading.Tasks;
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
        if(plugin.SkillUpdaterInstance.PurchaseRandomBoost(user.Player)) {
          await user.PrintMessageAsync(string.Format("Purchase succeed. Excess exp: {0}", plugin.SkillUpdaterInstance.GetExcessExp(user.Player)), System.Drawing.Color.LightGreen);
          await user.PrintMessageAsync(string.Format("Your new boost; {0}", user.Player.Player.skills.boost.ToString()), System.Drawing.Color.Aqua, false, "");
        }
        else
          await user.PrintMessageAsync(string.Format("Excess Exp insufficient. Current: {0}/{1}", plugin.SkillUpdaterInstance.GetExcessExp(user.Player), plugin.SkillConfigInstance.GetRandomBoostCost()), System.Drawing.Color.Red);
      }
    }
  }
}
