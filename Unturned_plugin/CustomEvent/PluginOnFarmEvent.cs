using System;
using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Core.Eventing;
using OpenMod.Core.Plugins.Events;
using OpenMod.Unturned.Players;
using SDG.Unturned;

namespace Nekos.SpecialtyPlugin.CustomEvent
{
  public class PluginOnFarmEvent: IPluginEvent, ICancellableEvent{
    public struct Param {
      public InteractableFarm farm;
      public SteamPlayer player;
    }

    public Param? param;
    public bool IsCancelled { get; set; }


    private static void _onFarming(InteractableFarm farm, SteamPlayer splayer, ref bool cancelled) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        PluginOnFarmEvent @event = new PluginOnFarmEvent {
          param = new Param {
            player = splayer,
            farm = farm,
          },

          IsCancelled = false
        };

        plugin.EventBus.EmitAsync(plugin, null, @event).Wait();
        if(@event.IsCancelled) {
          cancelled = true;
        }
      }
    }

    protected override void Subscribe() {
      InteractableFarm.OnHarvestRequested_Global -= _onFarming;
      InteractableFarm.OnHarvestRequested_Global += _onFarming;
    }

    protected override void Unsubscribe() {
      InteractableFarm.OnHarvestRequested_Global -= _onFarming;
    }


    public PluginOnFarmEvent() {}
  }
}
