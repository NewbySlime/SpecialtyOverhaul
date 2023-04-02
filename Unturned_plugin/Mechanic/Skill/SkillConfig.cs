using Microsoft.Extensions.Configuration;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This class is used for parsing config data from .yaml file and using the data for this plugin
  /// </summary>
  public class SkillConfig {
    public readonly static byte[][] specskill_default_maxlevel = {
      new byte[SpecialtyExpData._skill_offense_count] {
        7,
        7,
        5,
        5,
        5,
        5,
        5,
      },

      new byte[SpecialtyExpData._skill_defense_count] {
        7,
        5,
        5,
        5,
        5,
        5,
        5
      },

      new byte[SpecialtyExpData._skill_support_count] {
        7,
        3,
        5,
        3,
        5,
        7,
        5,
        3
      }
    };

    /// <summary>
    /// Indexer for parsing data from a string that contains name of the specialty or skill to enums
    /// </summary>
    public readonly static Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>> specskill_indexer = new Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>>(){
      {"offense", new KeyValuePair<byte, Dictionary<string, byte>>((byte)EPlayerSpeciality.OFFENSE, new Dictionary<string, byte>(){
        {"overkill", (byte)EPlayerOffense.OVERKILL },
        {"sharpshooter", (byte)EPlayerOffense.SHARPSHOOTER },
        {"dexterity", (byte)EPlayerOffense.DEXTERITY },
        {"cardio", (byte)EPlayerOffense.CARDIO },
        {"exercise", (byte)EPlayerOffense.EXERCISE },
        {"diving", (byte)EPlayerOffense.DIVING },
        {"parkour", (byte)EPlayerOffense.PARKOUR }
      })},

      {"defense", new KeyValuePair<byte, Dictionary<string, byte>>((byte)EPlayerSpeciality.DEFENSE, new Dictionary<string, byte>(){
        {"sneakybeaky", (byte)EPlayerDefense.SNEAKYBEAKY },
        {"vitality", (byte)EPlayerDefense.VITALITY },
        {"immunity", (byte)EPlayerDefense.IMMUNITY },
        {"toughness", (byte)EPlayerDefense.TOUGHNESS },
        {"strength", (byte)EPlayerDefense.STRENGTH },
        {"warmblooded", (byte)EPlayerDefense.WARMBLOODED },
        {"survival", (byte)EPlayerDefense.SURVIVAL }
      })},

      {"support", new KeyValuePair<byte, Dictionary<string, byte>>((byte)EPlayerSpeciality.SUPPORT, new Dictionary<string, byte>(){
        {"healing", (byte)EPlayerSupport.HEALING },
        {"crafting", (byte)EPlayerSupport.CRAFTING },
        {"outdoors", (byte)EPlayerSupport.OUTDOORS },
        {"cooking", (byte)EPlayerSupport.COOKING },
        {"fishing", (byte)EPlayerSupport.FISHING },
        {"agriculture", (byte)EPlayerSupport.AGRICULTURE },
        {"mechanic", (byte)EPlayerSupport.MECHANIC },
        {"engineer", (byte)EPlayerSupport.ENGINEER }
      })}
    };

    /// <summary>
    /// Does the opposite of specskill_indexer, this mainly used for display names
    /// </summary>
    public readonly static Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>> specskill_indexer_inverse = new Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>>(){
      {EPlayerSpeciality.OFFENSE, new KeyValuePair<string, Dictionary<byte, string>>("offense", new Dictionary<byte, string>(){
        {(byte)EPlayerOffense.OVERKILL, "overkill" },
        {(byte)EPlayerOffense.SHARPSHOOTER, "sharpshooter" },
        {(byte)EPlayerOffense.DEXTERITY, "dexterity" },
        {(byte)EPlayerOffense.CARDIO, "cardio" },
        {(byte)EPlayerOffense.EXERCISE, "exercise" },
        {(byte)EPlayerOffense.DIVING, "diving" },
        {(byte)EPlayerOffense.PARKOUR, "parkour" }
      })},

      {EPlayerSpeciality.DEFENSE, new KeyValuePair<string, Dictionary<byte, string>>("defense", new Dictionary<byte, string>(){
        {(byte)EPlayerDefense.SNEAKYBEAKY, "sneakybeaky" },
        {(byte)EPlayerDefense.VITALITY, "vitality" },
        {(byte)EPlayerDefense.IMMUNITY, "immunity" },
        {(byte)EPlayerDefense.TOUGHNESS, "toughness" },
        {(byte)EPlayerDefense.STRENGTH, "strength" },
        {(byte)EPlayerDefense.WARMBLOODED, "warmblooded" },
        {(byte)EPlayerDefense.SURVIVAL, "survival" }
      })},

      {EPlayerSpeciality.SUPPORT, new KeyValuePair<string, Dictionary<byte, string>>("support", new Dictionary<byte, string>(){
        {(byte)EPlayerSupport.HEALING, "healing" },
        {(byte)EPlayerSupport.CRAFTING, "crafting" },
        {(byte)EPlayerSupport.OUTDOORS, "outdoors" },
        {(byte)EPlayerSupport.COOKING, "cooking" },
        {(byte)EPlayerSupport.FISHING, "fishing" },
        {(byte)EPlayerSupport.AGRICULTURE, "agriculture" },
        {(byte)EPlayerSupport.MECHANIC, "mechanic" },
        {(byte)EPlayerSupport.ENGINEER, "engineer" }
      })}
    };

    /// <summary>
    /// Indexer for parsing a string of skillset names to EPlayerSKillset enum
    /// </summary>
    public readonly static Dictionary<string, byte> skillset_indexer = new Dictionary<string, byte>(){
      {"default", (byte)EPlayerSkillset.NONE },
      {"civilian", (byte)EPlayerSkillset.NONE },
      {"fire_fighter", (byte)EPlayerSkillset.FIRE },
      {"police_officer", (byte)EPlayerSkillset.POLICE },
      {"spec_ops", (byte)EPlayerSkillset.ARMY },
      {"farmer", (byte)EPlayerSkillset.FARM },
      {"fisher", (byte)EPlayerSkillset.FISH },
      {"lumberjack", (byte)EPlayerSkillset.CAMP },
      {"worker", (byte)EPlayerSkillset.WORK },
      {"chef", (byte)EPlayerSkillset.CHEF },
      {"thief", (byte)EPlayerSkillset.THIEF },
      {"doctor", (byte)EPlayerSkillset.MEDIC },
      {"admin", 255 }
    };

    /// <summary>
    /// Indexer for parsing a string containing an event name to ESkillEvent enum
    /// </summary>
    private readonly static Dictionary<string, ESkillEvent> skillevent_indexer = new Dictionary<string, ESkillEvent>(){
      // OFFENSE
      {"sharpshooter_shoot_dist_div", ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV },
      {"sharpshooter_shoot_dist_start", ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START },
      {"sharpshooter_shoot_player_crit", ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER_CRIT },
      {"sharpshooter_shoot_player", ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER },
      {"sharpshooter_shoot_zombie_crit", ESkillEvent.SHARPSHOOTER_SHOOT_ZOMBIE_CRIT },
      {"sharpshooter_shoot_zombie", ESkillEvent.SHARPSHOOTER_SHOOT_ZOMBIE },
      {"sharpshooter_shoot_animal", ESkillEvent.SHARPSHOOTER_SHOOT_ANIMAL },
      {"sharpshooter_shoot_tire", ESkillEvent.SHARPSHOOTER_SHOOT_TIRE },
      {"sharpshooter_player_killed_gun", ESkillEvent.SHARPSHOOTER_PLAYER_KILLED_GUN },
      {"sharpshooter_zombie_killed_gun", ESkillEvent.SHARPSHOOTER_ZOMBIE_KILLED_GUN },
      {"sharpshooter_animal_killed_gun", ESkillEvent.SHARPSHOOTER_ANIMAL_KILLED_GUN },

      {"overkill_melee_damage_based", ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED },
      {"overkill_melee_player_crit", ESkillEvent.OVERKILL_MELEE_PLAYER_CRIT },
      {"overkill_melee_player", ESkillEvent.OVERKILL_MELEE_PLAYER },
      {"overkill_melee_zombie_crit", ESkillEvent.OVERKILL_MELEE_ZOMBIE_CRIT },
      {"overkill_melee_zombie", ESkillEvent.OVERKILL_MELEE_ZOMBIE },
      {"overkill_melee_animal", ESkillEvent.OVERKILL_MELEE_ANIMAL },
      {"overkill_player_killed_melee", ESkillEvent.OVERKILL_PLAYER_KILLED_MELEE },
      {"overkill_zombie_killed_melee", ESkillEvent.OVERKILL_ZOMBIE_KILLED_MELEE },
      {"overkill_animal_killed_melee", ESkillEvent.OVERKILL_ANIMAL_KILLED_MELEE },

      {"dexterity_reload_allow_notempty_mags", ESkillEvent.DEXTERITY_RELOAD_ALLOW_NOTEMPTY_MAGS },
      {"dexterity_reload_per_ammo", ESkillEvent.DEXTERITY_RELOAD_PER_AMMO },
      {"dexterity_crafting", ESkillEvent.DEXTERITY_CRAFTING },
      {"dexterity_repairing_vehicle", ESkillEvent.DEXTERITY_REPAIRING_VEHICLE },

      {"cardio_stamina_regen", ESkillEvent.CARDIO_STAMINA_REGEN },
      {"cardio_oxygen_regen", ESkillEvent.CARDIO_OXYGEN_REGEN },

      {"exercise_stamina_use", ESkillEvent.EXERCISE_STAMINA_USE },

      {"diving_oxygen_use", ESkillEvent.DIVING_OXYGEN_USE },
      {"diving_oxygen_use_ifswimming", ESkillEvent.DIVING_OXYGEM_USE_IFSWIMMING },

      {"parkour_stamina_use_sprinting", ESkillEvent.PARKOUR_STAMINA_USE_SPRINTING },

      
      // DEFENSE
      {"sneakybeaky_zombie_max_dist", ESkillEvent.SNEAKYBEAKY_ZOMBIE_MAX_DIST },
      {"sneakybeaky_zombie_dist_div", ESkillEvent.SNEAKYBEAKY_ZOMBIE_DIST_DIV },
      {"sneakybeaky_animal_max_dist", ESkillEvent.SNEAKYBEAKY_ANIMAL_MAX_DIST },
      {"sneakybeaky_animal_dist_div", ESkillEvent.SNEAKYBEAKY_ANIMAL_DIST_DIV },

      {"vitality_maintain_hunger_above", ESkillEvent.VITALITY_MAINTAIN_HUNGER_ABOVE },
      {"vitality_maintain_thirst_above", ESkillEvent.VITALITY_MAINTAIN_THIRST_ABOVE },
      {"vitality_increase_persec_mult", ESkillEvent.VITALITY_INCREASE_PERSEC_MULT },

      {"immunity_virus_increase_mult", ESkillEvent.IMMUNITY_VIRUS_INCREASE_MULT },
      {"immunity_virus_decrease_mult", ESkillEvent.IMMUNITY_VIRUS_DECREASE_MULT },
      {"immunity_mainatain_virus_below", ESkillEvent.IMMUNITY_MAINTAIN_VIRUS_BELOW },
      {"immunity_increase_persec_mult", ESkillEvent.IMMUNITY_INCREASE_PERSEC_MULT },

      {"toughness_health_decrease_mult", ESkillEvent.TOUGHNESS_HEALTH_DECREASE_MULT },
      {"toughness_bleeding", ESkillEvent.TOUGHNESS_BLEEDING },
      {"toughness_fractured", ESkillEvent.TOUGHNESS_FRACTURED },

      {"strength_health_decrease_mult", ESkillEvent.STRENGTH_HEALTH_DECREASE_MULT },
      {"strength_health_decrease_fall_damage_mult", ESkillEvent.STRENGTH_HEALTH_DECREASE_FALL_DAMAGE_MULT },
      {"strength_bleeding", ESkillEvent.STRENGTH_BLEEDING },
      {"strength_fractured", ESkillEvent.STRENGTH_FRACTURED },

      {"warmblooded_on_cold_persec_mult", ESkillEvent.WARMBLOODED_ON_COLD_PERSEC_MULT },
      {"warmblooded_on_freezing_persec_mult", ESkillEvent.WARMBLOODED_ON_FREEZING_PERSEC_MULT },

      {"survival_maintain_hunger_below", ESkillEvent.SURVIVAL_MAINTAIN_HUNGER_BELOW },
      {"survival_maintain_thirst_below", ESkillEvent.SURVIVAL_MAINTAIN_THIRST_BELOW },
      {"survival_increase_persec_mult", ESkillEvent.SURVIVAL_INCREASE_PERSEC_MULT },


      // SUPPORT
      {"healing_health_mult", ESkillEvent.HEALING_HEALTH_MULT },
      {"healing_on_aiding", ESkillEvent.HEALING_ON_AIDING },
      {"healing_crafting", ESkillEvent.HEALING_CRAFTING },

      {"crafting_on_craft", ESkillEvent.CRAFTING_ON_CRAFT },

      {"outdoors_animal_killed", ESkillEvent.OUTDOORS_ANIMAL_KILLED },
      {"outdoors_resource_damage_based", ESkillEvent.OUTDOORS_RESOURCE_DAMAGE_BASED },
      {"outdoors_resource_damaging", ESkillEvent.OUTDOORS_RESOURCE_DAMAGING },

      {"cooking_on_cook", ESkillEvent.COOKING_ON_COOK },

      {"fishing_on_outdoors_skill_mult", ESkillEvent.FISHING_ON_OUTDOORS_SKILL_MULT },

      {"agriculture_onfarm", ESkillEvent.AGRICULTURE_ONFARM },
      {"agriculture_crafting", ESkillEvent.AGRICULTURE_CRAFTING },

      {"mechanic_repair_health", ESkillEvent.MECHANIC_REPAIR_HEALTH },

      {"engineer_repair_health", ESkillEvent.ENGINEER_REPAIR_HEALTH },
      {"engineer_crafting", ESkillEvent.ENGINEER_CRAFTING }
    };

    private readonly IConfiguration configuration;
    private readonly SpecialtyOverhaul plugin;

    private readonly PlayerSkills playerSkills = new PlayerSkills();

    private bool _isConfigLoadProperly = false;
    public bool ConfigLoadProperly {
      get {
        return _isConfigLoadProperly;
      }
    }

    /// <summary>
    /// A class that hold configuration datas for each skillsets
    /// </summary>
    private class config_data {

      /// <summary>
      /// Class containing configuration data
      /// </summary>
      public class skillset_updateconfig {
        public byte[][] _max_level = SpecialtyExpData.InitArrayT<byte>();
        public byte[][] _start_level = SpecialtyExpData.InitArrayT<byte>();
        public int[][] _base_level = SpecialtyExpData.InitArrayT<int>();
        public float[][] _mult_level = SpecialtyExpData.InitArrayT<float>();
        public float[][] _multmult_level = SpecialtyExpData.InitArrayT<float>();
        public float[][] _ondied_edit_level_value = SpecialtyExpData.InitArrayT<float>();
        public EOnDiedEditType[][] _ondied_edit_level_type = SpecialtyExpData.InitArrayT<EOnDiedEditType>();

        public static void CopyData(ref skillset_updateconfig dst, in skillset_updateconfig src) {
          SpecialtyExpData.CopyArrayT<byte>(ref dst._max_level, in src._max_level);
          SpecialtyExpData.CopyArrayT<byte>(ref dst._start_level, in src._start_level);
          SpecialtyExpData.CopyArrayT<int>(ref dst._base_level, in src._base_level);
          SpecialtyExpData.CopyArrayT<float>(ref dst._mult_level, in src._mult_level);
          SpecialtyExpData.CopyArrayT<float>(ref dst._multmult_level, in src._multmult_level);
          SpecialtyExpData.CopyArrayT<float>(ref dst._ondied_edit_level_value, in src._ondied_edit_level_value);
          SpecialtyExpData.CopyArrayT<EOnDiedEditType>(ref dst._ondied_edit_level_type, in src._ondied_edit_level_type);
        }
      }

      /// <summary>
      /// Array of floats to store eventskill_updatesumexp configuration data
      /// </summary>
      public float[] skillevent_exp = new float[(int)ESkillEvent.__len];

      /// <summary>
      /// Array of skillset_updateconfig to store each skillset configuration data
      /// </summary>
      public skillset_updateconfig[] skillupdate_configs = new skillset_updateconfig[12];

      /// <summary>
      /// Determines the interval in second of TickTimer 
      /// </summary>
      public float tickinterval = 0.1f;

      public config_data() {
        for(int i = 0; i < skillupdate_configs.Length; i++) {
          skillupdate_configs[i] = new skillset_updateconfig();
        }
      }
    }
    private config_data config_Data;

    /// <summary>
    /// Determines how the plugin should decrease certain skill exp
    ///  - "offset"
    ///    The value is how many exp to decrement from Player's Specialty exp value
    ///  - "mult"
    ///    The value is a multiplier to get the end value
    ///  - "base"
    ///    The value is a multiplier, to multiply base_level_exp using the same calculation to determine next level value in order to get decrement value
    /// </summary>
    public enum EOnDiedEditType {
      OFFSET,
      MULT,
      BASE
    }

    /// <summary>
    /// Enum for what type of skill event
    /// </summary>
    public enum ESkillEvent {
      SHARPSHOOTER_SHOOT_DIST_DIV,
      SHARPSHOOTER_SHOOT_DIST_START,
      SHARPSHOOTER_SHOOT_PLAYER_CRIT,
      SHARPSHOOTER_SHOOT_PLAYER,
      SHARPSHOOTER_SHOOT_ZOMBIE_CRIT,
      SHARPSHOOTER_SHOOT_ZOMBIE,
      SHARPSHOOTER_SHOOT_ANIMAL,
      SHARPSHOOTER_SHOOT_TIRE,
      SHARPSHOOTER_PLAYER_KILLED_GUN,
      SHARPSHOOTER_ANIMAL_KILLED_GUN,
      SHARPSHOOTER_ZOMBIE_KILLED_GUN,

      OVERKILL_MELEE_DAMAGE_BASED,
      OVERKILL_MELEE_PLAYER_CRIT,
      OVERKILL_MELEE_PLAYER,
      OVERKILL_MELEE_ZOMBIE_CRIT,
      OVERKILL_MELEE_ZOMBIE,
      OVERKILL_MELEE_ANIMAL,
      OVERKILL_PLAYER_KILLED_MELEE,
      OVERKILL_ANIMAL_KILLED_MELEE,
      OVERKILL_ZOMBIE_KILLED_MELEE,

      DEXTERITY_RELOAD_ALLOW_NOTEMPTY_MAGS,
      DEXTERITY_RELOAD_PER_AMMO,
      DEXTERITY_CRAFTING,
      DEXTERITY_REPAIRING_VEHICLE,

      CARDIO_STAMINA_REGEN,
      CARDIO_OXYGEN_REGEN,

      EXERCISE_STAMINA_USE,

      DIVING_OXYGEN_USE,
      DIVING_OXYGEM_USE_IFSWIMMING,

      PARKOUR_STAMINA_USE_SPRINTING,

      // DEFENSE
      SNEAKYBEAKY_ZOMBIE_MAX_DIST,
      SNEAKYBEAKY_ZOMBIE_DIST_DIV,
      SNEAKYBEAKY_ANIMAL_MAX_DIST,
      SNEAKYBEAKY_ANIMAL_DIST_DIV,

      VITALITY_MAINTAIN_HUNGER_ABOVE,
      VITALITY_MAINTAIN_THIRST_ABOVE,
      VITALITY_INCREASE_PERSEC_MULT,

      IMMUNITY_VIRUS_INCREASE_MULT,
      IMMUNITY_VIRUS_DECREASE_MULT,
      IMMUNITY_MAINTAIN_VIRUS_BELOW,
      IMMUNITY_INCREASE_PERSEC_MULT,

      TOUGHNESS_HEALTH_DECREASE_MULT,
      TOUGHNESS_BLEEDING,
      TOUGHNESS_FRACTURED,

      STRENGTH_HEALTH_DECREASE_MULT,
      STRENGTH_HEALTH_DECREASE_FALL_DAMAGE_MULT,
      STRENGTH_BLEEDING,
      STRENGTH_FRACTURED,

      WARMBLOODED_ON_COLD_PERSEC_MULT,
      WARMBLOODED_ON_FREEZING_PERSEC_MULT,

      SURVIVAL_MAINTAIN_HUNGER_BELOW,
      SURVIVAL_MAINTAIN_THIRST_BELOW,
      SURVIVAL_INCREASE_PERSEC_MULT,

      // SUPPORT
      HEALING_HEALTH_MULT,
      HEALING_ON_AIDING,
      HEALING_CRAFTING,

      CRAFTING_ON_CRAFT,

      OUTDOORS_ANIMAL_KILLED,
      OUTDOORS_RESOURCE_DAMAGE_BASED,
      OUTDOORS_RESOURCE_DAMAGING,

      COOKING_ON_COOK,

      FISHING_ON_OUTDOORS_SKILL_MULT,

      AGRICULTURE_ONFARM,
      AGRICULTURE_CRAFTING,

      MECHANIC_REPAIR_HEALTH,

      ENGINEER_REPAIR_HEALTH,
      ENGINEER_CRAFTING,

      __len
    }

    /// <summary>
    /// A custom Exception class that usually thrown when there's a problem when parsing config data
    /// </summary>
    private class ErrorSettingUpConfig: Exception {
      public ErrorSettingUpConfig(string what) : base(what) { }
    }

    /// <summary>
    /// Parsing a sublists of configuration data that only contains "specialty.skill"
    /// </summary>
    /// <typeparam name="T">The type of the sublists</typeparam>
    /// <param name="section">The sublists data</param>
    /// <param name="values">2D array reference for holding specialty-skill datas. The type of array that comes from <see cref="config_data.skillset_updateconfig"/></param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    /// <param name="on_notfound">If the parameter not found from the data</param>
    /// <exception cref="ErrorSettingUpConfig"></exception>
    private void _process_configdata_copytoarray<T>(IConfigurationSection section, ref T[][] values, bool create_error, Action<byte, byte>? on_notfound = null) {
      var _iter = section.GetChildren();
      const int _supposedlen = SpecialtyExpData._skill_offense_count + SpecialtyExpData._skill_defense_count + SpecialtyExpData._skill_support_count;
      int _assigned = 0;

      bool[][] _btmp = SpecialtyExpData.InitArrayT<bool>();
      foreach(var child in _iter) {
        string[] keys = child.Key.Split('.');
        if(specskill_indexer.ContainsKey(keys[0]) && keys.Length == 2) {
          var skill_indexer = specskill_indexer[keys[0]];
          if(skill_indexer.Value.ContainsKey(keys[1])) {
            int skill_idx = skill_indexer.Value[keys[1]];
            values[skill_indexer.Key][skill_idx] = child.Get<T>();
            _btmp[skill_indexer.Key][skill_idx] = true;

            _assigned++;
          }
          else if(create_error)
            throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
        }
        else if(create_error)
          throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
      }

      if(_assigned < _supposedlen && create_error)
        throw new ErrorSettingUpConfig(string.Format("The value(s) isn't sufficient enough to fill {0}.", section.Key));

      if(on_notfound != null) {
        for(byte i = 0; i < _btmp.Length; i++) {
          for(byte o = 0; o < _btmp[i].Length; o++) {
            if(!_btmp[i][o])
              on_notfound.Invoke(i, o);
          }
        }
      }
    }

    /// <summary>
    /// Parsing a sublists for ondied_edit_level_exp configuration data
    /// </summary>
    /// <param name="section">The sublists data</param>
    /// <param name="skillset_data">Current skillset data</param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    /// <exception cref="ErrorSettingUpConfig"></exception>
    private void _process_configdata_copytoarray_ondied_edit(IConfigurationSection section, ref config_data.skillset_updateconfig skillset_data, bool create_error) {
      var _iter = section.GetChildren();
      const int _supposedlen = (SpecialtyExpData._skill_offense_count + SpecialtyExpData._skill_defense_count + SpecialtyExpData._skill_support_count) * 2;
      int _assigned = 0;

      foreach(var child in _iter) {
        string[] keys = child.Key.Split('.');
        if(specskill_indexer.ContainsKey(keys[0]) && keys.Length == 3) {
          var skill_indexer = specskill_indexer[keys[0]];
          if(skill_indexer.Value.ContainsKey(keys[1])) {
            int skill_idx = skill_indexer.Value[keys[1]];
            switch(keys[2]) {
              case "value":
                skillset_data._ondied_edit_level_value[skill_indexer.Key][skill_idx] = child.Get<float>();
                break;

              case "type": {
                EOnDiedEditType type = EOnDiedEditType.OFFSET;
                switch(child.Value) {
                  case "offset":
                    type = EOnDiedEditType.OFFSET;
                    break;

                  case "mult":
                    type = EOnDiedEditType.MULT;
                    break;

                  case "base":
                    type = EOnDiedEditType.BASE;
                    break;
                }

                skillset_data._ondied_edit_level_type[skill_indexer.Key][skill_idx] = type;
              }
              break;

              default:
                if(create_error)
                  throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
                break;
            }

            _assigned++;
          }
          else if(create_error)
            throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
        }
        else if(create_error)
          throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
      }

      if(_assigned < _supposedlen && create_error)
        throw new ErrorSettingUpConfig(string.Format("The value(s) isn't sufficient enough to fill {0}.", section.Key));
    }

    /// <summary>
    /// Parsing sublists of configuration data that contains what normally a <see cref="config_data.skillset_updateconfig"/> contains. The function used for each skillsets
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="section">The sublists data</param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    private void _process_configdata_skillset(EPlayerSkillset skillset, IConfigurationSection section, bool create_error = false, bool usedefaultvalue = false) {
      var skillset_data = config_Data.skillupdate_configs[(int)skillset];
      Action<byte, byte>? action_notfound = null;

      // max_level
      if(usedefaultvalue)
        action_notfound = (byte spec, byte idx) => {
          skillset_data._max_level[spec][idx] = byte.MaxValue;
        };
      else
        action_notfound = null;

      _process_configdata_copytoarray<byte>(section.GetSection("max_level"), ref skillset_data._max_level, false, action_notfound);
      SpecialtyExpData.IterateArrayT<byte>((EPlayerSpeciality spec, byte skill) => {
        byte defmaxlevel = specskill_default_maxlevel[(byte)spec][skill];
        if(skillset_data._max_level[(byte)spec][skill] > defmaxlevel)
          skillset_data._max_level[(byte)spec][skill] = defmaxlevel; 
      });


      // start_level
      if(usedefaultvalue)
        action_notfound = (byte spec, byte idx) => {
          skillset_data._start_level[spec][idx] = byte.MinValue;
        };
      else
        action_notfound = null;

      _process_configdata_copytoarray<byte>(section.GetSection("start_level"), ref skillset_data._start_level, false, action_notfound);


      // base_level_exp
      _process_configdata_copytoarray<int>(section.GetSection("base_level_exp"), ref skillset_data._base_level, create_error);
      
      // mult_level_exp
      _process_configdata_copytoarray<float>(section.GetSection("mult_level_exp"), ref skillset_data._mult_level, create_error);

      // multmult_level_exp
      if(usedefaultvalue)
        action_notfound = (byte spec, byte idx) => {
          skillset_data._multmult_level[spec][idx] = 1.0f;
        };
      else
        action_notfound = null;

      _process_configdata_copytoarray<float>(section.GetSection("multmult_level_exp"), ref skillset_data._multmult_level, false, action_notfound);


      // ondied_edit_level_exp
      _process_configdata_copytoarray_ondied_edit(section.GetSection("ondied_edit_level_exp"), ref skillset_data, create_error);
    }

    /// <summary>
    /// Parsing data for skillset_config configuration data
    /// </summary>
    /// <param name="section">The sublists data</param>
    private void _process_configdata_skillsets(IConfigurationSection section) {
      // process default data first
      _process_configdata_skillset(EPlayerSkillset.NONE, section.GetSection("default"), true, true);

      foreach(var pair in skillset_indexer) {
        byte _currskillset = pair.Value;
        if((EPlayerSkillset)_currskillset == EPlayerSkillset.NONE)
          continue;

        if((byte)_currskillset == 255)
          _currskillset = (byte)(config_Data.skillupdate_configs.Length - 1);

        config_data.skillset_updateconfig.CopyData(ref config_Data.skillupdate_configs[_currskillset], in config_Data.skillupdate_configs[(int)EPlayerSkillset.NONE]);
        _process_configdata_skillset((EPlayerSkillset)_currskillset, section.GetSection(pair.Key));
      }
    }

    /// <summary>
    /// Parsing data for eventskill_udpatesumexp configuration data
    /// </summary>
    /// <param name="section"></param>
    private void _process_configdata_eventskill(IConfigurationSection section) {
      var _iter = section.GetChildren();

      var _tmp_dict = new Dictionary<string, ESkillEvent>(skillevent_indexer);
      foreach(var child in _iter) {
        if(skillevent_indexer.ContainsKey(child.Key)) {
          config_Data.skillevent_exp[(int)skillevent_indexer[child.Key]] = child.Get<float>();
          _tmp_dict.Remove(child.Key);
        }
        else
          SpecialtyOverhaul.Instance?.PrintToError(string.Format("Parameter {0} is invalid.", child.Key));
      }

      foreach(var child in _tmp_dict) {
        SpecialtyOverhaul.Instance?.PrintWarning(string.Format("Parameter {0} isn't available. The parameter will be set to 0.", child.Key));
        config_Data.skillevent_exp[(int)skillevent_indexer[child.Key]] = 0.0f;
      }
    }


    /// <param name="plugin">Current plugin object</param>
    /// <param name="configuration">Interface that handles configuration file data</param>
    public SkillConfig(SpecialtyOverhaul plugin, IConfiguration configuration) {
      this.configuration = configuration;
      this.plugin = plugin;

      config_Data = new config_data();
    }

    /// <summary>
    /// For re-reading configuration data. Used when .yaml config data has been edited
    /// </summary>
    /// <returns>Returns true if the configuration load properly</returns>
    public bool RefreshConfig() {
      try {
        // skillset_config
        _process_configdata_skillsets(configuration.GetSection("skillset_config"));

        // eventskill_updatesumexp
        _process_configdata_eventskill(configuration.GetSection("eventskill_updatesumexp"));

        // tick_interval
        config_Data.tickinterval = configuration.GetValue<float>("tick_interval", 0.3f);

        _isConfigLoadProperly = true;
      }
      catch(ErrorSettingUpConfig e) {
        plugin.PrintToError("Error occured when setting up default config");
        plugin.PrintToError(e.ToString());
        _isConfigLoadProperly = false;
      }

      return _isConfigLoadProperly;
    }

    /// <summary>
    /// Getting certain skill max level
    /// </summary>
    /// <param name="player">Current player</param>
    /// <param name="skillset">Player skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns></returns>
    public byte GetMaxLevel(Player player, EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      byte _playermax = player.skills.skills[(byte)spec][idx].max;
      byte _currentmax = config_Data.skillupdate_configs[(byte)skillset]._max_level[(byte)spec][idx];
      if(_currentmax > _playermax)
        return _playermax;
      else
        return _currentmax;
    }
    
    /// <summary>
    /// Getting certain skill starting level
    /// </summary>
    /// <param name="skillset">Player skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns></returns>
    public byte GetStartLevel(Player player, EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._start_level[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain base level exp
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns>The max level</returns>
    public int GetBaseLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._base_level[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain skill multiplier
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns>The skill multiplier</returns>
    public float GetMultLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._mult_level[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain skill multmult (power)
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns>The skill multmult (power)</returns>
    public float GetMultMultLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._multmult_level[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain value of ondied on certain skill
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns>The OnDied value</returns>
    public float GetOnDiedValue(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._ondied_edit_level_value[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain type of ondied on certain skill
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns>The OnDied type</returns>
    public EOnDiedEditType GetOnDiedType(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      return config_Data.skillupdate_configs[(int)skillset]._ondied_edit_level_type[(int)spec][idx];
    }

    /// <summary>
    /// Getting certain event update
    /// </summary>
    /// <param name="skillevent">What skill event</param>
    /// <returns>A value of the skill event</returns>
    public float GetEventUpdate(ESkillEvent skillevent) {
      return config_Data.skillevent_exp[(int)skillevent];
    }

    /// <summary>
    /// Getting tick interval in seconds
    /// </summary>
    /// <returns>Tick interval in seconds</returns>
    public float GetTickInterval() {
      return config_Data.tickinterval;
    }
  }
}
