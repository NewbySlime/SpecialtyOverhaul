using Nekos.SpecialtyPlugin.Mechanic.Skill;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public partial class NonAutoloadWatcher {
    private FarmWatcher farmWatcher = new FarmWatcher();
    private InventoryWatcher.ReloadWatcher reloadWatcher = new InventoryWatcher.ReloadWatcher();
  }
}
