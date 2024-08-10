using Nekos.SpecialtyPlugin.Mechanic.Autoload;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;


namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  public partial class SkillUpdater {
    private class StaticEventHandler:
      IEventListener<UnturnedPlayerDeathEvent>,
      IEventListener<UnturnedPlayerRevivedEvent>
      {

      public async Task HandleEventAsync(Object? obj, UnturnedPlayerDeathEvent @event){
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        if(plugin != null) {
          plugin.SkillUpdaterInstance.GetSkillEditor_WrapperFunction(@event.Player.SteamPlayer.playerID, (SkillEditor editor) => {
            editor.SkillPersistance.ExpData.lastDeathCause = @event.DeathCause;
          });
        }
      }

      public async Task HandleEventAsync(object? obj, UnturnedPlayerRevivedEvent @event) {
        SkillUpdater? _skillUpdater = SpecialtyOverhaul.Instance?.SkillUpdaterInstance;
        SkillConfig? _skillConfig = SpecialtyOverhaul.Instance?.SkillConfigInstance;
        if(_skillUpdater != null && _skillConfig != null) {
          _skillUpdater.GetSkillEditor_WrapperFunction(@event.Player.SteamPlayer.playerID, (SkillEditor editor) => {
            SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
              editor.CheckLevel(spec, skillidx);
            });

            PlayerContext? context = PlayerLookoutContext.GetPlayerContext(@event.Player);

            if(context != null) {
              List<ISkillMult_Environment> _envlist = new();
              context.IterateEnvironmentFlag((EEnvironment env) => {
                _envlist.Add(_skillConfig.GetEnvironmentMult(env));
              });

              ISkillMult_DeathCause skillMult_death = _skillUpdater.plugin.SkillConfigInstance.GetDeathCauseMult(editor.SkillPersistance.ExpData.lastDeathCause);
              SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
                float _multvalue = skillMult_death.GetMultiplier(spec, skillidx);
                foreach(ISkillMult_Environment _env in _envlist)
                  _multvalue *= _env.GetMultiplier(SkillConfig.EModifierType.LOSS, spec, skillidx);

                int _currentexp = editor.GetExp(spec, skillidx);
                EPlayerSkillset skillset = editor.GetSkillset();

                int _lastexp = _currentexp;
                _multvalue *= _skillUpdater.plugin.SkillConfigInstance.GetOnDiedValue(skillset, spec, skillidx);
                switch(_skillUpdater.plugin.SkillConfigInstance.GetOnDiedType(skillset, spec, skillidx)) {
                  case SkillConfig.EOnDiedEditType.OFFSET:
                    _currentexp -= (int)Math.Round(_multvalue);
                    break;

                  case SkillConfig.EOnDiedEditType.MULT:
                    _currentexp = Math.Min((int)Math.Round(_currentexp / _multvalue), _currentexp);
                    break;

                  case SkillConfig.EOnDiedEditType.BASE: {
                    float _newlvl = Math.Max(CalculateLevelFloat(editor.SkillPersistance.ExpData, spec, skillidx) - _multvalue, 0);
                    _currentexp = CalculateLevelExp(editor.SkillPersistance.ExpData, spec, skillidx, _newlvl);

                    break;
                  }
                }

                if(_currentexp < 0)
                  _currentexp = 0;

                SpecialtyOverhaul.Instance?.PrintToOutput(string.Format("new exp {0}, last exp {1}, _multvalue {2}", _currentexp, _lastexp, _multvalue));
              
                editor.SetExp(spec, skillidx, _currentexp);
              });
            }
          });
        }
      }
    }
  }
}
