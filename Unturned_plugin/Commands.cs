using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.API.Commands;
using OpenMod.Unturned.Commands;
using Nekos.SpecialtyPlugin;
using Cysharp.Threading.Tasks;
using SDG.Unturned;
using OpenMod.Unturned.Users;
using Steamworks;
using Nekos.SpecialtyPlugin.Commands;
using System.Runtime.InteropServices;
using OpenMod.API.Users;
using System.Runtime.Remoting.Contexts;
using OpenMod.Unturned.Players.Life.Events;

namespace Nekos.SpecialtyPlugin.Commands {
  /// <summary>
  /// CommandParameterParser, as the name suggest, parsing parameters into an object or using callbacks to further processing data
  /// </summary>
  static class CommandParameterParser {
    /// <summary>
    /// An Exception class where it contains an enum for determining what happened before throwing
    /// </summary>
    public class ParsingException : Exception {
      private ParsingExceptionType type;
      public ParsingExceptionType Type {
        get { return type; }
      }

      public ParsingException(ParsingExceptionType type) { this.type = type; }
    }

    public enum ParsingExceptionType {
      /// <summary>
      /// Happens when the name of the specialty is wrong
      /// </summary>
      WRONG_SPECIALTYNAME,
      /// <summary>
      /// Happens when the name of the skill is wrong
      /// </summary>
      WRONG_SKILLNAME
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
      if (_idx != -1) {
        specparam = param.Substring(0, _idx).ToLower();
        param = param.Substring(_idx + 1);
        plugin?.PrintToOutput(param);

        _idx = param.IndexOf('/');
        if (_idx != -1) {
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

      if (SkillConfig.specskill_indexer.ContainsKey(specparam)) {
        var _specpair = SkillConfig.specskill_indexer[specparam];
        result.spec = (EPlayerSpeciality)_specpair.Key;

        if (skillparam != string.Empty) {
          if (_specpair.Value.ContainsKey(skillparam)) {
            result.skillidx = _specpair.Value[skillparam];
          } else
            throw new ParsingException(ParsingExceptionType.WRONG_SKILLNAME);
        } else
          result.isAllSkill = true;
      } else
        throw new ParsingException(ParsingExceptionType.WRONG_SPECIALTYNAME);
    }

    /// <summary>
    /// Printing a false feedback of when specialty name is invalid
    /// </summary>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    public static async Task Print_OnNoParam(ICommandContext currentContext) {
      await currentContext.Actor.PrintMessageAsync("<specialty> parameter can be:", System.Drawing.Color.YellowGreen);

      foreach (var spec in SkillConfig.specskill_indexer) {
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
      } catch (CommandParameterParser.ParsingException e) {
        switch (e.Type) {
          case CommandParameterParser.ParsingExceptionType.WRONG_SPECIALTYNAME:
            await currentContext.Actor.PrintMessageAsync("Specialty name is invalid.", System.Drawing.Color.Red);
            break;

          case CommandParameterParser.ParsingExceptionType.WRONG_SKILLNAME:
            await currentContext.Actor.PrintMessageAsync("Skill name is invalid.", System.Drawing.Color.Red);
            break;
        }

        return;
      } catch (Exception e) {
        plugin.PrintToError("Something went wrong when parsing data.");
        plugin.PrintToError(e.ToString());

        return;
      }

      if (res.isAllSkill) {
        int skilllen = 0;
        switch (res.spec) {
          case EPlayerSpeciality.OFFENSE:
            skilllen = specialtyExpData._skill_offense_count;
            break;

          case EPlayerSpeciality.DEFENSE:
            skilllen = specialtyExpData._skill_defense_count;
            break;

          case EPlayerSpeciality.SUPPORT:
            skilllen = specialtyExpData._skill_support_count;
            break;
        }

        for (int i = 0; i < skilllen; i++) {
          var strpair = plugin.SkillUpdaterInstance.GetExp_AsProgressBar(user.Player, res.spec, i, true);
          await user.PrintMessageAsync(string.Format("{0}:\n  {1}", strpair.Key, strpair.Value), System.Drawing.Color.Aqua);
        }
      } else {
        var strpair = plugin.SkillUpdaterInstance.GetExp_AsProgressBar(user.Player, res.spec, res.skillidx);
        await user.PrintMessageAsync(strpair.Value, System.Drawing.Color.Aqua);
      }
    }

