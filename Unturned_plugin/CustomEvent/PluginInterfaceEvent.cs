using System;
using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Core.Eventing;
using OpenMod.Core.Plugins.Events;

namespace Nekos.SpecialtyPlugin.CustomEvent {
  public class PluginInterfaceEvent: Event,
    IEventListener<PluginLoadedEvent>,
    IEventListener<PluginUnloadedEvent>
    {

    protected virtual void Subscribe() {
    
    }

    protected virtual void Unsubscribe() {
    
    }
    

    public async Task HandleEventAsync(Object? obj, PluginLoadedEvent @event) {
      await Task.Run(Subscribe);
    }

    public async Task HandleEventAsync(Object? obj, PluginUnloadedEvent @event) {
      await Task.Run(Unsubscribe);
    }
  }
}
