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
  [Command("SpcOvh_ReloadConfig")]
  [CommandDescription("Reload plugin's configuration.")]
  [CommandActor(typeof(ConsoleActor))]
  public class ReloadConfigCommand: UnturnedCommand {
    private SpecialtyOverhaul plugin;

    public ReloadConfigCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base(provider) {
      this.plugin = plugin;
    }

    protected override async UniTask OnExecuteAsync() {
      plugin.RefreshConfig();
    }
  }
}
