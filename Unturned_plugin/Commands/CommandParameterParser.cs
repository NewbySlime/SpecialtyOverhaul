using dnlib.DotNet.Resources;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Misc;
using OpenMod.API.Commands;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// CommandParameterParser, as the name suggest, parsing parameters into an object or using callbacks to further processing data
  /// </summary>
  static class CommandParameterParser {
    /// <summary>
    /// An Exception class where it contains an enum for determining what happened before throwing
    /// </summary>
    public class ParsingException: Exception {
      private ParsingExceptionType type;
      public ParsingExceptionType Type {
        get {
          return type;
        }
      }

      public ParsingException(ParsingExceptionType type) {
        this.type = type;
      }
    }

    public enum ParsingExceptionType {
      /// <summary>
      /// Happens when the name of the specialty is wrong
      /// </summary>
      WRONG_SPECIALTYNAME = 0b1,
      /// <summary>
      /// Happens when the name of the skill is wrong
      /// </summary>
      WRONG_SKILLNAME = 0b10,
      WRONG_SPECIALTYSKILLNAME = WRONG_SPECIALTYNAME | WRONG_SKILLNAME,
      WRONG_SKILLSET = 0b100,
      WRONG_USERNAMEORID = 0b1000,
      WRONG_USER_SPEC = WRONG_USERNAMEORID | WRONG_SPECIALTYNAME,
      CANNOT_PARSE_PARAMETER = 0b10000
    }


    /// <summary>
    /// This contains resulting data when parsing parameters using one of the class functions
    /// </summary>
    public struct ParamResult_ToSpecialty {
      public EPlayerSpeciality spec;
      public byte skillidx;
      public bool isAllSkill;

      public string nextParam;
    }

    public struct ParamResult_ToUserAndSpecialty {
      public EPlayerSpeciality spec;
      public byte skillidx;
      public bool isAllSkill;

      public UnturnedUser? user;

      public string nextParam;
    }

    public struct ParamResult_ToUserAndSkillset {
      public EPlayerSkillset skillset;

      public UnturnedUser? user;

      public string nextParam;
    }

    /// <summary>
    /// This contains result of parsing parameter to skillset enum
    /// </summary>
    public struct ParamResult_ToSkillset {
      public EPlayerSkillset skillset;
    }


    public static readonly char param_splitter = '/';


    /// <summary>
    /// Using fuzzy search to get specialty enum <see cref="Algorithm.NameConfidence(string, string, int)"/>
    /// </summary>
    /// <param name="name">Specialty name (from parameter)</param>
    /// <param name="currentConfidence"></param>
    /// <returns>Returns a pair of confidence and specialty enum using tuple</returns>
    public static (int, EPlayerSpeciality?) SearchSpecialtyNameConfidence(string name, int currentConfidence = 0) {
      EPlayerSpeciality? res = null;

      foreach(var spec in SkillConfig.specskill_indexer) {
        int newConfidence = Algorithm.NameConfidence(spec.Key, name, currentConfidence);
        if(newConfidence > currentConfidence) {
          currentConfidence = newConfidence;
          res = (EPlayerSpeciality)spec.Value.Key;
        }
      }

      return (currentConfidence, res);
    }

    /// <summary>
    /// Using fuzzy search to get skill index enum <see cref="Algorithm.NameConfidence(string, string, int)"/>
    /// </summary>
    /// <param name="name">Skill name (from parameter)</param>
    /// <param name="currentConfidence"></param>
    /// <returns>Returns confidence and a (nullable) pair of specialty enum and skill index using nested tuples</returns>
    public static (int, (EPlayerSpeciality, byte)?) SearchSkillNameConfidence(string name, int currentConfidence = 0) {
      (EPlayerSpeciality, byte)? res = null;

      foreach(var skill in SkillConfig.skill_indexer) {
        int newConfidence = Algorithm.NameConfidence(skill.Key, name, currentConfidence);
        if(newConfidence > currentConfidence) {
          currentConfidence = newConfidence;
          res = skill.Value;
        }
      }

      return (currentConfidence, res);
    }

    /// <summary>
    /// Using fuzzy search to get skillset enum <see cref="Algorithm.NameConfidence(string, string, int)"/>
    /// </summary>
    /// <param name="name">Skillset name (from parameter)</param>
    /// <param name="currentConfidence"></param>
    /// <returns>Returns a pair of confidence and skillset enum using tuple</returns>
    public static (int, EPlayerSkillset?) SearchSkillsetNameConfidence(string name, int currentConfidence = 0) {
      EPlayerSkillset? res = null;

      foreach(var skillset in SkillConfig.skillset_indexer) {
        int newConfidence = Algorithm.NameConfidence(skillset.Key, name, currentConfidence);
        if(newConfidence > currentConfidence) {
          currentConfidence = newConfidence;
          res = (EPlayerSkillset?)skillset.Value;
        }
      }

      return (currentConfidence, res);
    }

    public static (int, UnturnedUser?) SearchUserNameConfidence(IEnumerable<UnturnedUser> users, string nameOrId, int currentConfidence = 0) {
      UnturnedUser? user = null;

      foreach(UnturnedUser _user in users) {
        if(string.Equals(_user.Id, nameOrId, StringComparison.OrdinalIgnoreCase))
          return (3, _user);

        int _newConfidence = Algorithm.NameConfidence(_user.DisplayName, nameOrId, currentConfidence);
        if(_newConfidence > currentConfidence) {
          user = _user;
          currentConfidence = _newConfidence;
        }
      }

      return (currentConfidence, user);
    }

    public static (int, dictValue?) SearchDictionaryConfidence<dictValue>(Dictionary<string, dictValue> dict, string valueName, int currentConfidence = 0) {
      dictValue? val = default;
      foreach(var pair in dict) {
        int _newConfidence = Algorithm.NameConfidence(pair.Key, valueName, currentConfidence);
        if(_newConfidence > currentConfidence) {
          currentConfidence = _newConfidence;
          val = pair.Value;
        }
      }

      return (currentConfidence, val);
    }

    /// <summary>
    /// Parsing parameter from string of <c>specialty/skill</c> to program-readable parameter. It outputs <see cref="ParamResult_ToSpecialty"/> by using "result" variable
    /// </summary>
    /// <param name="result">The output of the function</param>
    /// <param name="param">Parameter when calling a command </param>
    /// <exception cref="ParsingException"></exception>
    public static void ToSpecialty(out ParamResult_ToSpecialty result, string param) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      plugin?.PrintToOutput(param);

      string specparam = "", skillparam = "";

      int _idx = param.IndexOf('/');
      if(_idx != -1) {
        specparam = param.Substring(0, _idx).ToLower();
        param = param.Substring(_idx + 1);
        plugin?.PrintToOutput(param);

        _idx = param.IndexOf('/');
        if(_idx != -1) {
          skillparam = param.Substring(0, _idx).ToLower();
          param = param.Substring(_idx + 1);
          plugin?.PrintToOutput(param);
        }
        else {
          skillparam = param;
          param = "";
        }
      }
      else {
        specparam = param;
        param = "";
      }

      result = new ParamResult_ToSpecialty();
      result.nextParam = param;

      if(skillparam != string.Empty) {
        var res1 = SearchSpecialtyNameConfidence(specparam);
        if(res1.Item2 == null)
          throw new ParsingException(ParsingExceptionType.WRONG_SPECIALTYNAME);

        var res2 = SearchSkillNameConfidence(skillparam);
        if(res2.Item2 == null)
          throw new ParsingException(ParsingExceptionType.WRONG_SKILLNAME);

        result.spec = res1.Item2.Value;
        result.skillidx = res2.Item2.Value.Item2;
      }
      else {
        var res1 = SearchSpecialtyNameConfidence(specparam);
        var res2 = SearchSkillNameConfidence(specparam, res1.Item1);

        if(res2.Item1 > res1.Item1 && res2.Item2 != null) {
          result.spec = res2.Item2.Value.Item1;
          result.skillidx = res2.Item2.Value.Item2;
        }
        else if(res1.Item2 != null) {
          result.spec = res1.Item2.Value;
          result.isAllSkill = true;
        }
        else
          throw new ParsingException(ParsingExceptionType.WRONG_SPECIALTYSKILLNAME);
      }
    }

    public static void ToUserAndSpecialty(IEnumerable<UnturnedUser> users, out ParamResult_ToUserAndSpecialty res, string param) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      res = new ParamResult_ToUserAndSpecialty() {
        user = null
      };

      bool _search_user = true, _search_spec = true, _search_skill = true;
      bool _search_completed = false;

      Dictionary<string, byte>? _current_skill_indexer = null;

      string[] splits = param.Split(param_splitter);

      bool _keepLoop = true;
      int _lastidx = 0;
      for(int i = 0; _keepLoop && i < splits.Length; i++) {
        // for _what_confidence, look at switch below
        int _currentconfidence = 0, _what_confidence = -1;
        Object? _currentobj = null;

        if(_search_user) {
          var _confidence_user = SearchUserNameConfidence(users, splits[i], _currentconfidence);
          if(_confidence_user.Item1 > _currentconfidence) {
            _currentconfidence = _confidence_user.Item1;
            _currentobj = _confidence_user.Item2;
            _what_confidence = 1;
          }
        }

        if(_search_spec) {
          var _confidence_spec = SearchSpecialtyNameConfidence(splits[i], _currentconfidence);
          if(_confidence_spec.Item1 > _currentconfidence) {
            _currentconfidence = _confidence_spec.Item1;
            _currentobj = _confidence_spec.Item2;
            _what_confidence = 2;
          }
        }

        if(_search_skill) {
          (int, (EPlayerSpeciality, byte)?) _confidence_skill;

          if(_current_skill_indexer == null)
            _confidence_skill = SearchSkillNameConfidence(splits[i], _currentconfidence);
          else {
            // this assume that search spec is fulfilled
            var _newConfidence = SearchDictionaryConfidence(_current_skill_indexer, splits[i], _currentconfidence);
            _confidence_skill = (_newConfidence.Item1, (res.spec, _newConfidence.Item2));
          }

          if(_confidence_skill.Item1 > _currentconfidence) {
            plugin?.PrintToOutput("Change confidence");
            _currentconfidence = _confidence_skill.Item1;
            _currentobj = _confidence_skill.Item2;
            _what_confidence = 3;
          }
        }
        plugin?.PrintToOutput(string.Format("idx: {0}/{1}", i, splits.Length));


        // NOTE: that _currentobj shouldn't be null, since if null, the switch will fall to -1
        switch(_what_confidence) {
          // not found
          case -1:
            if(_search_spec && _search_skill)
              throw new ParsingException(ParsingExceptionType.WRONG_SPECIALTYSKILLNAME);
            break;

          case -2:
            _lastidx = i+1;
            break;

          // it's user
          case 1:
            _search_user = false;

            res.user = _currentobj as UnturnedUser;
            goto case -2;

          // it's spec
          case 2: {
            _search_user = false;
            _search_spec = false;

            res.isAllSkill = true;

            var _spec = _currentobj as EPlayerSpeciality?;
            if(_spec != null) {
              // since the user specified the spec, then skill_indexer should be narrowed
              res.spec = (EPlayerSpeciality)_spec;
              _current_skill_indexer = SkillConfig.specskill_indexer[IndexerHelper.GetIndexerEnumName(res.spec)].Value;
            }

            _search_completed = true;
            goto case -2;
          }

          // it's skill
          // if the switch falls to this, the function is considered as complete
          case 3: {
            _keepLoop = false;

            res.isAllSkill = false;
            
            var _skill = _currentobj as (EPlayerSpeciality, byte)?;
            if(_skill != null) {
              res.spec = _skill.Value.Item1;
              res.skillidx = _skill.Value.Item2;
            }

            _search_completed = true;
            goto case -2;
          }
        }
      }


      if(!_search_completed)
        throw new ParsingException(ParsingExceptionType.CANNOT_PARSE_PARAMETER);

      // creating next param
      res.nextParam = "";
      for(; _lastidx < splits.Length; _lastidx++) {
        res.nextParam += splits[_lastidx];
          
        if(_lastidx < (splits.Length - 1))
          res.nextParam += param_splitter;
      }
    }
    
    /// <summary>
    /// Parsing parameter to skillset enum
    /// </summary>
    /// <param name="result">The result of parsing</param>
    /// <param name="param">Inputted parameter</param>
    /// <exception cref="ParsingException"></exception>
    public static void ToSkillset(out ParamResult_ToSkillset result, string param) {
      var res = SearchSkillsetNameConfidence(param);
      if(res.Item2 != null) {
        result = new ParamResult_ToSkillset {
          skillset = res.Item2.Value
        };
      }
      else
        throw new ParsingException(ParsingExceptionType.WRONG_SKILLSET);
    }

    public static void ToUserAndSkillset(IEnumerable<UnturnedUser> users, out ParamResult_ToUserAndSkillset result, string param) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      result = new ParamResult_ToUserAndSkillset() {
        user = null
      };

      bool _search_user = true, _search_skillset = true;
      bool _search_completed = false;
      string[] splits = param.ToLower().Split(param_splitter);

      const int _looplen = 2;
      int i = 0;
      for(; i < _looplen && i < splits.Length; i++) {
        // for _what_confidence, look at switch below
        int _currentconfidence = 0, _what_confidence = -1;
        Object? _currentobj = null;

        if(_search_user) {
          var _confidence_user = SearchUserNameConfidence(users, splits[i], _currentconfidence);
          if(_confidence_user.Item1 > _currentconfidence) {
            _currentconfidence = _confidence_user.Item1;
            _currentobj = _confidence_user.Item2;
            _what_confidence = 1;
          }
        }

        if(_search_skillset) {
          var _confidence_skillset = SearchSkillsetNameConfidence(splits[i], _currentconfidence);
          if(_confidence_skillset.Item1 > _currentconfidence) {
            _currentconfidence = _confidence_skillset.Item1;
            _currentobj = _confidence_skillset.Item2;
            _what_confidence = 2;
          }
        }


        // NOTE: that _currentobj shouldn't be null, since if null, the switch will fall to -1
        switch(_what_confidence) {
          // not found
          case -1:
            if(_search_skillset)
              throw new ParsingException(ParsingExceptionType.WRONG_SKILLSET);

            break;

          // it's user
          case 1:
            _search_user = false;

            result.user = _currentobj as UnturnedUser;
            break;


          // it's skillset
          case 2:
            _search_user = false;
            _search_skillset = false;

            var _skillset = _currentobj as EPlayerSkillset?;
            if(_skillset.HasValue) 
              result.skillset = _skillset.Value;

            _search_completed = true;

            break;
        }
      }

      if(!_search_completed)
        throw new ParsingException(ParsingExceptionType.CANNOT_PARSE_PARAMETER);

      // combining the rest of the unused parameter
      result.nextParam = "";
      for(; i < splits.Length; i++) {
        result.nextParam += splits[i];
        if(i < (splits.Length - 1))
          result.nextParam += param_splitter;
      }
    }

    /// <summary>
    /// Printing a false feedback when specialty (or skill) name is invalid
    /// </summary>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    public static async Task Print_OnNoParamSpeciality(ICommandContext currentContext) {
      await currentContext.Actor.PrintMessageAsync("<specialty> parameter can be:", System.Drawing.Color.YellowGreen);

      foreach(var spec in SkillConfig.specskill_indexer) {
        await currentContext.Actor.PrintMessageAsync(string.Format("  {0}", spec.Key), System.Drawing.Color.Green);
      }
    }

    /// <summary>
    /// Printing a false feedback when skillset name is invalid
    /// </summary>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    public static async Task Print_OnNoParamSkillset(ICommandContext currentContext) {
      await currentContext.Actor.PrintMessageAsync("Use /ListSkillset to list all skillset names.", System.Drawing.Color.YellowGreen);
    }

    /// <summary>
    /// This parameter is used for commands that use parameter formatting of <c>[id/name]/specialty/skill</c>. Then when parsing succeed, the data then passed to callbacks. Name or ID can be omitted.
    /// </summary>
    /// <param name="plugin">Current plugin object</param>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    /// <param name="OnSuccess">Callback when the parsing succeeds (called every skill). The last one (typeof string) is the next parameter after "skill"</param>
    /// <returns></returns>
    /// <exception cref="NotEnoughPermissionException"></exception>
    /// <exception cref="CommandWrongUsageException"></exception>
    public static async Task ParseParameter_Type1(SpecialtyOverhaul plugin, ICommandContext currentContext, bool needPermission, Func<UnturnedUser, EPlayerSpeciality, bool, byte, string, Task> OnSuccess) {
      try {
        if(currentContext.Parameters.Length > 0) {
          ParamResult_ToUserAndSpecialty _res;
          ToUserAndSpecialty(plugin.UnturnedUserProviderInstance.GetOnlineUsers(), out _res, await currentContext.Parameters.GetAsync<string>(0));

          plugin.PrintToOutput(string.Format("isAllSkill {0}", _res.isAllSkill));

          UnturnedUser? _targetUser = currentContext.Actor as UnturnedUser;
          if(_targetUser != null) {
            if(_res.user != null) {
              // _targetUser is still current Context.Actor
              if(!needPermission || _targetUser.Player.SteamPlayer.isAdmin)
                _targetUser = _res.user;
              else {
                await currentContext.Actor.PrintMessageAsync("Can't use admin features (using user search)", System.Drawing.Color.Red);
                throw new CommandWrongUsageException(currentContext);
              }
            }
          }
          else if(currentContext.Actor is ConsoleActor) {
            if(_res.user != null)
              _targetUser = _res.user;
            else {
              await currentContext.Actor.PrintMessageAsync("Cannot find user.", System.Drawing.Color.Red);
              throw new CommandWrongUsageException(currentContext);
            }
          }


          // guarantee not null
          if(_targetUser != null) 
            await OnSuccess.Invoke(_targetUser, _res.spec, _res.isAllSkill, _res.skillidx, _res.nextParam);
        }
        else
          await Print_OnNoParamSpeciality(currentContext);
      }
      catch(ParsingException e) {
        switch(e.Type) {
          case ParsingExceptionType.WRONG_SPECIALTYSKILLNAME:
            await currentContext.Actor.PrintMessageAsync("Specialty or skill name is invalid.", System.Drawing.Color.Red);
            throw new CommandWrongUsageException(currentContext);

          case ParsingExceptionType.CANNOT_PARSE_PARAMETER:
            await currentContext.Actor.PrintMessageAsync("Parameter is invalid.", System.Drawing.Color.Red);
            throw new CommandWrongUsageException(currentContext);
        }
      }
    }

    /// <summary>
    /// This parameter is used for commands that use parameter formatting of <c>[id/name]/specialty/skill/level</c>. Then when parsing succeed, the data then passed to callbacks. Name or ID can be omitted.
    /// </summary>
    /// <param name="plugin">Current plugin object</param>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    /// <param name="OnSuccess">Callback when the parsing succeeds (called every skill). The last one (typeof string) is the next parameter after "level"</param>
    /// <returns></returns>
    /// <exception cref="CommandParameterParseException"></exception>
    /// <exception cref="CommandWrongUsageException"></exception>
    public static async Task ParseParameter_Type2(SpecialtyOverhaul plugin, ICommandContext currentContext, bool needPermission, Func<UnturnedUser, EPlayerSpeciality, bool, byte, float, string, Task> OnSuccess) {
      await ParseParameter_Type1(plugin, currentContext, needPermission, async (UnturnedUser user, EPlayerSpeciality spec, bool isAllSkill, byte skill_idx, string nextParam) => {
        plugin.PrintToOutput(string.Format("nextparam: {0}", nextParam));
        string[] _params = nextParam.Split(param_splitter);
        if(_params.Length >= 1) {
          if(float.TryParse(_params[0], out float _lvl))
            await OnSuccess.Invoke(user, spec, isAllSkill, skill_idx, _lvl, nextParam);
          else 
            throw new CommandParameterParseException("Level parameter is not a number.", "", typeof(float));
        }
        else {
          await currentContext.Actor.PrintMessageAsync("Insert level parameter.", System.Drawing.Color.Red);
          throw new CommandWrongUsageException(currentContext);
        }
      });
    }

    public static async Task ParseParameter_Type3(SpecialtyOverhaul plugin, ICommandContext currentContext, bool needPermission, Func<UnturnedUser, EPlayerSkillset, string, Task> OnSuccess) {
      try{
        if(currentContext.Parameters.Length > 0) {
          ParamResult_ToUserAndSkillset _res;
          ToUserAndSkillset(plugin.UnturnedUserProviderInstance.GetOnlineUsers(), out _res, await currentContext.Parameters.GetAsync<string>(0));

          UnturnedUser? _targetUser = currentContext.Actor as UnturnedUser;
          if(_targetUser != null) {
            if(_res.user != null) {
              // _targetUser is still current Context.Actor
              if(!needPermission || _targetUser.Player.SteamPlayer.isAdmin)
                _targetUser = _res.user;
              else {
                await currentContext.Actor.PrintMessageAsync("Can't use admin features (using user search)", System.Drawing.Color.Red);
                throw new CommandWrongUsageException(currentContext);
              }
            }
          }
          else if(currentContext.Actor is ConsoleActor) {
            if(_res.user != null)
              _targetUser = _res.user;
            else {
              await currentContext.Actor.PrintMessageAsync("Cannot find user.", System.Drawing.Color.Red);
              throw new CommandWrongUsageException(currentContext);
            }
          }

          // guarantee not null
          if(_targetUser != null)
            await OnSuccess(_targetUser, _res.skillset, _res.nextParam);
        }
        else {
          await Print_OnNoParamSkillset(currentContext);
          throw new CommandWrongUsageException(currentContext);
        }
      }
      catch(ParsingException e) {
        switch(e.Type) {
          case ParsingExceptionType.WRONG_SKILLSET:
            await currentContext.Actor.PrintMessageAsync("Skillset name is invalid.", System.Drawing.Color.Red);
            throw new CommandWrongUsageException(currentContext);
        }
      }
    }
  }
}
