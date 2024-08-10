using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("SpcOvh_DisableConfigAutoload")]
  [CommandDescription("This will disable/enable plugin's configuration autoload every time the config is edited.")]
  [CommandActor(typeof(ConsoleActor))]
  public class DisableConfigAutoloadCommand: UnturnedCommand {
    private readonly SpecialtyOverhaul plugin;

    public DisableConfigAutoloadCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) {
      this.plugin = plugin;
    }

    protected override async UniTask OnExecuteAsync() {
      bool _setflag = plugin.SetEnable_ConfigAutoload();
      if(_setflag)
        await Context.Actor.PrintMessageAsync("Config autoload enabled.");
      else
        await Context.Actor.PrintMessageAsync("Config autoload disable.");
    }
  }
}
