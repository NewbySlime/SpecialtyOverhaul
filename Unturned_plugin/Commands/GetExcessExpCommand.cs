using System;
using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("GetExcessExp")]
  [CommandAlias("Excess")]
  [CommandDescription("Getting excess exp")]
  [CommandActor(typeof(UnturnedUser))]
  public class GetExcessExpCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul plugin;

    public GetExcessExpCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base(provider) {
      this.plugin = plugin;
    }

    protected override async UniTask OnExecuteAsync() {
      UnturnedUser? user = Context.Actor as UnturnedUser;
      if(user != null)
        await user.PrintMessageAsync(string.Format("Excess exp: {0}", plugin.SkillUpdaterInstance.GetExcessExp(user.Player)), System.Drawing.Color.YellowGreen);
    }
  }
}
