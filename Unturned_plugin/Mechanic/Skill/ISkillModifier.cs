using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;
using Nekos.SpecialtyPlugin.Watcher;
using NuGet.Protocol.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {

  /// <summary>
  /// <para>There's a difference between ISkillModifier provided by SkillUpdater and the ones created by public class.</para>
  /// <para>Within ISKillModifier by SkillUdpater, the ISkillModifier can be changed and the modified data will be automatically sent to Unturned's user data.</para>
  /// <para>While ISkillModifier created by public class can be altererd without sending any updates to Unturned.</para>
  /// <para>ISkillModifier also gives an update on what to unwanted change, like for examples, excess exp incremented because of level up.</para>
  /// </summary>
  public interface ISkillModifier {
    /// <summary>
    /// To get or set player's ExcessExp.
    /// </summary>
    public int ExcessExp {
      get;
      set;
    }

    /// <summary>
    /// To get or set player's skill level. If the 'level' parameter is not set, then the function will be as getter.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public float Level(EPlayerSpeciality spec, byte index, float level = -1);

    /// <summary>
    /// To get or set player's skill exp. If the 'exp' parameter is not set, then the function will be as getter.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public int LevelExp(EPlayerSpeciality spec, byte index, int exp = -1);

    /// <summary>
    /// This function is to increment skill level using floating point.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="increment">Level to increment</param>
    public void ExpFractionIncrement(EPlayerSpeciality spec, byte index, float increment);

    /// <summary>
    /// This should set player's skillset, since changing skillset needs some minimum levels (or requirements) to be sufficient enough.
    /// </summary>
    /// <param name="skillset">The skillset that will be applied to the player</param>
    /// <param name="force">Will force the skillset, even if the requirements aren't fulfilled.</param>
    /// <returns>If successfully set the skillset.</returns>
    public bool SetPlayerSkillset(EPlayerSkillset skillset, bool force = false);

    /// <summary>
    /// To get player's current <see cref="EPlayerSpeciality"/>.
    /// </summary>
    /// <returns></returns>
    public EPlayerSkillset GetPlayerSkillset();

    /// <summary>
    /// To get requirements based on Skillset provided.
    /// </summary>
    /// <param name="skillset"></param>
    /// <returns>The requirements</returns>
    public List<(ESkillsetRequirementType, object)> GetSkillsetRequirement(EPlayerSkillset skillset);

    /// <summary>
    /// To check if the player can apply to certain skillset.
    /// </summary>
    /// <param name="skillset"></param>
    /// <returns>If applicable</returns>
    public bool IsSkillsetFulfilled(EPlayerSkillset skillset);

    /// <summary>
    /// To purchase a random boost.
    /// </summary>
    /// <returns>If successful or not. If unsuccessful, then ExcessExp isn't enough.</returns>
    public bool PurchaseRandomBoost();
  }
}
