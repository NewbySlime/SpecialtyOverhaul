using OpenMod.Unturned.Players;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This interface is for all calculating functions
  /// </summary>
  public interface ICalculationUtils {
    /// <summary>
    /// For calculating how much Exp based on the level.
    /// </summary>
    /// <param name="data">Current player exp data</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <param name="level">Current level</param>
    /// <returns>Total exp in integer. If return -1, then this class hasn't been loaded.</returns>
    public int CalculateLevelExp(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx, float level);

    /// <summary>
    /// For calculating current level based on current total exp.
    /// </summary>
    /// <param name="data">Current player exp data</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <returns>Level in floating point. If return -1, then this class hasn't been loaded.</returns>
    public float CalculateLevelFloat(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx);

    /// <summary>
    /// Functions the same as <see cref="CalculateLevelFloat"/> but as integer.
    /// </summary>
    /// <param name="data">Current player exp data</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <returns>Level in integer. If return -1, then this class hasn't been loaded.</returns>
    public int CalculateLevel(SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx);

    /// <summary>
    /// Applying player's all of level data. Typically this used when all skills is edited or when player has just connected. Only works when player is online. Unless it is used when only calculating all the skill.
    /// </summary>
    /// <param name="user">Current <see cref="UnturnedUser"/> object. Can be null if player isn't online.</param>=
    /// <param name="expData"></param>
    public PreviouslyModifiedSkillData ReCalculateAllSkillTo(UnturnedUser? user, SpecialtyExpData expData);

    /// <summary>
    /// Recalculating if there are level changes, and applied it to player. Event <see cref="PluginOnPlayerLevelChanged"/> will be invoked when there's a change in the level.
    /// </summary>
    /// <param name="user">Current <see cref="UnturnedUser"/> object. Can be null if player isn't online</param>
    /// <param name="expData"></param>
    /// <param name="speciality">What specialty.</param>
    /// <param name="index">What index.</param>
    public PreviouslyModifiedSkillData ReCalculateSkillTo(UnturnedUser? user, SpecialtyExpData expData, byte speciality, byte index);
  }
}
