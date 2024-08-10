using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.API.Eventing;
using OpenMod.Core.Eventing;
using OpenMod.Core.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin {

  /// <summary>
  /// Note when using this class/interface, all members in a class that will inherit this, should be static.
  /// </summary>
  public class IPluginAutoload:
    IEventListener<PluginLoadedEvent>,
    IEventListener<PluginUnloadedEvent> {

    protected virtual void Instantiate(SpecialtyOverhaul plugin) { }
    protected virtual void Destroy(SpecialtyOverhaul plugin) { }


    [EventListener(Priority = EventListenerPriority.Low)]
    public async Task HandleEventAsync(Object? obj, PluginLoadedEvent @event) {
      SpecialtyOverhaul? plugin = @event.Plugin as SpecialtyOverhaul;
      if(plugin != null) 
        await Task.Run(() => Instantiate(plugin));
    }

    [EventListener(Priority = EventListenerPriority.Low)]
    public async Task HandleEventAsync(Object? obj, PluginUnloadedEvent @event) {
      SpecialtyOverhaul? plugin = @event.Plugin as SpecialtyOverhaul;
      if(plugin != null)
        await Task.Run(() => Destroy(plugin));
    }
  }
}
