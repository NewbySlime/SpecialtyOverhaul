using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Commands;
using OpenMod.API.Users;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
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
      WRONG_SPECIALTYNAME,
      /// <summary>
      /// Happens when the name of the skill is wrong
      /// </summary>
      WRONG_SKILLNAME,
      WRONG_SPECIALTYSKILLNAME
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


    private static (int, EPlayerSpeciality?) searchSpecialtyNameConfidence(string name, int currentConfidence = 0) {
      EPlayerSpeciality? res = null;

      foreach(var spec in SkillConfig.specskill_indexer) {
        int newConfidence = Misc.NameConfidence(spec.Key, name, currentConfidence);
        if(newConfidence > currentConfidence) {
          currentConfidence = newConfidence;
          res = (EPlayerSpeciality)spec.Value.Key;
        }
      }

      return (currentConfidence, res);
    }

    private static (int, (EPlayerSpeciality, byte)?) searchSkillNameConfidence(string name, int currentConfidence = 0) {
      (EPlayerSpeciality, byte)? res = null;

      foreach(var skill in SkillConfig.skill_indexer) {
        int newConfidence = Misc.NameConfidence(skill.Key, name, currentConfidence);
        if(newConfidence > currentConfidence) {
          currentConfidence = newConfidence;
          res = skill.Value;
        }
      }

      return (currentConfidence, res);
    }

    /// <summary>
    /// Parsing parameter from string of <c>specialty/skill</c> to program-readable parameter. It outputs ParamResult_ToSpecialty by using "result" variable
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
        var res1 = searchSpecialtyNameConfidence(specparam);
        if(res1.Item2 == null)
          throw new ParsingException(ParsingExceptionType.WRONG_SPECIALTYNAME);

        var res2 = searchSkillNameConfidence(skillparam);
        if(res2.Item2 == null)
          throw new ParsingException(ParsingExceptionType.WRONG_SKILLNAME);

        result.spec = res1.Item2.Value;
        result.skillidx = res2.Item2.Value.Item2;
      }
      else {
        var res1 = searchSpecialtyNameConfidence(specparam);
        var res2 = searchSkillNameConfidence(specparam, res1.Item1);

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

    /// <summary>
    /// Printing a false feedback of when specialty name is invalid
    /// </summary>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    public static async Task Print_OnNoParam(ICommandContext currentContext) {
      await currentContext.Actor.PrintMessageAsync("<specialty> parameter can be:", System.Drawing.Color.YellowGreen);

      foreach(var spec in SkillConfig.specskill_indexer) {
        await currentContext.Actor.PrintMessageAsync(string.Format("  {0}", spec.Key), System.Drawing.Color.Green);
      }
    }

    /// <summary>
    /// Parsing a parameter (with user name/id stripped from param), then also process its data. Used for GetLevel/GetPlayerLevel command
    /// </summary>
    /// <param name="plugin">Current plugin</param>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    /// <param name="user">The user to get level to</param>
    /// <param name="param">Command parameters</param>
    public static async Task ParseToMessage_GetLevel(SpecialtyOverhaul plugin, ICommandContext currentContext, UnturnedUser user, string param) {
      CommandParameterParser.ParamResult_ToSpecialty res;

      try {
        CommandParameterParser.ToSpecialty(out res, param);
      }
      catch(CommandParameterParser.ParsingException e) {
        switch(e.Type) {
          case CommandParameterParser.ParsingExceptionType.WRONG_SPECIALTYNAME:
            await currentContext.Actor.PrintMessageAsync("Specialty name is invalid.", System.Drawing.Color.Red);
            break;

          case CommandParameterParser.ParsingExceptionType.WRONG_SKILLNAME:
            await currentContext.Actor.PrintMessageAsync("Skill name is invalid.", System.Drawing.Color.Red);
            break;

          case CommandParameterParser.ParsingExceptionType.WRONG_SPECIALTYSKILLNAME:
            await currentContext.Actor.PrintMessageAsync("Specialty or skill name is invalid.", System.Drawing.Color.Red);
            break;
        }

        return;
      }
      catch(Exception e) {
        plugin.PrintToError("Something went wrong when parsing data.");
        plugin.PrintToError(e.ToString());

        return;
      }

      if(res.isAllSkill) {
        int skilllen = 0;
        switch(res.spec) {
          case EPlayerSpeciality.OFFENSE:
            skilllen = SpecialtyExpData._skill_offense_count;
            break;

          case EPlayerSpeciality.DEFENSE:
            skilllen = SpecialtyExpData._skill_defense_count;
            break;

          case EPlayerSpeciality.SUPPORT:
            skilllen = SpecialtyExpData._skill_support_count;
            break;
        }

        for(int i = 0; i < skilllen; i++) {
          var strpair = plugin.SkillUpdaterInstance.GetExp_AsProgressBar(user.Player, res.spec, i, true);
          await user.PrintMessageAsync(string.Format("{0}:\n  {1}", strpair.Key, strpair.Value), System.Drawing.Color.Aqua);
        }
      }
      else {
        var strpair = plugin.SkillUpdaterInstance.GetExp_AsProgressBar(user.Player, res.spec, res.skillidx);
        await user.PrintMessageAsync(strpair.Value, System.Drawing.Color.Aqua);
      }
    }

    /// <summary>
    /// This parameter is used for commands that use parameter formatting of <c>[id/name]/specialty/skill/level.</c> Then when parsing succeed, the data then passed to callbacks
    /// </summary>
    /// <param name="plugin">Current plugin</param>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    /// <param name="OnSuccess">Callback when the parsing succeeds (called every skill)</param>
    public static async Task ParseParameter1(SpecialtyOverhaul plugin, ICommandContext currentContext, System.Action<UnturnedUser, EPlayerSpeciality, byte, int> OnSuccess) {
      if(currentContext.Parameters.Length > 0) {
        try {
          string _param = await currentContext.Parameters.GetAsync<string>(0);
          int _idx = _param.IndexOf('/');
          if(_idx == -1) {
            await currentContext.Actor.PrintMessageAsync("Parameter only consist of user name/id", System.Drawing.Color.Red);
            return;
          }

          string userstr = _param.Substring(0, _idx);
          UnturnedUser? user = await plugin.UnturnedUserProviderInstance.FindUserAsync("", userstr, UserSearchMode.FindByNameOrId) as UnturnedUser;
          if(user == null) {
            await currentContext.Actor.PrintMessageAsync(string.Format("User {0} cannot be found.", userstr), System.Drawing.Color.Red);
            return;
          }

          CommandParameterParser.ParamResult_ToSpecialty res;
          CommandParameterParser.ToSpecialty(out res, _param.Substring(_idx + 1));

          _param = res.nextParam;
          _idx = _param.IndexOf("/");

          string _numstr = _param;
          if(_idx != -1)
            _numstr = _param.Substring(0, _idx);

          int level;
          if(!int.TryParse(_numstr, out level)) {
            await currentContext.Actor.PrintMessageAsync("Level parameter isn't an integer.", System.Drawing.Color.Red);
            return;
          }

          if(res.isAllSkill) {
            int skilllen = 0;
            switch(res.spec) {
              case EPlayerSpeciality.OFFENSE:
                skilllen = SpecialtyExpData._skill_offense_count;
                break;

              case EPlayerSpeciality.DEFENSE:
                skilllen = SpecialtyExpData._skill_defense_count;
                break;

              case EPlayerSpeciality.SUPPORT:
                skilllen = SpecialtyExpData._skill_support_count;
                break;
            }

            for(int i = 0; i < skilllen; i++)
              OnSuccess.Invoke(user, res.spec, (byte)i, level);
          }
          else
            OnSuccess.Invoke(user, res.spec, res.skillidx, level);
        }
        catch(CommandParameterParser.ParsingException e) {
          switch(e.Type) {
            case CommandParameterParser.ParsingExceptionType.WRONG_SPECIALTYNAME:
              await currentContext.Actor.PrintMessageAsync("Specialty name is invalid.", System.Drawing.Color.Red);
              break;

            case CommandParameterParser.ParsingExceptionType.WRONG_SKILLNAME:
              await currentContext.Actor.PrintMessageAsync("Skill name is invalid.", System.Drawing.Color.Red);
              break;
          }
        }
      }
      else
        await currentContext.Actor.PrintMessageAsync("Parameter cannot be empty.", System.Drawing.Color.Red);
    }
  }
}
