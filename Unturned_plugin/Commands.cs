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

namespace Nekos.SpecialtyPlugin.Commands {
  // CommandParameterParser, as the name suggest, parsing parameters into an object or using callbacks
  //   to further processing data
  static class CommandParameterParser {
    public class ParsingException : Exception {
      private ParsingExceptionType type;
      public ParsingExceptionType Type {
        get { return type; }
      }

      public ParsingException(ParsingExceptionType type) { this.type = type; }
    }

    public enum ParsingExceptionType { WRONG_SPECIALTYNAME, WRONG_SKILLNAME }

    public struct ParamResult_ToSpecialty {
      public EPlayerSpeciality spec;
      public byte skillidx;
      public bool isAllSkill;

      public string nextParam;
    }

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
        } else {
          skillparam = param;
          param = "";
        }
      } else {
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
  }

  static class CommandMiscellaneous {
    public static async Task Print_OnNoParam(ICommandContext currentContext) {
      await currentContext.Actor.PrintMessageAsync("<specialty> parameter can be:", System.Drawing.Color.YellowGreen);

      foreach (var spec in SkillConfig.specskill_indexer) {
        await currentContext.Actor.PrintMessageAsync(string.Format("  {0}", spec.Key), System.Drawing.Color.Green);
      }
    }

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

    // parameters with [id/name]/specialty/skill/level
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

  [Command("spcPlugin_reload_config")]
  [CommandDescription("To reload Specialty Overhaul Plugin configuration (Admin Only)")]
  public class ReloadConfigCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public ReloadConfigCommand(SpecialtyOverhaul plugin, IServiceProvider serviceProvider) : base(serviceProvider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() { await Task.Run(_plugin.RefreshConfig); }
  }

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

          await CommandMiscellaneous.ParseToMessage_GetLevel(_plugin, Context, user, await Context.Parameters.GetAsync<string>(0));
        } else
          await CommandMiscellaneous.Print_OnNoParam(Context);
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GetLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }

  [Command("GivePlayerLevel")]
  [CommandDescription("Give player level, can set all skills in a specialty (Admin Only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>/<number>")]
  public class GivePlayerLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public GivePlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        await CommandMiscellaneous.ParseParameter1(_plugin, Context, (UnturnedUser user, EPlayerSpeciality spec, byte skillidx, int level) => { _plugin.SkillUpdaterInstance.GivePlayerLevel(user.Player, (byte)spec, skillidx, level); });
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"GivePlayerLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }

  [Command("SetPlayerLevel")]
  [CommandDescription("Set player level, can set all skills in a specialty (Admin Only)")]
  [CommandSyntax("<name or id>/<specialty>/<skill>/<number>")]
  public class SetPlayerLevelCommand : UnturnedCommand {
    private readonly SpecialtyOverhaul _plugin;

    public SetPlayerLevelCommand(SpecialtyOverhaul plugin, IServiceProvider provider) : base(provider) { _plugin = plugin; }

    protected override async UniTask OnExecuteAsync() {
      try {
        await CommandMiscellaneous.ParseParameter1(_plugin, Context, (UnturnedUser user, EPlayerSpeciality spec, byte skillidx, int level) => { _plugin.SkillUpdaterInstance.SetPlayerLevel(user.Player, (byte)spec, skillidx, level); });
      } catch (Exception e) {
        _plugin.PrintToError("Something went wrong when calling command \"SetPlayerLevel\"");
        _plugin.PrintToError(e.ToString());
      }
    }
  }

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
            await CommandMiscellaneous.ParseToMessage_GetLevel(_plugin, Context, user, _param.Substring(_idx + 1));
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
