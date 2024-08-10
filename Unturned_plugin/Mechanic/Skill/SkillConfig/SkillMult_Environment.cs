using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill
{
    public partial class SkillConfig {
    private class SkillMult_Environment: ISkillMult_Environment {
      private readonly Dictionary<(EPlayerSpeciality, byte, EModifierType), float> env_mult;

      public SkillMult_Environment(Dictionary<(EPlayerSpeciality, byte, EModifierType), float> dict) {
        env_mult = dict;
      }

      public void SkillIterate(SkillConfig.EModifierType eModifierType, Action<EPlayerSpeciality, byte, float> callback) {
        SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
          callback.Invoke(spec, skillidx, GetMultiplier(eModifierType, spec, skillidx));
        });
      }

      public float GetMultiplier(SkillConfig.EModifierType eModifierType, EPlayerSpeciality spec, byte skillidx) {
        if(env_mult.TryGetValue((spec, skillidx, eModifierType), out float _val))
          return _val;
        else
          return 1;
      }
    }
  }
}
