using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Binding;
using Nekos.SpecialtyPlugin.Error;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Persistance;
using Nekos.SpecialtyPlugin.Utils;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Core.Plugins.Events;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.SteamworksProvider;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static Nekos.SpecialtyPlugin.Commands.CommandParameterParser;


namespace Nekos.SpecialtyPlugin.Commands {

  [Command("EditSkill")]
  [CommandDescription("Editing Player level data. Can also change, create, or delete level files. Use `help` to get subcommand names. Player will be notified if skill data is edited.")]
  [CommandSyntax("EditSKill <subcommand> <param-1>/<param-2>/.../<param-n>")]
  [CommandActor(typeof(ConsoleActor))]
  public class EditSkillCommand: UnturnedCommand {
    private enum _internalerror_codes {
      ALREADY_OPENED,
      CURRENTLY_CLOSED,
      FILE_EXISTED,
      
      WRONG_CHARID,
      WRONG_STEAMID,

      USER_NOTFOUND,

      PARAMETER_INVALID,
      PARSE_ERROR_SPECIALITY,
      PARSE_ERROR_SKILL,
      PARSE_ERROR_NUMBER,
      PARSE_ERROR_SKILLSET,
      PARSE_ERROR_UNKNOWN,

      WRONG_COMMAND
    }


    private enum _current_subcommand {
      NONE,
      CREATE,
      OPEN,
      OPENONLINE,
      SAVE,
      ERASE,
      CLOSE,
      SETLEVEL,
      GIVELEVEL,
      GETLEVEL,
      GIVEEXP,
      GETEXP,
      SETEXP,
      GETSKILLSET,
      SETSKILLSET,
      HELP,
      INFOCURRENT,
      DUMPDATA,
      UNDO,
      REDO,
      CANCEL
    }



    // why did i use this method
    private static readonly (string, string)[] _subcommand_desc = new (string, string)[] {

      ("\n---File subcommand---", ""),
      ("create", "Creating skill file data. Existing file data will be overwritten.\nSyntax: <steam id>/<character id>" ),
      ("open", "Opening existing skill file data.\nSyntax: <steam id>/<character id>"),
      ("openonline", "Opening skill file data from currently online players.\nSyntax: <name or steam id>"),
      ("save", "For saving edited data to file data."),
      ("erase", "Erasing/resetting player level data. If player is online, their level will directly reset."),
      ("close", "Closing currently opened file, it will not save upon closing."),

      ("\n---Skill subcommand---", ""),
      ("setlevel", "Set player level.\nSyntax: [specialty]/<skill>/<level>"),
      ("givelevel", "Give player level.\nSyntax: [speciality]/<skill>/<level>"),
      ("getlevel", "Get current player level.\nSyntax: [speciality]/<skill>"),
      ("setexp", "Set player exp.\nSyntax: [speciality]/<skill>/<exp>"),
      ("giveexp", "Giving exp to certain skill.\nSyntax: [speciality]/<skill>/<amount>"),
      ("getexp", "Get current player exp for certain skill.\nSyntax: [speciality]/<skill>"),

      ("\n---Misc subcommand---", ""),
      ("help", "Showing this command descriptions."),
      ("infocurrent", "Show player info on currently opened file."),
      ("undo", string.Format("Undo edit(s) (max step: {0}).\nSyntax: [number of step]", _max_undo)),
      ("redo", string.Format("Redo edit(s) (max step: {0}).\nSyntax: [number of step]", _max_undo)),
      ("cancel", "To cancel out last command."),
      ("dump_data", "To dump skill data to output.")
    };


    private static readonly int _max_undo = 20;

    private SpecialtyOverhaul plugin;
    private static SkillDataEditor? _currentSkillEditor = null;
    private static Binder _binder = new Binder();

    private static _current_subcommand _last_subcommand = _current_subcommand.NONE;
    private static object? _last_object = null;

    private static void _printHelp(ICommandContext currentContext) {
      string _help_desc = "";

      foreach(var _pair in _subcommand_desc)
        _help_desc += string.Format("{0}\n\t{1}\n\n", _pair.Item1, _pair.Item2);

      currentContext.Actor.PrintMessageAsync(_help_desc).Wait();
    }


