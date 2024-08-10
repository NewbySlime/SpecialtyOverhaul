using Nekos.SpecialtyPlugin.CustomEvent;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class LevelWatcher:
    IEventListener<PluginOnPlayerLevelChanged>
    {

    public async Task HandleEventAsync(Object? obj, PluginOnPlayerLevelChanged @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null && @event.param != null) {
        plugin.PrintToOutput(string.Format("new {0}, last {1}", @event.param.newLevel, @event.param.lastLevel));
        await plugin.SkillUpdaterInstance.GetSkillEditor_WrapperFunction(@event.param.player.SteamPlayer.playerID, async (SkillEditor editor) => {

          if(@event.param.newLevel > @event.param.lastLevel) {
            await @event.param.player.PrintMessageAsync(
              string.Format(
                "{0} Leveled up!\t({1}/{2})",
                SkillConfig.specskill_indexer_inverse[@event.param.skill.Item1].Value[@event.param.skill.Item2],
                @event.param.newLevel,
                plugin.SkillConfigInstance.GetMaxLevel(editor.GetSkillset(), @event.param.skill.Item1, @event.param.skill.Item2)
              ),
              System.Drawing.Color.Green
            );
          }

          EPlayerSkillset playerSkillset = editor.GetSkillset();
          if(playerSkillset == EPlayerSkillset.NONE) {
            for(int i = 0; i < SkillConfig._skillset_count; i++) {
              EPlayerSkillset skillset = (EPlayerSkillset)i;
              if(skillset == EPlayerSkillset.NONE || skillset == playerSkillset)
                continue;

              ISkillsetRequirement requirements = plugin.SkillConfigInstance.GetSkillsetRequirement(skillset);

              byte _reqlevel = requirements.GetLevelRequirement(@event.param.skill.Item1, @event.param.skill.Item2);
              if(@event.param.lastLevel < _reqlevel && @event.param.newLevel >= _reqlevel && requirements.IsRequirementsFulfilled(editor.SkillPersistance.ExpData)) {
                await @event.param.player.PrintMessageAsync(string.Format("You are eligible to become {0}.", SkillConfig.skillset_indexer_inverse[(byte)skillset]), System.Drawing.Color.Aqua);
              }
            }
          }
          else if(plugin.SkillConfigInstance.IsSkillsetDemoteable(playerSkillset)) {
            ISkillsetRequirement requirements = plugin.SkillConfigInstance.GetSkillsetRequirement(playerSkillset);

            byte _reqlevel = requirements.GetLevelRequirement(@event.param.skill.Item1, @event.param.skill.Item2);
            if(@event.param.newLevel < _reqlevel) {
              editor.SetSkillset(EPlayerSkillset.NONE);
              await @event.param.player.PrintMessageAsync("You have been demoted.", System.Drawing.Color.Red);
            }
          }
        });
      }
    }
  }
}
