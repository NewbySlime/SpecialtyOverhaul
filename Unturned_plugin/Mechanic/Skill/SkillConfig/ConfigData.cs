using SDG.Unturned;
using Nekos.SpecialtyPlugin.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  public partial class SkillConfig {
    /// <summary>
    /// A class that hold configuration datas for each skillsets
    /// </summary>
    private partial class ConfigData {

      /// <summary>
      /// See <see cref="EDeathCause"/> for length
      /// </summary>
      public static int _deathcause_len = 30;


      /// <summary>
      /// Array of floats to store eventskill_updatesumexp configuration data
      /// </summary>
      public float[] skillevent_exp = new float[(int)ESkillEvent.__len];

      /// <summary>
      /// Array of SkillsetUpdateConfig to store each skillset configuration data
      /// </summary>
      public SkillsetUpdateConfig[] skillupdate_configs = ArrayHelper.InitArrayTClass<SkillsetUpdateConfig>(_skillset_count, (int i) => { return new SkillsetUpdateConfig(); });

      /// <summary>
      /// Determines the interval in second of TickTimer 
      /// </summary>
      public float tickinterval = 0.1f;

      public float autosave_interval = 5f;

      public bool allow_change_skillset_after_change = false;

      public bool recheck_level_if_skillset_changed = true;

      public bool player_levelretain = true;

      public int random_boost_cost = 1;

      public Dictionary<EEnvironment, Dictionary<(EPlayerSpeciality, byte, EModifierType), float>> environment_exp_multiplier = new();

      public Dictionary<(EPlayerSpeciality, byte), float>[] death_cause_muitiplier =
        ArrayHelper.InitArrayTClass(
          _deathcause_len,
          (int i) => {
            return new Dictionary<(EPlayerSpeciality, byte), float>();
          }
        );


      public static EModifierType ParseTo_EModifierType(string key) {
        key = key.ToLower();
        switch(key) {
          case "loss":
            return EModifierType.LOSS;

          case "gain":
            return EModifierType.GAIN;
        }

        return EModifierType.NONE;
      }
    }
  }
}
