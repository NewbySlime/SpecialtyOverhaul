using Nekos.SpecialtyPlugin.CustomEvent;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Core.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin {
  public partial class SpecialtyOverhaul {
    private class StaticEventHandler : 
      IEventListener<PluginLoadedEvent>,
      IEventListener<PluginOnExcessExpIncrementedEvent>{


      public async Task HandleEventAsync(Object? obj, PluginLoadedEvent @event) {
        SpecialtyOverhaul? plugin = @event.Plugin as SpecialtyOverhaul;
        if(plugin != null) 
          await plugin._loadPlugin();
      }

      public async Task HandleEventAsync(Object? obj, PluginOnExcessExpIncrementedEvent @event) {
        if(@event.param != null) {
          var _skill_indexer = SkillConfig.specskill_indexer_inverse[@event.param.Spec];
          await @event.param.User.PrintMessageAsync(string.Format("Excess Exp Incremented +{0} by '{1}.{2}'", @event.param.Excess, _skill_indexer.Key, _skill_indexer.Value[@event.param.Skillidx]), System.Drawing.Color.Aqua);
        }
      }
    }
  }
}
