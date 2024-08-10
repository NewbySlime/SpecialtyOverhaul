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

    public int excess_exp = 0;


    // data for checking if the player has leveled up or not
    
    /// <summary>
    /// Note: this value will be negative if current level is max (to store border for excess exp)
    /// </summary>
    public int[][] skillsets_expborderhigh = InitArrayT<int>();
    public int[][] skillsets_expborderlow = InitArrayT<int>();


    public float[][] skillsets_exp_fraction = InitArrayT<float>();

    public int[][] skillsets_exp = InitArrayT<int>();
    public EPlayerSkillset skillset = EPlayerSkillset.NONE;

    public EDeathCause lastDeathCause = EDeathCause.KILL;


    private static T[] _createArray<T>(int len, T? defvalue = default) {
      T[] arr = new T[len];
      if(defvalue != null) {
        for(int i = 0; i < len; i++)
          arr[i] = defvalue;
      }

      return arr;
    }


    public SpecialtyExpData(SpecialtyExpData? expData = null) {
      if(expData != null) {
        SetTo(expData);
      }
    }

    public void SetTo(SpecialtyExpData expData) {
      CopyArrayT(skillsets_exp_fraction, expData.skillsets_exp_fraction);
      CopyArrayT(skillsets_exp, expData.skillsets_exp);
      skillset = expData.skillset;
      excess_exp = expData.excess_exp;
    }

    /// <summary>
    /// Creating an array by how many specialties and skills
    /// </summary>
    /// <typeparam name="T">The type of the array</typeparam>
    /// <returns>Array of T</returns>
    public static T[][] InitArrayT<T>(T? defvalue = default) {
      return new T[_speciality_count][] {
        // EPlayerspeciality.OFFENSE
        _createArray(_skill_offense_count, defvalue),
 
        // EPlayerspeciality.DEFENSE
        _createArray(_skill_defense_count, defvalue),

        // EPlayerspeciality.SUPPORT
        _createArray(_skill_support_count, defvalue)
      };
    }

    /// <summary>
    /// For iterating each specialty and skill
    /// </summary>
    /// <typeparam name="T">The array type</typeparam>
    /// <param name="callback">Callback to handle it</param>
    public static void IterateArray(Action<EPlayerSpeciality, byte> callback) {
      for(byte i_specs = 0; i_specs < _speciality_count; i_specs++) {
        byte skill_len = 0;
        switch((EPlayerSpeciality)i_specs) {
          case EPlayerSpeciality.OFFENSE:
            skill_len = _skill_offense_count;
            break;

          case EPlayerSpeciality.DEFENSE:
            skill_len = _skill_defense_count;
            break;

          case EPlayerSpeciality.SUPPORT:
            skill_len = _skill_support_count;
            break;
        }

        for(byte i_skill = 0; i_skill < skill_len; i_skill++)
          callback.Invoke((EPlayerSpeciality)i_specs, i_skill);
      }
    }

    public static void IterateArray(EPlayerSpeciality spec, Action<byte> callback) {
      byte skill_len = 0;
      switch(spec) {
        case EPlayerSpeciality.OFFENSE:
          skill_len = _skill_offense_count;
          break;

        case EPlayerSpeciality.DEFENSE:
          skill_len = _skill_defense_count;
          break;

        case EPlayerSpeciality.SUPPORT:
          skill_len = _skill_support_count;
          break;
      }

      for(byte i_skill = 0; i_skill < skill_len; i_skill++)
        callback.Invoke(i_skill);
    }

    /// <summary>
    /// Copying an array to target array. The array size is determined by how many specialties and skills. Look at <see cref="InitArrayT{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the array</typeparam>
    /// <param name="dst">Target array</param>
    /// <param name="src">Source array</param>
    public static void CopyArrayT<T>(T[][] dst, T[][] src) {
      IterateArray((EPlayerSpeciality spec, byte skill) => {
        dst[(byte)spec][skill] = src[(byte)spec][skill];
      });
    }

    public static void FillArrayT<T>(T[][] dst, T val) {
      IterateArray((EPlayerSpeciality spec, byte skill) => {
        dst[(byte)spec][skill] = val;
      });
    }
  }
}
