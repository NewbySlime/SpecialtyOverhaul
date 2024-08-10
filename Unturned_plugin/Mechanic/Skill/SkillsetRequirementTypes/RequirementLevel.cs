using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes {
  public struct RequirementLevel {
    public EPlayerSpeciality spec;
    public byte skill_idx;

    public byte currentlevel;
    public byte level;
  }
}
