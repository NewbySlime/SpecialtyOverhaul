using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Core.Eventing;
using OpenMod.Core.Events;
using OpenMod.Core.Plugins.Events;
using SDG.Unturned;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class IPluginEvent: Event,
    IEventListener<PluginLoadedEvent>,
    IEventListener<PluginUnloadedEvent>
    {

    protected virtual void Subscribe() {
    
    }

    protected virtual void Unsubscribe() {
    
    }


    [EventListener(Priority = EventListenerPriority.Lowest)]
    public async Task HandleEventAsync(Object? obj, PluginLoadedEvent @event) {
      await Task.Run(Subscribe);
    }

    [EventListener(Priority = EventListenerPriority.Lowest)]
    public async Task HandleEventAsync(Object? obj, PluginUnloadedEvent @event) {
      await Task.Run(Unsubscribe);
    }
  }
}
