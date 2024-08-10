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
    private class SkillMult_DeathCause: ISkillMult_DeathCause {
      private readonly Dictionary<(EPlayerSpeciality, byte), float> deathcause_mult;

      public SkillMult_DeathCause(Dictionary<(EPlayerSpeciality, byte), float> dict) {
        deathcause_mult = dict;
      }

      public void SkillIterate(Action<EPlayerSpeciality, byte, float> callback) {
        SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
          callback.Invoke(spec, skillidx, GetMultiplier(spec, skillidx));
        });
      }

      public float GetMultiplier(EPlayerSpeciality spec, byte skillidx) {
        if(deathcause_mult.TryGetValue((spec, skillidx), out float _val))
          return _val;
        else
          return 1;
      }
    }
  }
}
