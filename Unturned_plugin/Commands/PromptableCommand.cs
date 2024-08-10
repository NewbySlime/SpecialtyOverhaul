using Cysharp.Threading.Tasks;
using OpenMod.Core.Console;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  public class PromptableCommand: UnturnedCommand {
    private struct _keyData {
      public ActorType _actorType;
      public ulong _actorId;
    }

    protected enum ActorType {
      NONE,
      CONSOLE_ACTOR,
      UNTURNED_USER
    }

    protected delegate void ConfirmedCallback(Object obj);

    protected readonly static string ConfirmPrompt = "confirm";
    protected readonly static TimeSpan ConfirmTime = new(0, 1, 0);

    private readonly static ulong _defaultID = 0;
    private readonly static Mutex _accessorMutex = new Mutex();
    private static Dictionary<(ActorType, ulong), (Object, Type, Timer.Timer)> _promptData = new();

    private readonly string _commandName;


    // assuming that already using mutex to safely accessing the dictionary
    private bool _removePromptData((ActorType, ulong) key) {
      if(_promptData.TryGetValue(key, out var _data)) {
        _data.Item3.Stop();
        _promptData.Remove(key);

        return true;
      }

      return false;
    }

    private void _onTimerFinished(Object? sender, Object? obj) {
      _keyData? _key = obj as _keyData?;

      _accessorMutex.WaitOne();
      if(_key.HasValue) {
        if(_promptData.TryGetValue((_key.Value._actorType, _key.Value._actorId), out var _pair)) {
          _pair.Item3.OnFinished -= _onTimerFinished;
          OnTimeout(_pair.Item1).Wait();
        }

        if(_removePromptData((_key.Value._actorType, _key.Value._actorId))) {
          SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;

          if(plugin != null) {
            switch(_key.Value._actorType) {
              case ActorType.CONSOLE_ACTOR: {
                plugin.PrintToOutput("Command confirming timeout.");

                break;
              }

              case ActorType.UNTURNED_USER: {
                UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(new Steamworks.CSteamID(_key.Value._actorId));
                user?.PrintMessageAsync("Confirming timeout.", System.Drawing.Color.Yellow);

                break;
              }
            }
          }
        }
      }
      _accessorMutex.ReleaseMutex();
    }


    public PromptableCommand(string commandName, IServiceProvider provider): base(provider) { 
      _commandName = commandName;
    }


    protected (ActorType, ulong) getActorInfo() {
      ulong _id = _defaultID;
      ActorType actorType = ActorType.NONE;
      if(Context.Actor is ConsoleActor)
        actorType = ActorType.CONSOLE_ACTOR;
      else if(Context.Actor is UnturnedUser) {
        actorType = ActorType.UNTURNED_USER;

        UnturnedUser? _user = Context.Actor as UnturnedUser;
        if(_user != null)
          _id = _user.Player.SteamId.m_SteamID;
      }

      return (actorType, _id);
    }

    protected async Task askPrompt(string msg, Object obj) {
      var _info = getActorInfo();

      Timer.Timer _timer = new();
      _keyData _key = new() {
        _actorType = _info.Item1,
        _actorId = _info.Item2
      };

      _timer.Interval = ConfirmTime.TotalMilliseconds;
      _timer.EventParameter = _key;
      _timer.OnFinished += _onTimerFinished;

      _accessorMutex.WaitOne();
      _promptData[(_info.Item1, _info.Item2)] = (obj, this.GetType(), _timer);
      _accessorMutex.ReleaseMutex();

      string _separator = " ";
      if(string.IsNullOrEmpty(msg))
        _separator = "";

      await Context.Actor.PrintMessageAsync(msg + _separator + string.Format("Type '{0} confirm' command to confirm.", _commandName));
    }

    protected async Task<bool> checkPrompt() {
      var _info = getActorInfo();

      string _param = (await Context.Parameters.GetAsync<string>(0)).ToLower();
      if(_param == ConfirmPrompt) {
        bool _isConfirmed = false;

        _accessorMutex.WaitOne();
        if(_promptData.TryGetValue((_info.Item1, _info.Item2), out var _pair)) {
          if(_pair.Item2 == this.GetType()) {
            _isConfirmed = true;

            _removePromptData((_info.Item1, _info.Item2));
          }
        }
        _accessorMutex.ReleaseMutex();

        if(_isConfirmed)
          await OnConfirm(_pair.Item1);

        

        if(!_isConfirmed)
          await Context.Actor.PrintMessageAsync("Nothing to confirm.", System.Drawing.Color.Yellow);

        return true;
      }
      else
        return false;
    }


    protected virtual async Task OnTimeout(Object obj) {

    }

    protected virtual async Task OnConfirm(Object obj) {
      throw new NotImplementedException();
    }

    protected override async UniTask OnExecuteAsync() {
      throw new NotImplementedException();
    }
  }
}
