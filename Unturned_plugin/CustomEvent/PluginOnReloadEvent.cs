using System;
using OpenMod.API.Eventing;
using OpenMod.Core.Eventing;
using OpenMod.Core.Plugins.Events;
using OpenMod.Unturned.Players;
using SDG.Unturned;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class PluginOnReloadEvent: PluginInterfaceEvent{
    public struct Param {
      public UseableGun gun;
    }

    public Param? param;


    protected static void _onReloading(UseableGun gun) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginOnReloadEvent @event = new PluginOnReloadEvent {
          param = new Param {
            gun = gun
          }
        };

        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
      }
    }


    protected override void Subscribe() {
      UseableGun.OnReloading_Global -= _onReloading;
      UseableGun.OnReloading_Global += _onReloading;
    }

    protected override void Unsubscribe() {
      UseableGun.OnReloading_Global -= _onReloading;
    }

    public PluginOnReloadEvent() {}
  }
}
