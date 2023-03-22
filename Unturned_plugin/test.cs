using System;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using Cysharp.Threading.Tasks;
using OpenMod.Unturned.Users;
using Nekos.SpecialtyPlugin;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Effects;
using Steamworks;

[Command("test")]
[CommandDescription("just a test")]
[CommandActor(typeof(UnturnedUser))]
public class Test_command : UnturnedCommand
{
  private readonly SpecialtyOverhaul plugin;
  public Test_command(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider)
  {
    this.plugin = plugin;
  }

  protected override async UniTask OnExecuteAsync()
  {
    await PrintAsync(string.Format("{0}, {1}", Context.Actor.DisplayName, Context.Actor.Id));

    UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(new CSteamID(ulong.Parse(Context.Actor.Id)));
    if(user != null)
    {
      await user.PrintMessageAsync("test");
    }
  }
}