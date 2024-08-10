using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;
using static Nekos.SpecialtyPlugin.Mechanic.Skill.PreviouslyModifiedSkillData;
using OpenMod.Unturned.Users;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill
{
    public partial class SkillConfig {
    private class SkillsetRequirements: ISkillsetRequirement {
      private readonly ConfigData.SkillsetUpdateConfig _skillsetConfig;
      private readonly SkillConfig _skillConfig;

      public SkillsetRequirements(SkillConfig config, ConfigData.SkillsetUpdateConfig skillsetConfig) {
        _skillConfig = config;
        _skillsetConfig = skillsetConfig;
      }


      public byte GetLevelRequirement(EPlayerSpeciality spec, byte idx) {
        if(_skillsetConfig._level_requirements.TryGetValue((spec, idx), out byte value))
          return value;

        return 0;
      }

      public bool IsRequirementsFulfilled(SpecialtyExpData expData) {
        ICalculationUtils calcutil = _skillConfig.Calculation;

        foreach(var skillreq in _skillsetConfig._level_requirements) {
          if(calcutil.CalculateLevel(expData, skillreq.Key.Item1, skillreq.Key.Item2) < skillreq.Value)
            return false;
        }

        return true;
      }

      public List<(ESkillsetRequirementType, object)> ListOfPendingRequirement(SpecialtyExpData expData) {
        ICalculationUtils calcutil = _skillConfig.Calculation;
        List<(ESkillsetRequirementType, object)> _reqs = new List<(ESkillsetRequirementType, object)>();

        foreach(var skillreq in _skillsetConfig._level_requirements) {
          int _currentlevel = calcutil.CalculateLevel(expData, skillreq.Key.Item1, skillreq.Key.Item2);
          if(_currentlevel < skillreq.Value) {
            _reqs.Add((ESkillsetRequirementType.SKILL_LEVEL, new RequirementLevel {
              spec = skillreq.Key.Item1,
              skill_idx = skillreq.Key.Item2,
              currentlevel = (byte)_currentlevel,
              level = skillreq.Value
            }));
          }
        }

        return _reqs;
      }

      public PreviouslyModifiedSkillData ForceRequirements(SpecialtyExpData expData, UnturnedUser? user = null) {
        ICalculationUtils calcutil = _skillConfig.Calculation;
        PreviouslyModifiedSkillData _res = new();

        foreach(var skillreq in _skillsetConfig._level_requirements) {
          int _currentlevel = calcutil.CalculateLevel(expData, skillreq.Key.Item1, skillreq.Key.Item2);
          if(_currentlevel < skillreq.Value) {
            EPlayerSpeciality spec = skillreq.Key.Item1;
            byte skillidx = skillreq.Key.Item2;

            _res.ModifiedData.Add((
              ChangeCodes.TYPE_EXP,
              new ChangeExp() {
                Speciality = spec,
                SkillIdx = skillidx,
                Exp = expData.skillsets_exp[(byte)spec][skillidx]
              }
            ));

            expData.skillsets_exp[(byte)spec][skillidx] = calcutil.CalculateLevelExp(expData, spec, skillidx, skillreq.Value);
            var _pmsd = calcutil.ReCalculateSkillTo(user, expData, (byte)spec, skillidx);

            _res.CombineWithAnother(_pmsd);
          }
        }

        return _res;
      }
    }
  }
}