    /// <summary>
    /// This parameter is used for commands that use parameter formatting of [id/name]/specialty/skill/level. Then when parsing succeed, the data then passed to callbacks
    /// </summary>
    /// <param name="plugin">Current plugin</param>
    /// <param name="currentContext">Current command context used by class that handles commands</param>
    /// <param name="OnSuccess">Callback when the parsing succeeds (called every skill)</param>
    public static async Task ParseParameter1(SpecialtyOverhaul plugin, ICommandContext currentContext, System.Action<UnturnedUser, EPlayerSpeciality, byte, int> OnSuccess) {
      if (currentContext.Parameters.Length > 0) {
        try {
          string _param = await currentContext.Parameters.GetAsync<string>(0);
          int _idx = _param.IndexOf('/');
          if (_idx == -1) {
            await currentContext.Actor.PrintMessageAsync("Parameter only consist of user name/id", System.Drawing.Color.Red);
            return;
          }

          string userstr = _param.Substring(0, _idx);
          UnturnedUser? user = await plugin.UnturnedUserProviderInstance.FindUserAsync("", userstr, UserSearchMode.FindByNameOrId) as UnturnedUser;
          if (user == null) {
            await currentContext.Actor.PrintMessageAsync(string.Format("User {0} cannot be found.", userstr), System.Drawing.Color.Red);
            return;
          }

          CommandParameterParser.ParamResult_ToSpecialty res;
          CommandParameterParser.ToSpecialty(out res, _param.Substring(_idx + 1));

          _param = res.nextParam;
          _idx = _param.IndexOf("/");

          string _numstr = _param;
          if (_idx != -1)
            _numstr = _param.Substring(0, _idx);

          int level;
          if (!int.TryParse(_numstr, out level)) {
            await currentContext.Actor.PrintMessageAsync("Level parameter isn't an integer.", System.Drawing.Color.Red);
            return;
          }

          if (res.isAllSkill) {
            int skilllen = 0;
            switch (res.spec) {
              case EPlayerSpeciality.OFFENSE:
                skilllen = specialtyExpData._skill_offense_count;
                break;

              case EPlayerSpeciality.DEFENSE:
                skilllen = specialtyExpData._skill_defense_count;
                break;

              case EPlayerSpeciality.SUPPORT:
                skilllen = specialtyExpData._skill_support_count;
                break;
            }

            for (int i = 0; i < skilllen; i++)
              OnSuccess.Invoke(user, res.spec, (byte)i, level);
          } else
            OnSuccess.Invoke(user, res.spec, res.skillidx, level);
        } catch (CommandParameterParser.ParsingException e) {
          switch (e.Type) {
            case CommandParameterParser.ParsingExceptionType.WRONG_SPECIALTYNAME:
              await currentContext.Actor.PrintMessageAsync("Specialty name is invalid.", System.Drawing.Color.Red);
              break;

            case CommandParameterParser.ParsingExceptionType.WRONG_SKILLNAME:
              await currentContext.Actor.PrintMessageAsync("Skill name is invalid.", System.Drawing.Color.Red);
              break;
          }
        }
      } else
        await currentContext.Actor.PrintMessageAsync("Parameter cannot be empty.", System.Drawing.Color.Red);
    }
  }


  /// <summary>
  /// Command for when admins want to reload the configuration
  /// </summary>
  [Command("spcPlugin_reload_config")]
  [CommandDescription("To reload Specialty Overhaul Plugin configuration (Admin Only)")]
  public class ReloadConfigCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public ReloadConfigCommand(SpecialtyOverhaul plugin, IServiceProvider serviceProvider) : base(serviceProvider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() { await Task.Run(_plugin.RefreshConfig); }
  }

  
  /// <summary>
  /// Command when a player wants to know their level(s)
  /// </summary>
  [Command("GetLevel")]
  [CommandDescription("To get specialty level data")]
  [CommandSyntax("<specialty>/<skill>")]
  public class GetLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GetLevelCommand(SpecialtyOverhaul plugin, IServiceProvider serviceProvider) : base(serviceProvider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        if (Context.Parameters.Length > 0) {
          UnturnedUser? user = _plugin.UnturnedUserProviderInstance.GetUser(new CSteamID(ulong.Parse(Context.Actor.Id)));
          if (user == null)
            throw new Exception(string.Format("Id of {0} is invalid.", Context.Actor.Id));

          await CommandParameterParser.ParseToMessage_GetLevel(_plugin, Context, user, await Context.Parameters.GetAsync<string>(0));
        } else
          await CommandParameterParser.Print_OnNoParam(Context);
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GetLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }


  /// <summary>
  /// Command for when an admin want to give level(s) to a player
  /// </summary>
  [Command("GivePlayerLevel")]
  [CommandDescription("Give player level, can set all skills in a specialty (Admin Only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>/<number>")]
  public class GivePlayerLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GivePlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        await CommandParameterParser.ParseParameter1(_plugin, Context, (UnturnedUser user, EPlayerSpeciality spec, byte skillidx, int level) => { _plugin.SkillUpdaterInstance.GivePlayerLevel(user.Player, (byte)spec, skillidx, level); });
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GivePlayerLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }


  /// <summary>
  /// Command for when an admin want to set player level(s)
  /// </summary>
  [Command("SetPlayerLevel")]
  [CommandDescription("Set player level, can set all skills in a specialty (Admin Only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>/<number>")]
  public class SetPlayerLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public SetPlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        await CommandParameterParser.ParseParameter1(_plugin, Context, (UnturnedUser user, EPlayerSpeciality spec, byte skillidx, int level) => { _plugin.SkillUpdaterInstance.SetPlayerLevel(user.Player, (byte)spec, skillidx, level); });
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"SetPlayerLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }


  /// <summary>
  /// Same as GetLevel command, but with user name/id for when an admin wants to know a player level(s)
  /// </summary>
  [Command("GetPlayerLevel")]
  [CommandDescription("Get another player level status (Admin only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>")]
  public class GetPlayerLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GetPlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      if (Context.Parameters.Length > 0) {
        try {
          string _param = await Context.Parameters.GetAsync<string>(0);
          int _idx = _param.IndexOf('/');
          string userstr = _param.Substring(0, _idx);

          UnturnedUser? user = await _plugin.UnturnedUserProviderInstance.FindUserAsync("", userstr, UserSearchMode.FindByNameOrId) as UnturnedUser;
          if (user != null)
            await CommandParameterParser.ParseToMessage_GetLevel(_plugin, Context, user, _param.Substring(_idx + 1));
          else
            await Context.Actor.PrintMessageAsync("User name/id not found.", System.Drawing.Color.Red);
        } catch (Exception e) {
          _plugin.PrintToError("Something went wrong when calling command \"GetPlayerLevel\"");
          _plugin.PrintToError(e.ToString());
        }
      } else
        await Context.Actor.PrintMessageAsync("Parameter cannot be empty.", System.Drawing.Color.Red);
    }
  }
}