    private (CSteamID, byte) _parse_user(string[] subparams) {
      CSteamID steamID;
      byte charID;

      if(subparams.Length > 0 && ulong.TryParse(subparams[0], out ulong val1))
        steamID = new(val1);
      else
        throw new InternalErrorCodeException((int)_internalerror_codes.WRONG_STEAMID);

      if(subparams.Length > 1 && byte.TryParse(subparams[1], out byte val2))
        charID = val2;
      else
        throw new InternalErrorCodeException((int)_internalerror_codes.WRONG_CHARID);

      return (steamID, charID);
    }

    private void _parseHelper1(string _params, out ParamResult_ToSpecialty paramResult) {
      try {
        ToSpecialty(out paramResult, _params);
        if(paramResult.isAllSkill)
          throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_SKILL);
      }
      catch(ParsingException e) {
        switch(e.Type) {
          case ParsingExceptionType.WRONG_SPECIALTYSKILLNAME:
          case ParsingExceptionType.WRONG_SPECIALTYNAME:
            throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_SPECIALITY);

          case ParsingExceptionType.WRONG_SKILLNAME:
            throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_SKILL);

          default:
            throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_UNKNOWN);
        }
      }
    }

    private void _parseHelper2(string _params, out ParamResult_ToSkillset paramResult) {
      try {
        ToSkillset(out paramResult, _params);
      }
      catch(ParsingException e) {
        switch(e.Type) {
          case ParsingExceptionType.WRONG_SKILLSET:
            throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_SKILLSET);

          default:
            throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_UNKNOWN);
        }
      }
    }


    protected override async UniTask OnExecuteAsync() {
      if(Context.Parameters.Length <= 0) {
        _printHelp(Context);
        return;
      }

      try {
        switch(await Context.Parameters.GetAsync<string>(0)) {
          case "create": {
            _last_subcommand = _current_subcommand.CREATE;

            if(_currentSkillEditor != null)
              throw new InternalErrorCodeException((int)_internalerror_codes.ALREADY_OPENED);

            string[] _subparams = (await Context.Parameters.GetAsync<string>(1)).Split(param_splitter);

            var _userid = _parse_user(_subparams);
            SteamPlayerID _newSteamPlayer = new(_userid.Item1, _userid.Item2, "", "", "", new(0));

            if(SkillPersistancePool.IsSkillPersistanceFileExists(_newSteamPlayer))
              throw new InternalErrorCodeException((int)_internalerror_codes.FILE_EXISTED);

            ISkillPersistance skillPersistance = SkillPersistancePool.GetSkillPersistance(_newSteamPlayer, _binder);

            _currentSkillEditor = new(skillPersistance);
            _currentSkillEditor.SetMaxUndo(_max_undo);

            break;
          }


          case "open": {
            _last_subcommand = _current_subcommand.OPEN;

            if(_currentSkillEditor != null) 
              throw new InternalErrorCodeException((int)_internalerror_codes.ALREADY_OPENED);

            string[] _subparams = (await Context.Parameters.GetAsync<string>(1)).Split(param_splitter);

            var _userid = _parse_user(_subparams);
            SteamPlayerID _newSteamPlayer = new(_userid.Item1, _userid.Item2, "", "", "", new(0));

            if(!SkillPersistancePool.IsSkillPersistanceFileExists(_newSteamPlayer))
              await Context.Actor.PrintMessageAsync("File does not exist. Will create a new file.", System.Drawing.Color.Yellow);

            ISkillPersistance skillPersistance = SkillPersistancePool.GetSkillPersistance(_newSteamPlayer, _binder);

            _currentSkillEditor = new(skillPersistance);
            _currentSkillEditor.SetMaxUndo(_max_undo);

            break;
          }


          case "openonline": {
            _last_subcommand = _current_subcommand.OPENONLINE;

            if(_currentSkillEditor != null)
              throw new InternalErrorCodeException((int)_internalerror_codes.ALREADY_OPENED);

            var _userconfidence = SearchUserNameConfidence(plugin.UnturnedUserProviderInstance.GetOnlineUsers(), (await Context.Parameters.GetAsync<string>(1)));
            if(_userconfidence.Item2 == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.USER_NOTFOUND);

            ISkillPersistance skillPersistance = SkillPersistancePool.GetSkillPersistance(_userconfidence.Item2.Player.SteamPlayer.playerID, _binder);

            _currentSkillEditor = new(skillPersistance);
            _currentSkillEditor.SetMaxUndo(_max_undo);

            break;
          }


          case "save": {
            _last_subcommand = _current_subcommand.SAVE;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _currentSkillEditor.SkillPersistance.AddMsgFlag(ISkillPersistance.PersistanceMsg.PERSISTANCE_EDITED);
            _currentSkillEditor.SaveData();

            break;
          }


          case "erase": {
            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            if(_last_subcommand == _current_subcommand.ERASE) {
              _currentSkillEditor.EraseData();
              await Context.Actor.PrintMessageAsync("Successfully erased.");

              SkillPersistancePool.UnbindSkillPersistance(_currentSkillEditor.SkillPersistance.PlayerID, _binder);
              _currentSkillEditor = null;
            }
            else {
              _last_subcommand = _current_subcommand.ERASE;
              await Context.Actor.PrintMessageAsync("Are you sure you want to ERASE (IRREVERSIBLE) this skill data? (Run `erase` command again to confirm, or `cancel`)");
            }

            break;
          }


          case "close": {
            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            if(_last_subcommand == _current_subcommand.CLOSE) {
              SkillPersistancePool.UnbindSkillPersistance(_currentSkillEditor.SkillPersistance.PlayerID, _binder);
              _currentSkillEditor = null;
            }
            else {
              _last_subcommand = _current_subcommand.CLOSE;
              await Context.Actor.PrintMessageAsync("All changed data will be gone. Are you sure you want to close? (Run `close` command again to confirm, or `cancel`)");
            }

            break;
          }


          case "setlevel": {
            _last_subcommand = _current_subcommand.SETLEVEL;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            if(float.TryParse(paramResult.nextParam, out float _val1))
              _currentSkillEditor.Level(paramResult.spec, paramResult.skillidx, _val1);
            else
              throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_NUMBER);

            break;
          }


          case "givelevel": {
            _last_subcommand = _current_subcommand.GIVELEVEL;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            if(float.TryParse(paramResult.nextParam, out float _val1)) {
              float _currentlvl = _currentSkillEditor.Level(paramResult.spec, paramResult.skillidx);
              _currentSkillEditor.Level(paramResult.spec, paramResult.skillidx, _currentlvl+_val1);
            }
            else
              throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_NUMBER);

            break;
          }


          case "getlevel": {
            _last_subcommand = _current_subcommand.GETLEVEL;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            await Context.Actor.PrintMessageAsync(
              string.Format(
                "Current level: {0}",
                _currentSkillEditor.Level(paramResult.spec, paramResult.skillidx)
              )
            );

            break;
          }


          case "setexp": {
            _last_subcommand = _current_subcommand.SETEXP;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            if(int.TryParse(paramResult.nextParam, out int _val1))
              _currentSkillEditor.LevelExp(paramResult.spec, paramResult.skillidx, _val1);
            else
              throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_NUMBER);

            break;
          }


          case "giveexp": {
            _last_subcommand = _current_subcommand.GIVEEXP;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            if(int.TryParse(paramResult.nextParam, out int _val1)) {
              int _currentexp = _currentSkillEditor.LevelExp(paramResult.spec, paramResult.skillidx);
              _currentSkillEditor.LevelExp(paramResult.spec, paramResult.skillidx, _currentexp+_val1);
            }
            else
              throw new InternalErrorCodeException((int)_internalerror_codes.PARSE_ERROR_NUMBER);

            break;
          }


          case "getexp": {
            _last_subcommand = _current_subcommand.GETEXP;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            _parseHelper1((await Context.Parameters.GetAsync<string>(1)), out ParamResult_ToSpecialty paramResult);

            await Context.Actor.PrintMessageAsync(string.Format("Current exp: {0}", _currentSkillEditor.LevelExp(paramResult.spec, paramResult.skillidx)));
            break;
          }


          case "setskillset": {
            _last_subcommand = _current_subcommand.SETSKILLSET;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);
