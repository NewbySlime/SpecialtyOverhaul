using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMod.Core.Eventing;
using OpenMod.Unturned.Users;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class UnturnedUserRecheckEvent: Event {
    public UnturnedUser user;

    public UnturnedUserRecheckEvent(UnturnedUser user) {
      this.user = user;
    }
  }
}
