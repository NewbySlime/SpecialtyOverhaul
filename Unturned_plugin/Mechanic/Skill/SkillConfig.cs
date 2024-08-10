using Microsoft.Extensions.Configuration;
using Nekos.SpecialtyPlugin.Commands;
using Nekos.SpecialtyPlugin.Error;
using Nekos.SpecialtyPlugin.Error.Helper;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers;
using Nekos.SpecialtyPlugin.Misc;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This class is used for parsing config data from .yaml file and using the data for this plugin
  /// </summary>
  public partial class SkillConfig {
    private class ParserException: Exception {
      private readonly ErrorType _errorType;
      public ErrorType errorType {
        get {
          return _errorType;
        }
      }
      
      public enum ErrorType {
        KEYS_INSUFFICIENT,

        SPECIALTY_INVALID,
        SKILL_INVALID,
      }


      public ParserException(ErrorType errorType) {
        _errorType = errorType;
      }
    }

    private struct ParamResult_parseToSpecialtySkill {
      public EPlayerSpeciality spec;
      public byte skill_idx;

      public bool _isSpec_wildcard;
      public bool _isSkill_wildcard;

      public string nextParam;
    }


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



    private readonly static Func<Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>>> specskill_indexer_init = () => {
      var _res = new Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>>();

      var _specs = IndexerHelper.CreateIndexerByEnum<EPlayerSpeciality>();
      foreach(var spec in _specs) {
        Dictionary<string, byte> _currentIndexer;

        switch(spec.Value) {
          case EPlayerSpeciality.OFFENSE:
            _currentIndexer = IndexerHelper.CreateIndexerByEnum_ParseToByte<EPlayerOffense>();
            break;

          case EPlayerSpeciality.DEFENSE:
            _currentIndexer = IndexerHelper.CreateIndexerByEnum_ParseToByte<EPlayerDefense>();
            break;

          case EPlayerSpeciality.SUPPORT:
            _currentIndexer = IndexerHelper.CreateIndexerByEnum_ParseToByte<EPlayerSupport>();
            break;

          default:
            continue;
        }

        _res[spec.Key] = new KeyValuePair<byte, Dictionary<string, byte>>((byte)spec.Value, _currentIndexer);
      }

      return _res;
    };

    /// <summary>
    /// Indexer for parsing data from a string that contains name of the specialty or skill to enums
    /// </summary>
    public readonly static Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>> specskill_indexer = specskill_indexer_init();



    private readonly static Func<Dictionary<string, (EPlayerSpeciality, byte)>> skill_indexer_init = () => {
      var _res = new Dictionary<string, (EPlayerSpeciality, byte)>();

      foreach(var spec in specskill_indexer) 
        foreach(var skill in spec.Value.Value) 
          _res[skill.Key] = ((EPlayerSpeciality)spec.Value.Key, skill.Value);

      return _res;
    };

    public readonly static Dictionary<string, (EPlayerSpeciality, byte)> skill_indexer = skill_indexer_init();


    /// <summary>
    /// Does the opposite of specskill_indexer, this mainly used for display names
    /// </summary>
    public readonly static Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>> specskill_indexer_inverse = new Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>>(){
      {EPlayerSpeciality.OFFENSE, new KeyValuePair<string, Dictionary<byte, string>>("Offense", new Dictionary<byte, string>(){
        {(byte)EPlayerOffense.OVERKILL, "Overkill" },
        {(byte)EPlayerOffense.SHARPSHOOTER, "Sharpshooter" },
        {(byte)EPlayerOffense.DEXTERITY, "Dexterity" },
        {(byte)EPlayerOffense.CARDIO, "Cardio" },
        {(byte)EPlayerOffense.EXERCISE, "Exercise" },
        {(byte)EPlayerOffense.DIVING, "Diving" },
        {(byte)EPlayerOffense.PARKOUR, "Parkour" }
      })},

      {EPlayerSpeciality.DEFENSE, new KeyValuePair<string, Dictionary<byte, string>>("Defense", new Dictionary<byte, string>(){
        {(byte)EPlayerDefense.SNEAKYBEAKY, "Sneakybeaky" },
        {(byte)EPlayerDefense.VITALITY, "Vitality" },
        {(byte)EPlayerDefense.IMMUNITY, "Immunity" },
        {(byte)EPlayerDefense.TOUGHNESS, "Toughness" },
        {(byte)EPlayerDefense.STRENGTH, "Strength" },
        {(byte)EPlayerDefense.WARMBLOODED, "Warmblooded" },
        {(byte)EPlayerDefense.SURVIVAL, "Survival" }
      })},

      {EPlayerSpeciality.SUPPORT, new KeyValuePair<string, Dictionary<byte, string>>("Support", new Dictionary<byte, string>(){
        {(byte)EPlayerSupport.HEALING, "Healing" },
        {(byte)EPlayerSupport.CRAFTING, "Crafting" },
        {(byte)EPlayerSupport.OUTDOORS, "Outdoors" },
        {(byte)EPlayerSupport.COOKING, "Cooking" },
        {(byte)EPlayerSupport.FISHING, "Fishing" },
        {(byte)EPlayerSupport.AGRICULTURE, "Agriculture" },
        {(byte)EPlayerSupport.MECHANIC, "Mechanic" },
        {(byte)EPlayerSupport.ENGINEER, "Engineer" }
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
      {"doctor", (byte)EPlayerSkillset.MEDIC }
    };

    /// <summary>
    /// This indexer is used for getting display names for each skillset
    /// </summary>
    public readonly static Dictionary<byte, string> skillset_indexer_inverse = new Dictionary<byte, string>() {
      {(byte)EPlayerSkillset.NONE, "Civilian" },
      {(byte)EPlayerSkillset.FIRE, "Fire Fighter" },
      {(byte)EPlayerSkillset.POLICE, "Police Officer" },
      {(byte)EPlayerSkillset.ARMY, "Spec Ops" },
      {(byte)EPlayerSkillset.FARM, "Farmer" },
      {(byte)EPlayerSkillset.FISH, "Fisherman" },
      {(byte)EPlayerSkillset.CAMP, "Lumberjack" },
      {(byte)EPlayerSkillset.WORK, "Worker" },
      {(byte)EPlayerSkillset.CHEF, "Chef" },
      {(byte)EPlayerSkillset.THIEF, "Thief" },
      {(byte)EPlayerSkillset.MEDIC, "Doctor" }
    };

    /// <summary>
    /// Indexer for parsing a string containing a name for <see cref="ESkillEvent"/>
    /// </summary>
    private readonly static Dictionary<string, ESkillEvent> skillevent_indexer = IndexerHelper.CreateIndexerByEnum<ESkillEvent>(new HashSet<ESkillEvent>() { ESkillEvent.__len });

    /// <summary>
    /// Indexer for parsing a string containing a name for <see cref="EDeathCause"/>
    /// </summary>
    public readonly static Dictionary<string, EDeathCause> deathcause_indexer = IndexerHelper.CreateIndexerByEnum<EDeathCause>();

    /// <summary>
    /// Indexer for parsing a string containing a name for <see cref="EEnvironment"/>
    /// </summary>
    public readonly static Dictionary<string, EEnvironment> environment_indexer = IndexerHelper.CreateIndexerByEnum<EEnvironment>(new HashSet<EEnvironment>() { EEnvironment._temp_flags, EEnvironment._time_flags });


    private readonly IConfiguration configuration;
    private readonly SpecialtyOverhaul plugin;

    private readonly static char _separator_char = '.';
    private readonly static string _wildcard_string = "_all";

    private readonly static bool _default_playersAllowRetainLevel = true;
    private readonly static bool _default_isDemotable = false;
    private readonly static float _default_tickInterval = 0.3f;
    private readonly static float _default_autosaveInterval = 180f;
    private readonly static float _default_randomBoostCost = 3.0f;
    private readonly static float _default_multMultLevelExp = 1.0f;
    private readonly static int _default_excessExpIncrement = 1;
    private readonly static float _default_allowChangeSkillsetAfterChange = 0;
    private readonly static float _default_recheckLevelIfSkillsetChanged = 1;

    private ConfigData config_Data;

    public readonly static int _skillset_count = 11;
    public readonly ICalculationUtils Calculation;


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
      NONE,
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

      //DEXTERITY_RELOAD_ALLOW_NOTEMPTY_MAGS,
      DEXTERITY_RELOAD_PER_AMMO,
      DEXTERITY_RELOAD_MULT_CONSTANT,
      DEXTERITY_RELOAD_ADAPTABLE_COOLDOWN,
      DEXTERITY_RELOAD_MAXAMMO_PER_MINUTE,
      DEXTERITY_CRAFTING,
      DEXTERITY_REPAIRING_VEHICLE,

      CARDIO_STAMINA_REGEN,
      CARDIO_OXYGEN_REGEN,

      EXERCISE_STAMINA_USE,

      DIVING_OXYGEN_USE,
      DIVING_OXYGEN_USE_IFSWIMMING,

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
    /// Parsing value in string to <see cref="EOnDiedEditType"/>
    /// </summary>
    /// <param name="value">The value in string</param>
    /// <returns>Returns <see cref="EOnDiedEditType"/></returns>
    private static EOnDiedEditType _parseString_to_eOnDiedEditType(string value) {
      switch(value.ToLower()) {
        case "offset":
          return EOnDiedEditType.OFFSET;

        case "mult":
          return EOnDiedEditType.MULT;

        case "base":
          return EOnDiedEditType.BASE;
      }

      return EOnDiedEditType.NONE;
    }

    /// <summary>
    /// Getting param name based on the <see cref="EPlayerSpeciality"/> and skill index
    /// </summary>
    /// <param name="spec">Current specialty</param>
    /// <param name="skill_idx">Current skill index</param>
    /// <returns>The name of the parameter</returns>
    private static string _getKeyParamName(EPlayerSpeciality spec, byte skill_idx) {
      string _skill_str = "";
      switch(spec) {
        case EPlayerSpeciality.OFFENSE:
          _skill_str = IndexerHelper.GetIndexerEnumName((EPlayerOffense)skill_idx);
          break;

        case EPlayerSpeciality.DEFENSE:
          _skill_str = IndexerHelper.GetIndexerEnumName((EPlayerDefense)skill_idx);
          break;

        case EPlayerSpeciality.SUPPORT:
          _skill_str = IndexerHelper.GetIndexerEnumName((EPlayerSupport)skill_idx);
          break;
      }

      return string.Format("{0}{1}{2}", IndexerHelper.GetIndexerEnumName(spec), _separator_char, _skill_str);
    }

    /// <summary>
    /// Parsing parameter name to more program-readable
    /// </summary>
    /// <param name="param">The parameter name</param>
    /// <returns>The program-readable result</returns>
    /// <exception cref="ParserException"></exception>
    private static ParamResult_parseToSpecialtySkill _parseToSpecialtySkill(string param) {
      param = param.ToLower();

      ParamResult_parseToSpecialtySkill result = new ParamResult_parseToSpecialtySkill() {
        nextParam = "",
        _isSpec_wildcard = false,
        _isSkill_wildcard = false
      };

      int _keys_used = 0;

      string[] _keys = param.Split(_separator_char);

      if(_keys[0] == _wildcard_string) {
        result._isSpec_wildcard = true;
        _keys_used++;
      }
      else if(_keys.Length >= 2) {
        if(specskill_indexer.TryGetValue(_keys[0], out var _skill_indexer)) {
          result.spec = (EPlayerSpeciality)_skill_indexer.Key;
          _keys_used++;

          if(_keys[1] == _wildcard_string) {
            result._isSkill_wildcard = true;
            _keys_used++;
          }
          else if(_skill_indexer.Value.TryGetValue(_keys[1], out var _skill_idx)) {
            result.skill_idx = _skill_idx;
            _keys_used++;
          }
          else
            throw new ParserException(ParserException.ErrorType.SKILL_INVALID);
        }
        else
          throw new ParserException(ParserException.ErrorType.SPECIALTY_INVALID);
      }
      else
        throw new ParserException(ParserException.ErrorType.KEYS_INSUFFICIENT);


      result.nextParam = "";
      for(; _keys_used < _keys.Length; _keys_used++) {
        result.nextParam += _keys[_keys_used];
        if(_keys_used < (_keys.Length - 1))
          result.nextParam += _separator_char;
      }

      return result;
    }


    /// <summary>
    /// This used for handling (giving outputs for certain types in <see cref="ParserException.ErrorType"/>) errors
    /// </summary>
    /// <param name="error">The error thrown</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    /// <param name="output">Callback for when this function give messages</param>
    private static void _handle_ParserException(ParserException error, NestedParameterData paramName, Action<string> output) {
      switch(error.errorType) {
        case ParserException.ErrorType.KEYS_INSUFFICIENT:
          output.Invoke(paramName.ToString("Key is insufficient to determine what skill it is."));
          break;

        case ParserException.ErrorType.SPECIALTY_INVALID:
          output.Invoke(paramName.ToString("Specialty name is invalid."));
          break;

        case ParserException.ErrorType.SKILL_INVALID:
          output.Invoke(paramName.ToString("Skill name is invalid."));
          break;
      }
    }


    /// <summary>
    /// Parsing a sublists of configuration data that only contains "specialty.skill"
    /// </summary>
    /// <typeparam name="T">The type of the sublists</typeparam>
    /// <param name="section">The sublists data</param>
    /// <param name="values">2D array reference for holding specialty-skill datas. The type of array that comes from <see cref="ConfigData.SkillsetUpdateConfig"/></param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    /// <param name="on_notfound">If the parameter not found from the data</param>
    /// <exception cref="ErrorSettingUpConfig"></exception>
    private void _process_configdata_copytoarray<T>(IConfigurationSection section, T[][] values, bool create_error, NestedParameterData paramName, Action<byte, byte>? on_notfound = null) {
      TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
      if(converter != null) {
        Action<string> _on_error = (string _errmsg) => {
          if(create_error)
            throw new ErrorSettingUpConfig(_errmsg);
          else
            plugin.PrintWarning(_errmsg);
        };


        Dictionary<(EPlayerSpeciality, byte), T> _currentValues = new Dictionary<(EPlayerSpeciality, byte), T>();
        HashSet<(EPlayerSpeciality, byte)> _remainingValues = new HashSet<(EPlayerSpeciality, byte)>(skill_indexer.Values);

        foreach(var skill in section.GetChildren()) {
          var paramName_skill = paramName.Nest(skill.Key);
          if(skill.Exists()) {
            try {
              var _param_result = _parseToSpecialtySkill(skill.Key);
              bool _continue_parsing = true;

              T? val = default;
              try {
                val = (T)converter.ConvertFromString(skill.Value);
              }
              catch(Exception) {
                _on_error.Invoke(paramName_skill.ToString(string.Format("Cannot parse value to {0}.", typeof(T).Name)));
                _continue_parsing = false;
              }

              if(_continue_parsing && val != null) {
                if(_param_result._isSpec_wildcard) {
                  SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                    values[(byte)spec][skill_idx] = val;
                  });

                  _remainingValues.Clear();
                }
                else if(_param_result._isSkill_wildcard) {
                  SpecialtyExpData.IterateArray(_param_result.spec, (byte skill_idx) => {
                    values[(byte)_param_result.spec][skill_idx] = val;
                    _remainingValues.Remove((_param_result.spec, skill_idx));
                  });
                }
                else {
                  if(_currentValues.ContainsKey((_param_result.spec, _param_result.skill_idx)))
                    plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

                  _currentValues[(_param_result.spec, _param_result.skill_idx)] = val;
                  _remainingValues.Remove((_param_result.spec, _param_result.skill_idx));
                }
              }
            }
            catch(ParserException e) {
              _handle_ParserException(e, paramName_skill, _on_error);
            }
          }
          else
            _on_error.Invoke(paramName_skill.ToString("Value is empty."));
        }


        if(_remainingValues.Count > 0) {
          if(create_error) {
            plugin.PrintToError(paramName.ToString("Following key are missing;"));
            foreach(var specskill in _remainingValues)
              plugin.PrintToError(string.Format("Key skill: {0}", _getKeyParamName(specskill.Item1, specskill.Item2)));

            throw new ErrorSettingUpConfig(paramName.ToString("The value(s) isn't sufficient enough."));
          }

          if(on_notfound != null)
            foreach(var specskill in _remainingValues)
              on_notfound.Invoke((byte)specskill.Item1, specskill.Item2);
        }

        
        foreach(var specskill in _currentValues)
          values[(byte)specskill.Key.Item1][specskill.Key.Item2] = specskill.Value;
      }
      else
        throw new InvalidCastException(paramName.ToString(string.Format("Converter of type '{0}' could not be found.", typeof(T).ToString())));
    }

    /// <summary>
    /// Parsing a sublists for ondied_edit_level_exp configuration data
    /// </summary>
    /// <param name="section">The sublists data</param>
    /// <param name="skillset_data">Current skillset data</param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    /// <exception cref="ErrorSettingUpConfig"></exception>
    private void _process_configdata_copytoarray_ondied_edit(IConfigurationSection section, ConfigData.SkillsetUpdateConfig skillset_data, bool create_error, NestedParameterData paramName) {
      Action<string> _on_error = (string _errmsg) => {
        if(create_error)
          throw new ErrorSettingUpConfig(_errmsg);
        else
          plugin.PrintWarning(_errmsg);
      };
      

      Dictionary<(EPlayerSpeciality, byte), float> _currentValues = new Dictionary<(EPlayerSpeciality, byte), float>();
      Dictionary<(EPlayerSpeciality, byte), EOnDiedEditType> _currentTypes = new Dictionary<(EPlayerSpeciality, byte), EOnDiedEditType>();
      HashSet<(EPlayerSpeciality, byte)>
        _remainingValues = new HashSet<(EPlayerSpeciality, byte)>(skill_indexer.Values),
        _remainingTypes = new HashSet<(EPlayerSpeciality, byte)>(skill_indexer.Values)
      ;

      foreach(var skill in section.GetChildren()) {
        var paramName_skill = paramName.Nest(skill.Key);
        if(skill.Exists()) {
          try {
            var _param_result = _parseToSpecialtySkill(skill.Key);

            if(string.IsNullOrEmpty(_param_result.nextParam)) {
              _on_error.Invoke(paramName_skill.ToString("SubType not defined in the key."));
              continue;
            }

            string[] _keys = _param_result.nextParam.Split(_separator_char);
            if(_keys.Length > 1) {
              _on_error.Invoke(paramName_skill.ToString("Key length is too long."));
              continue;
            }


            switch(_keys[0]) {
              case "type": {
                var _type = _parseString_to_eOnDiedEditType(skill.Value);
                if(_type != EOnDiedEditType.NONE) {
                  if(_param_result._isSpec_wildcard) {
                    SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                      skillset_data._ondied_edit_level_type[(byte)spec][skill_idx] = _type;
                    });

                    _remainingTypes.Clear();
                  }
                  else if(_param_result._isSkill_wildcard) {
                    SpecialtyExpData.IterateArray(_param_result.spec, (byte skill_idx) => {
                      skillset_data._ondied_edit_level_type[(byte)_param_result.spec][skill_idx] = _type;
                      _remainingTypes.Remove((_param_result.spec, skill_idx));
                    });
                  }
                  else {
                    if(_currentTypes.ContainsKey((_param_result.spec, _param_result.skill_idx)))
                      plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

                    _currentTypes[(_param_result.spec, _param_result.skill_idx)] = _type;
                    _remainingTypes.Remove((_param_result.spec, _param_result.skill_idx));
                  }
                }
                else {
                  _on_error.Invoke(paramName_skill.ToString("The type is invalid."));
                  continue;
                }

                break;
              }

              case "value": {
                if(float.TryParse(skill.Value, out float _val)) {
                  if(_param_result._isSpec_wildcard) {
                    SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                      skillset_data._ondied_edit_level_value[(byte)spec][skill_idx] = _val;
                    });

                    _remainingValues.Clear();
                  }
                  else if(_param_result._isSkill_wildcard) {
                    SpecialtyExpData.IterateArray(_param_result.spec, (byte skill_idx) => {
                      skillset_data._ondied_edit_level_value[(byte)_param_result.spec][skill_idx] = _val;
                      _remainingValues.Remove((_param_result.spec, skill_idx));
                    });
                  }
                  else {
                    if(_currentValues.ContainsKey((_param_result.spec, _param_result.skill_idx)))
                      plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

                    _currentValues[(_param_result.spec, _param_result.skill_idx)] = _val;
                    _remainingValues.Remove((_param_result.spec, _param_result.skill_idx));
                  }
                }
                else {
                  _on_error.Invoke(paramName_skill.ToString("Value cannot be parsed to Float."));
                  continue;
                }
              
                break;
              }

              default:
                _on_error.Invoke(paramName_skill.ToString("SubType of the value is invalid."));
                continue;
            }
          }
          catch(ParserException e) {
            _handle_ParserException(e, paramName_skill, _on_error);
          }
        }
        else
          _on_error.Invoke(paramName_skill.ToString("Value is empty."));
      }


      if(create_error) {
        bool _error = false;
        if(_remainingTypes.Count > 0) {
          _error = true;
          plugin.PrintToError(paramName.ToString("Following key for \"type\" values are missing;"));
          foreach(var specskill in _remainingTypes)
            plugin.PrintToError(string.Format("Key skill: {0}", _getKeyParamName(specskill.Item1, specskill.Item2)));
        }

        if(_remainingValues.Count > 0) {
          _error = true;
          plugin.PrintToError(paramName.ToString("Following key for \"value\" values are missing;"));
          foreach(var specskill in _remainingValues)
            plugin.PrintToError(string.Format("Key skill: {0}", _getKeyParamName(specskill.Item1, specskill.Item2)));
        }
        
        if(_error)
          throw new ErrorSettingUpConfig(paramName.ToString("The value(s) isn't sufficient enough."));
      }


      foreach(var specskill in _currentTypes)
        skillset_data._ondied_edit_level_type[(byte)specskill.Key.Item1][specskill.Key.Item2] = specskill.Value;

      foreach(var specskill in _currentValues)
        skillset_data._ondied_edit_level_value[(byte)specskill.Key.Item1][specskill.Key.Item2] = specskill.Value;
    }

    /// <summary>
    /// Function for processing sublist of "skillset_requirement"
    /// </summary>
    /// <param name="section">Current subsection</param>
    /// <param name="skillset_data">Current data for a skillset</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    /// <param name="usedefaultvalue">Is currently processing default values</param>
    private void _process_configdata_skillset_requirement(IConfigurationSection section, ConfigData.SkillsetUpdateConfig skillset_data, NestedParameterData paramName, bool usedefaultvalue = false) {
      // default value should be ignored, thus it is emptied
      if(usedefaultvalue) {
        skillset_data._level_requirements = new Dictionary<(EPlayerSpeciality, byte), byte>();
        return;
      }


      Dictionary<(EPlayerSpeciality, byte), int> _currentValues = new Dictionary<(EPlayerSpeciality, byte), int>();

      foreach(var skill in section.GetChildren()) {
        var paramName_skill = paramName.Nest(skill.Key);
        try {
          var _param_result = _parseToSpecialtySkill(skill.Key);

          if(int.TryParse(skill.Value, out int _val)) {
            if(_param_result._isSpec_wildcard) {
              SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_index) => {
                skillset_data._level_requirements[(spec, skill_index)] = (byte)_val;
              });
            }
            else if(_param_result._isSkill_wildcard) {
              SpecialtyExpData.IterateArray(_param_result.spec, (byte skill_idx) => {
                skillset_data._level_requirements[(_param_result.spec, skill_idx)] = (byte)_val;
              });
            }
            else {
              if(_currentValues.ContainsKey((_param_result.spec, _param_result.skill_idx)))
                plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

              _currentValues[(_param_result.spec, _param_result.skill_idx)] = (byte)_val;
            }
          }
          else 
            plugin.PrintWarning(paramName_skill.ToString("Cannot parse value to Int32."));
        }
        catch(ParserException e) {
          _handle_ParserException(e, paramName, (string msg) => {
            plugin.PrintWarning(msg);
          });
        }
      }


      foreach(var specskill in _currentValues)
        skillset_data._level_requirements[(specskill.Key.Item1, specskill.Key.Item2)] = (byte)specskill.Value;
    }

    /// <summary>
    /// Parsing sublists of configuration data that contains what normally a <see cref="ConfigData.SkillsetUpdateConfig"/> contains. The function used for each skillsets
    /// </summary>
    /// <param name="skillset">Current skillset</param>
    /// <param name="section">The sublists data</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    /// <param name="create_error">If true, it throws an error and not continuing on parsing the data</param>
    /// <param name="usedefaultvalue">Is currently processing for default values</param>
    private void _process_configdata_skillset(EPlayerSkillset skillset, IConfigurationSection section, NestedParameterData paramName, bool create_error = false, bool usedefaultvalue = false) {
      var skillset_data = config_Data.skillupdate_configs[(int)skillset];
      Action<byte, byte>? action_notfound = null;


      Dictionary<string, System.Action<IConfigurationSection>> _parsers_dict = new Dictionary<string, System.Action<IConfigurationSection>>() {
        {"max_level", (IConfigurationSection _currsection) => {
          // max_level
          if(usedefaultvalue)
            action_notfound = (byte spec, byte idx) => {
              skillset_data._max_level[spec][idx] = byte.MaxValue;
            };

          _process_configdata_copytoarray<byte>(_currsection, skillset_data._max_level, false, paramName.Nest(_currsection.Key), action_notfound);
          SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill) => {
            byte defmaxlevel = specskill_default_maxlevel[(byte)spec][skill];
            if(skillset_data._max_level[(byte)spec][skill] > defmaxlevel)
              skillset_data._max_level[(byte)spec][skill] = defmaxlevel;
          });

          action_notfound = null;
        }},

        {"start_level", (IConfigurationSection _currsection) => {
          // start_level
          if(usedefaultvalue)
            action_notfound = (byte spec, byte idx) => {
              skillset_data._start_level[spec][idx] = byte.MinValue;
            };

          _process_configdata_copytoarray<byte>(_currsection, skillset_data._start_level, false, paramName.Nest(_currsection.Key), action_notfound);
            
          action_notfound = null;
        }},

        {"base_level_exp", (IConfigurationSection _currsection) => {
          // base_level_exp
          _process_configdata_copytoarray<int>(_currsection, skillset_data._base_level, create_error, paramName.Nest(_currsection.Key));
        }},

        {"mult_level_exp", (IConfigurationSection _currsection) => {
          // mult_level_exp
          _process_configdata_copytoarray<float>(_currsection, skillset_data._mult_level, create_error, paramName.Nest(_currsection.Key));
        }},

        {"multmult_level_exp", (IConfigurationSection _currsection) => {
          // multmult_level_exp
          if(usedefaultvalue)
            action_notfound = (byte spec, byte idx) => {
              skillset_data._multmult_level[spec][idx] = _default_multMultLevelExp;
            };
          
          _process_configdata_copytoarray<float>(_currsection, skillset_data._multmult_level, false, paramName.Nest(_currsection.Key), action_notfound);

          action_notfound = null;
        }},

        {"ondied_edit_level_exp", (IConfigurationSection _currsection) => {
          // ondied_edit_level_exp
          _process_configdata_copytoarray_ondied_edit(_currsection, skillset_data, create_error, paramName.Nest(_currsection.Key));
        }},

        {"excess_exp_increment", (IConfigurationSection _currsection) => {
          // excess_exp_increment
          if(usedefaultvalue)
            action_notfound = (byte spec, byte idx) => {
              skillset_data._excess_exp_increment[spec][idx] = _default_excessExpIncrement;
            };

          _process_configdata_copytoarray<int>(_currsection, skillset_data._excess_exp_increment, false, paramName.Nest(_currsection.Key), action_notfound);
          
          action_notfound = null;
        }},

        {"skillset_requirement", (IConfigurationSection _currsection) => {
          // skillset_requirement
          _process_configdata_skillset_requirement(_currsection, skillset_data, paramName.Nest(_currsection.Key), usedefaultvalue);
        }},

        {"demote_player_on_level_reduced", (IConfigurationSection _currsection) => {
          // demote_player_on_level_reduced
          // for default, still ignored, but if the value exists, the value will be passed to another skillsets
          if(_currsection.Exists()) {
            plugin.PrintToError(string.Format("demote value: {0}", _currsection.Value));
            if(float.TryParse(_currsection.Value, out float _val))
              skillset_data._is_demoteable = (int)Math.Round(_val) > 0;
            else {
              plugin.PrintToError(paramName.ToString(string.Format("Cannot parse value to float. Defaulting to {0}.", _default_isDemotable)));
              skillset_data._is_demoteable = _default_isDemotable;
            }
          }
          else if(usedefaultvalue)
            skillset_data._is_demoteable = _default_isDemotable;
        }}
      };


      HashSet<string> _important_parsers = new HashSet<string>() {
        "base_level_exp",
        "mult_level_exp",
        "ondied_edit_level_exp"
      };


      HashSet<string> _parsers_remaining = new HashSet<string>(_parsers_dict.Keys);

      foreach(var _param in section.GetChildren()) {
        var paramName_part = paramName.Nest(_param.Key);

        string _param_lower = _param.Key.ToLower();
        if(_parsers_dict.TryGetValue(_param_lower, out var parser)) {
          parser.Invoke(_param);

          if(!_parsers_remaining.Remove(_param_lower))
            plugin.PrintWarning(paramName_part.ToString("Multiple parameter."));

          _important_parsers.Remove(_param_lower);
        }
        else
          plugin.PrintWarning(paramName_part.ToString("Parameter name is invalid."));
      }

      if(_parsers_remaining.Count > 0) {
        foreach(var _parse in _parsers_remaining) {
          var paramName_part = paramName.Nest(_parse);
          IConfigurationSection _childsection = section.GetSection(_parse);

          _parsers_dict[_parse].Invoke(_childsection);
        }
      }

      if(_important_parsers.Count > 0 && create_error) {
        plugin.PrintWarning(paramName.ToString("Important parameter(s) missing;"));
        foreach(var _paramname in _important_parsers)
          plugin.PrintWarning(_paramname);

        throw new ErrorSettingUpConfig(paramName.ToString("Parameter is not enough."));
      }
    }

    /// <summary>
    /// Parsing data for skillset_config configuration data
    /// </summary>
    /// <param name="section">The sublists data</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    private void _process_configdata_skillsets(IConfigurationSection section, NestedParameterData paramName) {
      // process default data first
      _process_configdata_skillset(EPlayerSkillset.NONE, section.GetSection("default"), paramName.Nest("default"), true, true);

      Dictionary<string, byte> _skillset_remain = new Dictionary<string, byte>(skillset_indexer);
      foreach(var skillset in section.GetChildren()) {
        if(skillset_indexer.TryGetValue(skillset.Key.ToLower(), out byte _currskillset)) {
          if((EPlayerSkillset)_currskillset == EPlayerSkillset.NONE)
            continue;

          if(_currskillset == 255)
            _currskillset = (byte)(config_Data.skillupdate_configs.Length - 1);

          ConfigData.SkillsetUpdateConfig.CopyData(ref config_Data.skillupdate_configs[_currskillset], in config_Data.skillupdate_configs[(int)EPlayerSkillset.NONE]);
          _process_configdata_skillset((EPlayerSkillset)_currskillset, skillset, paramName.Nest(skillset.Key));

          _skillset_remain.Remove(skillset.Key.ToLower());
        }
        else
          plugin.PrintToError(paramName.ToString("Skillset name is invalid."));
      }

      foreach(var skillset in _skillset_remain) {
        ConfigData.SkillsetUpdateConfig.CopyData(ref config_Data.skillupdate_configs[skillset.Value], in config_Data.skillupdate_configs[(byte)EPlayerSkillset.NONE]);
      }
    }

    /// <summary>
    /// Parsing data for eventskill_udpatesumexp configuration data
    /// </summary>
    /// <param name="section"></param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    private void _process_configdata_eventskill(IConfigurationSection section, NestedParameterData paramName) {
      var _tmp_dict = new Dictionary<string, ESkillEvent>(skillevent_indexer);
      foreach(var child in section.GetChildren()) {
        var paramName_skill = paramName.Nest(child.Key);

        if(skillevent_indexer.TryGetValue(child.Key.ToLower(), out ESkillEvent eSkillEvent)) {
          if(float.TryParse(child.Value, out float _resval)) {
            config_Data.skillevent_exp[(int)eSkillEvent] = _resval;
            if(!_tmp_dict.Remove(child.Key.ToLower()))
              plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));
          }
          else
            plugin.PrintToError(paramName_skill.ToString("Value cannot be parsed."));
        }
        else
          plugin.PrintToError(paramName_skill.ToString("Parameter is invalid."));
      }

      foreach(var child in _tmp_dict) {
        plugin.PrintWarning(paramName.ToString(string.Format("Parameter {0} isn't available. The parameter will be set to 0.", child.Key)));
        config_Data.skillevent_exp[(int)skillevent_indexer[child.Key]] = 0.0f;
      }
    }

    /// <summary>
    /// Function for processing sublist of "environment_exp_multiplier"
    /// </summary>
    /// <param name="section">Current subsection</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    private void _process_configdata_environment_mult(IConfiguration section, NestedParameterData paramName) {
      foreach(var env in section.GetChildren()) {
        NestedParameterData paramName_env = paramName.Nest(env.Key);

        if(environment_indexer.TryGetValue(env.Key.ToLower(), out EEnvironment eEnvironment)) {
          Dictionary<(EPlayerSpeciality, byte, EModifierType), float>
            _assigned = new(),
            _assignedWildcard = new();

          foreach(var specskill in env.GetChildren()) {
            var paramName_skill = paramName_env.Nest(specskill.Key);

            try {
              var _parseResult = _parseToSpecialtySkill(specskill.Key);

              string[] _nextKeys = _parseResult.nextParam.Split(_separator_char);

              EModifierType eModifierType = ConfigData.ParseTo_EModifierType(_nextKeys[0]);
              if(eModifierType != EModifierType.NONE) {
                if(float.TryParse(specskill.Value, out float _val)) {
                  if(_parseResult._isSpec_wildcard) {
                    SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                      _assignedWildcard[(spec, skill_idx, eModifierType)] = _val;
                    });
                  }
                  else if(_parseResult._isSkill_wildcard) {
                    SpecialtyExpData.IterateArray(_parseResult.spec, (byte skill_idx) => {
                      _assignedWildcard[(_parseResult.spec, skill_idx, eModifierType)] = _val;
                    });
                  }
                  else {
                    var _dictkey = (_parseResult.spec, _parseResult.skill_idx, eModifierType);
                    if(_assigned.ContainsKey(_dictkey))
                      plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

                    _assigned[(_parseResult.spec, _parseResult.skill_idx, eModifierType)] = _val;
                  }
                }
                else
                  plugin.PrintToError(paramName_skill.ToString("Cannot parse value to float."));
              }
              else 
                plugin.PrintWarning(paramName_skill.ToString("Modifier type is invalid."));
            }
            catch(ParserException e) {
              _handle_ParserException(e, paramName_skill, (string msg) => {
                plugin.PrintWarning(msg);
              });
            }
          }


          foreach(var skill in _assigned)
            _assignedWildcard[skill.Key] = skill.Value;


          config_Data.environment_exp_multiplier[eEnvironment] = _assignedWildcard;
        }
        else
          plugin.PrintWarning(paramName_env.ToString("Environment name is invalid."));
      }
    }

    /// <summary>
    /// Function for processing "death_cause_multiplier"
    /// </summary>
    /// <param name="section">Current subsection</param>
    /// <param name="paramName">Current <see cref="NestedParameterData"/></param>
    private void _process_configdata_death_cause_mult(IConfiguration section, NestedParameterData paramName) {
      foreach(var deathcause in section.GetChildren()) {
        var deathcauseName = paramName.Nest(deathcause.Key);

        if(deathcause_indexer.TryGetValue(deathcause.Key.ToLower(), out EDeathCause eDeathCause)) {
          Dictionary<(EPlayerSpeciality, byte), float>
            _assigned = new(),
            _assignedWildcard = new();


          foreach(var specskill in deathcause.GetChildren()) {
            var paramName_skill = deathcauseName.Nest(specskill.Key);

            try {
              var _parseResult = _parseToSpecialtySkill(specskill.Key.ToLower());

              if(float.TryParse(specskill.Value, out float _val)) {
                if(_parseResult._isSpec_wildcard) {
                  SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                    _assignedWildcard[(spec, skill_idx)] = _val;
                  });
                }
                else if(_parseResult._isSkill_wildcard) {
                  SpecialtyExpData.IterateArray(_parseResult.spec, (byte skill_idx) => {
                    _assignedWildcard[(_parseResult.spec, skill_idx)] = _val;
                  });
                }
                else {
                  var _key = (_parseResult.spec, _parseResult.skill_idx);
                  if(_assigned.ContainsKey(_key))
                    plugin.PrintWarning(paramName_skill.ToString("Multiple parameter."));

                  _assigned[_key] = _val;
                }
              }
              else
                plugin.PrintToError(paramName_skill.ToString("Cannot parse value to float."));
            }
            catch(ParserException e) {
              _handle_ParserException(e, paramName_skill, (string msg) => {
                plugin.PrintWarning(msg);
              });
            }
          }


          foreach(var skill in _assigned)
            _assignedWildcard[(skill.Key)] = skill.Value;


          config_Data.death_cause_muitiplier[(byte)eDeathCause] = _assignedWildcard;
        }
        else
          plugin.PrintWarning(deathcauseName.ToString("Death cause name is invalid."));
      }
    }


    /// <param name="plugin">Current plugin object</param>
    /// <param name="configuration">Interface that handles configuration file data</param>
    public SkillConfig(SpecialtyOverhaul plugin, IConfiguration configuration) {
      this.configuration = configuration;
      this.plugin = plugin;
      Calculation = new CalculationUtils(this);

      config_Data = new ConfigData();
    }

    /// <summary>
    /// For re-reading configuration data. Used when .yaml config data has been edited. If something went wrong, it will fallback to previous config.
    /// </summary>
    /// <returns>Returns true if the configuration load properly</returns>
    public bool RefreshConfig() {
      // NOTE: make sure every parser has their own fallback values (if not exists or can't be parsed)

      ConfigData _lastConfigData = config_Data;
      bool _refresh_success = true;

      // just to make sure that every data is erased before refreshing
      config_Data = new ConfigData();
      

      Func<string, IConfiguration, float, float> configCheckerToFloat = (string key, IConfiguration currentConfig, float defvalue) => {
        IConfigurationSection _section = currentConfig.GetSection(key);
        if(float.TryParse(_section.Value, out float res))
          return res;
        else
          plugin.PrintToError(string.Format("Cannot parse value of key '{0}'. Defaulting to {1}.", key, defvalue));
        
        return defvalue;
      };


      Dictionary<string, Action<IConfigurationSection>> _parserList = new() {
        {"players_allow_retain_level", (IConfigurationSection section) => {
          float levelRetain = configCheckerToFloat(section.Key, section, _default_playersAllowRetainLevel? 1.0f: 0.0f);
          config_Data.player_levelretain = (int)Math.Round(levelRetain) > 0;
        }},

        {"skillset_config", (IConfigurationSection section) => {
          _process_configdata_skillsets(configuration.GetSection(section.Key), new NestedParameterData(section.Key, _separator_char.ToString()));
        }},

        {"eventskill_updatesumexp", (IConfigurationSection section) => {
          _process_configdata_eventskill(configuration.GetSection(section.Key), new NestedParameterData(section.Key, _separator_char.ToString()));
        }},

        {"environment_exp_multiplier", (IConfigurationSection section) => {
          _process_configdata_environment_mult(configuration.GetSection(section.Key), new NestedParameterData(section.Key, _separator_char.ToString()));
        }},

        {"death_cause_multiplier", (IConfigurationSection section) => {
          _process_configdata_death_cause_mult(configuration.GetSection(section.Key), new NestedParameterData(section.Key, _separator_char.ToString()));
        }},

        {"allow_change_skillset_after_change", (IConfigurationSection section) => {
          int _boolint = (int)Math.Round(configCheckerToFloat(section.Key, configuration, _default_allowChangeSkillsetAfterChange));

          config_Data.allow_change_skillset_after_change = _boolint > 0;
        }},

        {"recheck_level_if_skillset_changed", (IConfigurationSection section) => {
          int _boolint = (int)Math.Round(configCheckerToFloat(section.Key, configuration, _default_recheckLevelIfSkillsetChanged));

          config_Data.recheck_level_if_skillset_changed = _boolint > 0;
        }},

        {"tick_interval", (IConfigurationSection section) => {
          config_Data.tickinterval = configCheckerToFloat(section.Key, configuration, _default_tickInterval);
        }},

        {"autosave_interval", (IConfigurationSection section) => {
          config_Data.autosave_interval = configCheckerToFloat(section.Key, configuration, _default_autosaveInterval);
        }},

        {"random_boost_cost", (IConfigurationSection section) => {
          config_Data.random_boost_cost = (int)configCheckerToFloat(section.Key, configuration, _default_randomBoostCost);
        }}
      };


      
      HashSet<string> _remainingParameters = new(_parserList.Keys);
      HashSet<string> _importantParameters = new() {
        "skillset_config",
        "eventskill_updatesumexp"
      };


      try {
        foreach(var parameter in configuration.GetChildren()) {
          string _paramkey = parameter.Key.ToLower();
          if(_parserList.TryGetValue(_paramkey, out var parser)) {
            parser.Invoke(parameter);

            _remainingParameters.Remove(_paramkey);
            _importantParameters.Remove(_paramkey);
          }
          else 
            plugin.PrintWarning(string.Format("Unknown parameter: {0}", parameter.Key));
        }

        if(_importantParameters.Count > 0) {
          plugin.PrintToError("Important parameter missing;");
          foreach(var _paramkey in _importantParameters)
            plugin.PrintToError(_paramkey);

          throw new ErrorSettingUpConfig("Important parameter(s) missing.");
        }
        
        if(_remainingParameters.Count > 0) {
          plugin.PrintWarning("Some parameters are missing;");
          foreach(var _paramkey in _remainingParameters) {
            plugin.PrintWarning(_paramkey);

            _parserList[_paramkey].Invoke(configuration.GetSection(_paramkey));
          }
        }
      }
      catch(ErrorSettingUpConfig e) {
        plugin.PrintToError("Error occured when setting up default config");
        plugin.PrintToError(e.ToString());
        _refresh_success = false;
      }
      catch(Exception e) {
        plugin.PrintToError("Something went wrong.");
        plugin.PrintToError(e.ToString());
        _refresh_success = false;
      }

      if(!_refresh_success)
        config_Data = _lastConfigData;

      return _refresh_success;
    }

    /// <summary>
    /// Getting certain skill max level
    /// </summary>
    /// <param name="player">Current player</param>
    /// <param name="skillset">Player skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns></returns>
    public byte GetMaxLevel(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
      byte _playermax = specskill_default_maxlevel[(byte)spec][idx];
      byte _currentmax = config_Data.skillupdate_configs[(byte)skillset]._max_level[(byte)spec][idx];

      return _currentmax > _playermax ? _playermax : _currentmax;
    }
    
    /// <summary>
    /// Getting certain skill starting level
    /// </summary>
    /// <param name="skillset">Player skillset</param>
    /// <param name="spec">What specialty</param>
    /// <param name="idx">What skill</param>
    /// <returns></returns>
    public byte GetStartLevel(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) {
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

    public float GetAutosaveInterval() {
      return config_Data.autosave_interval;
    }

    /// <summary>
    /// Getting players_allow_retain_level value in boolean
    /// </summary>
    /// <returns>The value</returns>
    public bool GetPlayerRetainLevel() {
      return config_Data.player_levelretain;
    }

    public bool GetAllowChangeSkillsetAfterChange() {
      return config_Data.allow_change_skillset_after_change;
    }

    public bool GetRecheckLevelIfSkillsetChanged() {
      return config_Data.recheck_level_if_skillset_changed;
    }

    /// <summary>
    /// Getting random_boost_cost value
    /// </summary>
    /// <returns>The value</returns>
    public int GetRandomBoostCost() {
      return config_Data.random_boost_cost;
    }

    /// <summary>
    /// Getting excess_exp_increment value for each skillset and skill
    /// </summary>
    /// <param name="skillset">Player skillset</param>
    /// <param name="spec">Current specialty</param>
    /// <param name="index">Current skill</param>
    /// <returns>Increment value</returns>
    public int GetPlayerExcessExpIncrement(EPlayerSkillset skillset, EPlayerSpeciality spec, byte index) {
      int _ex = config_Data.skillupdate_configs[(byte)skillset]._excess_exp_increment[(int)spec][index];
      return _ex < 0 ? 0 : _ex;
    }

    public ISkillsetRequirement GetSkillsetRequirement(EPlayerSkillset skillset) {
      return new SkillsetRequirements(config_Data.skillupdate_configs[(byte)skillset]);
    }

    public bool IsSkillsetDemoteable(EPlayerSkillset skillset) {
      return config_Data.skillupdate_configs[(byte)skillset]._is_demoteable;
    }

    public ISkillMult_Environment GetEnvironmentMult(EEnvironment eEnvironment) {
      if(config_Data.environment_exp_multiplier.TryGetValue(eEnvironment, out var _mults))
        return new SkillMult_Environment(_mults);
      else
        return new SkillMult_Environment(new());
    }

    public ISkillMult_DeathCause GetDeathCauseMult(EDeathCause eDeathCause) {
      return new SkillMult_DeathCause(config_Data.death_cause_muitiplier[(int)eDeathCause]);
    }
  }
}
