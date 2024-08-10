using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers {
  public interface ISkillMult_DeathCause {
    public void SkillIterate(Action<EPlayerSpeciality, byte, float> callback);
    public float GetMultiplier(EPlayerSpeciality spec, byte skillidx);
  }
}
