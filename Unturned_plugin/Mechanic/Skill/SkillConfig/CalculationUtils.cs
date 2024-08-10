using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Nekos.SpecialtyPlugin.Mechanic.Skill.PreviouslyModifiedSkillData;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  partial class SkillConfig {
    internal class CalculationUtils: ICalculationUtils {
      private SkillConfig config;
      public CalculationUtils(SkillConfig config) {
        this.config = config;
      }


      public int CalculateLevelExp(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx, float level) {
        float basef = (float)config.GetBaseLevelExp(data.skillset, spec, skill_idx);
        float multf = config.GetMultLevelExp(data.skillset, spec, skill_idx);
        float multmultf = config.GetMultMultLevelExp(data.skillset, spec, skill_idx);

        return (int)Math.Abs(Math.Ceiling(basef * Math.Pow(multf * level, multmultf)));
      }

      public float CalculateLevelFloat(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx) {
        float dataf = data.skillsets_exp[(int)spec][skill_idx];
        float basef = config.GetBaseLevelExp(data.skillset, spec, skill_idx);

        if(dataf < basef)
          return 0;

        float multf = config.GetMultLevelExp(data.skillset, spec, skill_idx);
        float multmultf = config.GetMultMultLevelExp(data.skillset, spec, skill_idx);

        return (float)Math.Pow(dataf / basef, 1.0 / multmultf) / multf;
      }

      public int CalculateLevel(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx) {
        // getting fraction of the result
        float _lvl = CalculateLevelFloat(data, spec, skill_idx);
        int nlvl = (int)Math.Floor(_lvl);
        _lvl -= nlvl;

        // check floating-point error
        if(_lvl > 0.99)
          nlvl++;

        return (byte)nlvl;
      }

      public PreviouslyModifiedSkillData ReCalculateAllSkillTo(UnturnedUser? user, SpecialtyExpData expData) {
        PreviouslyModifiedSkillData _res = new();

        SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
          var _funcres = ReCalculateSkillTo(user, expData, (byte)spec, skill_idx);
          _res.CombineWithAnother(_funcres);
        });

        return _res;
      }

      public PreviouslyModifiedSkillData ReCalculateSkillTo(UnturnedUser? user, SpecialtyExpData expData, byte speciality, byte index) {
        PreviouslyModifiedSkillData _res = new();
        ICalculationUtils calcutil = config.Calculation;

        byte _newlevel = (byte)calcutil.CalculateLevel(expData, (EPlayerSpeciality)speciality, index);
        byte _minlevel = config.GetStartLevel(expData.skillset, (EPlayerSpeciality)speciality, index);
        byte _maxlevel = config.GetMaxLevel(expData.skillset, (EPlayerSpeciality)speciality, index);

        SpecialtyOverhaul.Instance?.PrintToOutput(string.Format("newlevel {0}/{1}", _newlevel, _maxlevel));

        if(_newlevel < _minlevel) {
          // don't add to PreviouslyModifiedSkillData because the exp is invalid
          _newlevel = _minlevel;
          expData.skillsets_exp[speciality][index] = calcutil.CalculateLevelExp(expData, (EPlayerSpeciality)speciality, index, _newlevel);
        }
        else if(_newlevel > _maxlevel)
          _newlevel = _maxlevel;

        SpecialtyOverhaul.Instance?.PrintToOutput(string.Format("newlevel {0}", _newlevel));

        int _borderlow = calcutil.CalculateLevelExp(expData, (EPlayerSpeciality)speciality, index, _newlevel);
        int _borderhigh = calcutil.CalculateLevelExp(expData, (EPlayerSpeciality)speciality, index, (byte)(_newlevel + 1));

        if(_newlevel >= _maxlevel)
          _borderhigh *= -1;

        expData.skillsets_expborderhigh[speciality][index] = _borderhigh;
        expData.skillsets_expborderlow[speciality][index] = _borderlow;

        int _currentexp = expData.skillsets_exp[speciality][index];
        if(_borderhigh < 0 && (_currentexp >= Math.Abs(_borderhigh))) {
          // don't add exp to PreviouslyModifiedSkillData because the exp already exceeded the max
          _res.ModifiedData.Add((
            ChangeCodes.TYPE_EXCESS,
            new ChangeExcessExp() {
              ExcessExp = expData.excess_exp
            }
          ));

          expData.skillsets_exp[speciality][index] = ((_currentexp - _borderlow) % (Math.Abs(_borderhigh) - _borderlow)) + _borderlow;
          
          int _excess_incr = config.GetPlayerExcessExpIncrement(expData.skillset, (EPlayerSpeciality)speciality, index);
          expData.excess_exp += _excess_incr;

          if(user != null) {
            PluginOnExcessExpIncrementedEvent.Invoke(this, new(user, _excess_incr, (EPlayerSpeciality)speciality, index));
          }
        }


        // this part of code is just to remind player of leveling up and applying the level
        if(user != null) {
          PlayerSkills playerSkills = user.Player.Player.skills;

          byte _lastLevel = playerSkills.skills[speciality][index].level;
          if(_lastLevel != _newlevel) {
            playerSkills.ServerSetSkillLevel(speciality, index, _newlevel);

            PluginOnPlayerLevelChanged.Invoke(null, new PluginOnPlayerLevelChanged.Param(user.Player, _lastLevel, _newlevel, ((EPlayerSpeciality)speciality, index)));
          }
        }

        return _res;
      }
    }
  }
}
