using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This class contains player exp data for each skill
  /// </summary>
  public class SpecialtyExpData {
    public const int _speciality_count = 3;
    public const int _skill_offense_count = (int)EPlayerOffense.PARKOUR + 1;
    public const int _skill_defense_count = (int)EPlayerDefense.SURVIVAL + 1;
    public const int _skill_support_count = (int)EPlayerSupport.ENGINEER + 1;

    // data for checking if the player has leveled up or not
    public int[][] skillsets_expborderhigh = InitArrayT<int>();
    public int[][] skillsets_expborderlow = InitArrayT<int>();


    public float[][] skillsets_exp_fraction = InitArrayT<float>();

    public int[][] skillsets_exp = InitArrayT<int>();
    public EPlayerSkillset skillset = EPlayerSkillset.NONE;


    /// <summary>
    /// Creating an array by how many specialties and skills
    /// </summary>
    /// <typeparam name="T">The type of the array</typeparam>
    /// <returns>Array of T</returns>
    public static T[][] InitArrayT<T>() {
      return new T[_speciality_count][] {
        // EPlayerspeciality.OFFENSE
        new T[_skill_offense_count],
 
        // EPlayerspeciality.DEFENSE
        new T[_skill_defense_count],

        // EPlayerspeciality.SUPPORT
        new T[_skill_support_count]
      };
    }

    /// <summary>
    /// Copying an array to target array. The array size is determined by how many specialties and skills. Look at <see cref="InitArrayT{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the array</typeparam>
    /// <param name="dst">Target array</param>
    /// <param name="src">Source array</param>
    public static void CopyArrayT<T>(ref T[][] dst, in T[][] src) {
      for(int i = 0; i < _speciality_count; i++) {
        int len = 0;
        switch((EPlayerSpeciality)i) {
          case EPlayerSpeciality.OFFENSE:
            len = _skill_offense_count;
            break;

          case EPlayerSpeciality.DEFENSE:
            len = _skill_defense_count;
            break;

          case EPlayerSpeciality.SUPPORT:
            len = _skill_support_count;
            break;
        }

        for(int o = 0; o < len; o++) {
          dst[i][o] = src[i][o];
        }
      }
    }
  }
}