#error

            break;
          }


          case "getskillset": {
            _last_subcommand = _current_subcommand.GETSKILLSET;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            await Context.Actor.PrintMessageAsync(string.Format("Current skillset: {0}", SkillConfig.skillset_indexer_inverse[(byte)_currentSkillEditor.GetPlayerSkillset()]));

            break;
          }


          case "help":
            _last_subcommand = _current_subcommand.HELP;

            _printHelp(Context);
            break;


          case "infocurrent": {
            _last_subcommand = _current_subcommand.INFOCURRENT;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            SteamPlayerID playerID = _currentSkillEditor.SkillPersistance.PlayerID;
            await Context.Actor.PrintMessageAsync(string.Format("\bPlayer Name: {0}\n\tSteamID: {1}\n\tCharacterID: {2}", playerID.playerName, playerID.steamID.m_SteamID, playerID.characterID));
            break;
          }


          case "undo": {
            _last_subcommand = _current_subcommand.UNDO;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            if(!_currentSkillEditor.UndoData())
              await Context.Actor.PrintMessageAsync("Cannot undo more.");

            break;
          }


          case "redo": {
            _last_subcommand = _current_subcommand.REDO;

            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            if(!_currentSkillEditor.RedoData())
              await Context.Actor.PrintMessageAsync("Cannot redo more.");

            break;
          }



          case "cancel":
            _last_subcommand = _current_subcommand.CANCEL;
            break;


          case "dump_data": {
            _last_subcommand = _current_subcommand.DUMPDATA;
            if(_currentSkillEditor == null)
              throw new InternalErrorCodeException((int)_internalerror_codes.CURRENTLY_CLOSED);

            string _dumped_log = "";

            _dumped_log += string.Format(
              "Skillset: {0}", 
              SkillConfig.skillset_indexer_inverse[(byte)_currentSkillEditor.GetPlayerSkillset()]
            );
            _dumped_log += "\n\n";

            _dumped_log += "Skill data:";
            SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
              float _lvl = _currentSkillEditor.Level(spec, skillidx);
              int _maxlevel = plugin.SkillConfigInstance.GetMaxLevel(_currentSkillEditor.GetPlayerSkillset(), spec, skillidx);
              _dumped_log += string.Format("\n\t{0}.{1}: {2}/{3}, {4}", (int)spec, (int)skillidx, _lvl, _maxlevel, _currentSkillEditor.LevelExp(spec, skillidx));
            });
            _dumped_log += "\n\n";

            _dumped_log += string.Format(
              "Excess Exp: {0}",
              _currentSkillEditor.ExcessExp
            );

            await Context.Actor.PrintMessageAsync(_dumped_log);

            break;
          }


          default:
            _last_subcommand = _current_subcommand.NONE;
            throw new InternalErrorCodeException((int)_internalerror_codes.WRONG_COMMAND);
        }
      }
      catch(InternalErrorCodeException e) {
        switch((_internalerror_codes)e.ErrorCode) {
          case _internalerror_codes.ALREADY_OPENED:
            await Context.Actor.PrintMessageAsync("Cannot open file while currently opening a file. Close file before opening.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.CURRENTLY_CLOSED:
            await Context.Actor.PrintMessageAsync("Nothing to work on, currently no opened file.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.FILE_EXISTED:
            await Context.Actor.PrintMessageAsync("File already existed. Use `open` to open existing data.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.WRONG_CHARID:
            await Context.Actor.PrintMessageAsync("Cannot parse CharID to unsigned 8-bit number.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.WRONG_STEAMID:
            await Context.Actor.PrintMessageAsync("Cannot parse SteamID to unsigned 64-bit number.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.USER_NOTFOUND:
            await Context.Actor.PrintMessageAsync("User not found.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.PARAMETER_INVALID:
            await Context.Actor.PrintMessageAsync("Parameter is invalid.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.PARSE_ERROR_SPECIALITY:
            await Context.Actor.PrintMessageAsync("Specialty name is invalid.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.PARSE_ERROR_SKILL:
            await Context.Actor.PrintMessageAsync("Skill name is invalid.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.PARSE_ERROR_NUMBER:
            await Context.Actor.PrintMessageAsync("Cannot parse to number.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.PARSE_ERROR_UNKNOWN:
            await Context.Actor.PrintMessageAsync("Something went wrong when parsing.", System.Drawing.Color.Red);
            break;

          case _internalerror_codes.WRONG_COMMAND:
            await Context.Actor.PrintMessageAsync("Wrong command.");
            break;
        }
      }
    }

    public EditSkillCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base(provider) {
      this.plugin = plugin;
    }
  }
}
