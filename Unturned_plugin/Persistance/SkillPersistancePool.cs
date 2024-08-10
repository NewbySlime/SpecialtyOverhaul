using Nekos.SpecialtyPlugin.Binding;
using Nekos.SpecialtyPlugin.CustomEvent;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Persistance
{
  public partial class SkillPersistancePool: IPluginAutoload,
    IEventListener<UnturnedUserConnectedEvent>,
    IEventListener<UnturnedUserDisconnectedEvent> {


    
    private static Dictionary<(CSteamID, byte), SkillPersistance> _skillDataPool = new();
    private readonly static Binder _poolBinder = new Binder();

    private readonly static Mutex _poolAccessor_mutex = new();


    protected override void Instantiate(SpecialtyOverhaul plugin) {
      _skillDataPool.Clear();

      foreach(var _user in plugin.UnturnedUserProviderInstance.GetOnlineUsers()) {
        SkillPersistance? _persistance = GetSkillPersistance(_user.Player.SteamPlayer.playerID, _poolBinder, _user) as SkillPersistance;
      }
    }

    protected override void Destroy(SpecialtyOverhaul plugin) {
      foreach(var _pair in _skillDataPool) 
        _pair.Value.Save();

      _skillDataPool.Clear();
    }




    public static ISkillPersistance GetSkillPersistance(SteamPlayerID playerID, Binder bind, UnturnedUser? user = null) {
      _poolAccessor_mutex.WaitOne();

      SkillPersistance _persistance;
      if(_skillDataPool.TryGetValue((playerID.steamID, playerID.characterID), out var skillPersistance))
        _persistance = skillPersistance;
      else {
        _persistance = new SkillPersistance(playerID, user);
        _skillDataPool[(playerID.steamID, playerID.characterID)] = _persistance;

        SpecialtyOverhaul.Instance?.SkillConfigInstance.Calculation.ReCalculateAllSkillTo(user, _persistance.ExpData);
      }

      _persistance.Bind(bind);
      _poolAccessor_mutex.ReleaseMutex();

      return _persistance;
    }

    public static void UnbindSkillPersistance(SteamPlayerID playerID, Binder bind) {
      _poolAccessor_mutex.WaitOne();
      if(_skillDataPool.TryGetValue((playerID.steamID, playerID.characterID), out var skillPersistance))
        skillPersistance.Unbind(bind);

      _poolAccessor_mutex.ReleaseMutex();
    }


    public static void GetSkillPersistance_WrapperFunction(SteamPlayerID playerID, UnturnedUser? user, Binder bind, Action<ISkillPersistance> callback) {
      ISkillPersistance persistance = GetSkillPersistance(playerID, bind, user);
      callback.Invoke(persistance);
      UnbindSkillPersistance(playerID, bind);
    }

    public async Task GetSkillPersistance_WrapperFunction(SteamPlayerID playerID, UnturnedUser? user, Binder bind, Func<ISkillPersistance, Task> callback) {
      ISkillPersistance persistance = GetSkillPersistance(playerID, bind, user);
      await callback.Invoke(persistance);
      UnbindSkillPersistance(playerID, bind);
    }


    public static bool IsSkillPersistanceFileExists(SteamPlayerID playerID) {
      if(_skillDataPool.ContainsKey((playerID.steamID, playerID.characterID)))
        return true;

      return SkillPersistance.FileExists(playerID);
    }

    public static void SaveAllSkillPersistance() {
      _poolAccessor_mutex.WaitOne();

      foreach(var persistance in _skillDataPool)
        persistance.Value.Save();

      _poolAccessor_mutex.ReleaseMutex();
    }
    

    public async Task HandleEventAsync(Object? obj, UnturnedUserConnectedEvent @event) {
      ISkillPersistance.PersistanceMsg _msg = ISkillPersistance.PersistanceMsg.NONE;
      if(_skillDataPool.ContainsKey((@event.User.SteamId, @event.User.Player.SteamPlayer.playerID.characterID)))
        _msg = ISkillPersistance.PersistanceMsg.PERSISTANCE_STILL_OPENED;

      SkillPersistance? persistance = GetSkillPersistance(@event.User.Player.SteamPlayer.playerID, _poolBinder, @event.User) as SkillPersistance;
      if(persistance != null) 
        persistance.AddMsgFlag(_msg);
    }

    public async Task HandleEventAsync(Object? obj, UnturnedUserDisconnectedEvent @event) {
      SteamPlayerID _playerID = @event.User.Player.SteamPlayer.playerID;
      if(_skillDataPool.TryGetValue((_playerID.steamID, _playerID.characterID), out var _pair)) {
        _pair._user = null;
        _pair.Save();
      }
      
      UnbindSkillPersistance(@event.User.Player.SteamPlayer.playerID, _poolBinder);
    }
  }
}
