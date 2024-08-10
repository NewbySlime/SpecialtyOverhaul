using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers {
  public interface ISkillMult_Environment {
    public void SkillIterate(SkillConfig.EModifierType eModifierType, Action<EPlayerSpeciality, byte, float> callback);
    public float GetMultiplier(SkillConfig.EModifierType eModifierType, EPlayerSpeciality spec, byte skillidx);
  }
}
