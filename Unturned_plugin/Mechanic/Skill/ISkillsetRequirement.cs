using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// ISkillsetRequirement can be obtained by the <see cref="SkillConfig"/> class
  /// </summary>
  public interface ISkillsetRequirement {

    public byte GetLevelRequirement(EPlayerSpeciality spec, byte idx);
    public bool IsRequirementsFulfilled(SpecialtyExpData expData);

    public List<(ESkillsetRequirementType, object)> ListOfPendingRequirement(SpecialtyExpData expData);

    public PreviouslyModifiedSkillData ForceRequirements(SpecialtyExpData expData, UnturnedUser? user = null);
  }
}
