using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Plugins;
using SDG.Unturned;
using OpenMod.Unturned.Players;
using OpenMod.Core.Users;
using OpenMod.Core.Persistence;
using OpenMod.Core.Permissions;
using OpenMod.Core.Localization;
using Nekos.SpecialtyPlugin.Watcher;
using Nekos.SpecialtyPlugin.CustomEvent;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Timer;
using System.Threading;
using Microsoft.Extensions.Primitives;

[assembly:PluginMetadata("Nekos.SpecialtyPlugin", DisplayName = "Specialty Overhaul")]
namespace Nekos.SpecialtyPlugin {
  /// <summary>
  /// The main plugin class
  /// </summary>
  public class SpecialtyOverhaul : OpenModUnturnedPlugin {
    /// <summary>
    /// This mutex is used to make editing _tickCallbacks thread-safe
    /// </summary>
    private static Mutex _tickMutex = new Mutex();

    private readonly IConfiguration m_Configuration;
    private readonly IStringLocalizer m_StringLocalizer;
    private readonly ILogger<SpecialtyOverhaul> m_Logger;

    private readonly OpenModDataStoreAccessor openModDataStoreAccessor;
    private readonly UserDataSeeder userDataSeeder;
    private readonly UserDataStore userDataStore;
    private readonly PermissionRolesDataStore permissionRolesDataStore;
    private readonly DefaultPermissionRoleStore defaultPermissionRoleStore;

    private readonly ConfigurationBasedStringLocalizerFactory configurationBasedStringLocalizerFactory;
    private readonly OpenModStringLocalizer openModStringLocalizer;

    private readonly NonAutoloadWatcher nonAutoloadWatcher;
      
    private IDisposable? _configChangeListener;

    private TickTimer _tickTimer_onError;
    private string _tickTimer_onError_msg = "";


    /// <summary>
    /// A dictionary to store callbacks that tied to certain playerID and specialty and skill enum combined into ushort
    /// </summary>
    private Dictionary<KeyValuePair<ulong, ushort>, KeyValuePair<OnTickInterval, object>> _tickCallbacks = new Dictionary<KeyValuePair<ulong, ushort>, KeyValuePair<OnTickInterval, object>>();

    private Dictionary<ulong, UnturnedPlayer> _players = new Dictionary<ulong, UnturnedPlayer>();

    private SkillUpdater skillUpdater;
    public SkillUpdater SkillUpdaterInstance {
      get { 
        return skillUpdater; 
      }
    }

    private SkillConfig skillConfig;
    public SkillConfig SkillConfigInstance {
      get {
        return skillConfig; 
      }
    }

    private UnturnedUserProvider userProvider;
    public UnturnedUserProvider UnturnedUserProviderInstance {
      get { 
        return userProvider; 
      }
    }

    private static SpecialtyOverhaul? _instance = null;
    public static SpecialtyOverhaul? Instance {
      get { 
        return _instance;
      }
    }

    private TickTimer _tickTimer;
    public TickTimer TickTimerInstance {
      get {
        return _tickTimer;
      }
    }

    /// <summary>
    /// A class that contains player datas, used for calling events. It only has UnturnedPlayer atm, who knows in the future that this class might need more than one data
    /// </summary>
    public class PlayerData : EventArgs {
      public UnturnedPlayer player;
      
      /// <param name="player">Current UnturnedPlayer object</param>
      public PlayerData(UnturnedPlayer player) {
        this.player = player;
      }
    }

    
    // events that this class needed
    public event EventHandler<PlayerData>? OnPlayerConnected;
    public event EventHandler<PlayerData>? OnPlayerDisconnected;
    public event EventHandler<PlayerData>? OnPlayerDied;
    public event EventHandler<PlayerData>? OnPlayerRevived;
    public event EventHandler<PlayerData>? OnPlayerRespawned;

    
    /// <summary>
    /// Delegate for tick event
    /// </summary>
    /// <param name="obj">The object that subscribers use to determine which data that needs to be processed</param>
    /// <param name="removeObj">For telling the event invoker that this data needs to be removed from _tickCallbacks</param>
    public delegate void OnTickInterval(object obj, ref bool removeObj);

    /// <summary>
    /// Callback event for TickTimer. The function call all callbacks contained in _tickCallbacks
    /// </summary>
    private void _onTick(Object? obj, System.EventArgs eventArgs) {
      List<KeyValuePair<ulong, ushort>> _keyListToRemove = new List<KeyValuePair<ulong, ushort>>();
      
      _tickMutex.WaitOne();
      foreach (var child in _tickCallbacks) {
        bool _removeobj = false;
        child.Value.Key(child.Value.Value, ref _removeobj);

        if (_removeobj)
          _keyListToRemove.Add(child.Key);
      }

      foreach (var _key in _keyListToRemove)
        _tickCallbacks.Remove(_key);

      _tickMutex.ReleaseMutex();
    }

    /// <summary>
    /// Tick delegate for notifying server that the plugin has errored
    /// </summary>
    /// <param name="obj">The object that subscribers use to determine which data that needs to be processed</param>
    /// <param name="eventArgs">For telling the event invoker that this data needs to be removed from _tickCallbacks</param>
    private async void _onTick_error(Object? obj, System.EventArgs eventArgs) {
      var users = await userProvider.GetUsersAsync("");
      foreach(var user in users)
        await user.PrintMessageAsync(_tickTimer_onError_msg, System.Drawing.Color.OrangeRed);
    }

    /// <summary>
    /// Starting TickTimer to notify error to server
    /// </summary>
    /// <param name="msg">Error message</param>
    private void _startTickError(string msg) {
      _tickTimer_onError_msg = msg;
      _tickTimer_onError.StartTick();
    }

    /// <summary>
    /// Stopping TickTimer that used for notifying error to server
    /// </summary>
    private void _stopTickError() {
      _tickTimer_onError.StopTick();
    }

    /// <summary>
    /// Callback for when config changed
    /// </summary>
    /// <param name="state">Object that passed from registering callback</param>
    private void _onConfigChanged(Object state) {
      Task.Run(RefreshConfig);
    }

    /// <summary>
    /// Turning EPlayerSpecialty enum and certain skill index into ushort that can be used for _tickCallbacks keys
    /// </summary>
    /// <param name="spec">The Specialty</param>
    /// <param name="idx">The Skill index</param>
    /// <returns>Keys from combining EPlayerSpecialty enum and skill index</returns>
    private ushort _toUshort(EPlayerSpeciality spec, byte idx) {
      return (ushort)(((byte)spec << 8) | idx);
    }


    public SpecialtyOverhaul(IConfiguration configuration, IStringLocalizer stringLocalizer, ILogger<SpecialtyOverhaul> logger, IServiceProvider serviceProvider) : base(serviceProvider) {
      m_Configuration = configuration;
      m_StringLocalizer = stringLocalizer;
      m_Logger = logger;

      /**
       * In order to get an openmod classes, what you need is to create other classes that contains an interface that you needed.
       * And searching through the documentation to get what you needed, also there's no explanation on how to get those dependencies.
       * 
       * 
       * This literally took me 2 days figuring out what goes where, so just I can use these functions and create UnturnedUserProvider.
       */

      openModDataStoreAccessor = new OpenModDataStoreAccessor(Runtime);
      userDataStore = new UserDataStore(openModDataStoreAccessor, Runtime);
      permissionRolesDataStore = new PermissionRolesDataStore(new Logger<PermissionRolesDataStore>(new LoggerFactory()), openModDataStoreAccessor, Runtime, EventBus);
      defaultPermissionRoleStore = new DefaultPermissionRoleStore(permissionRolesDataStore, userDataStore, Runtime, EventBus);
      userDataSeeder = new UserDataSeeder(userDataStore, defaultPermissionRoleStore);

      configurationBasedStringLocalizerFactory = new ConfigurationBasedStringLocalizerFactory(new Logger<ConfigurationBasedStringLocalizer>(new LoggerFactory()));
      openModStringLocalizer = new OpenModStringLocalizer(configurationBasedStringLocalizerFactory, Runtime);

      userProvider = new UnturnedUserProvider(EventBus, openModStringLocalizer, userDataSeeder, userDataStore, Runtime);

      skillConfig = new SkillConfig(this, m_Configuration);
      skillUpdater = new SkillUpdater(this);

      nonAutoloadWatcher = new NonAutoloadWatcher();

      _tickTimer = new TickTimer(0);

      // ticking every 3 minutes
      _tickTimer_onError = new TickTimer(180);
    }


    /// <summary>
    /// Invoked when the plugin needs to be loaded/initialized
    /// </summary>
    protected override async UniTask OnLoadAsync() {
      _instance = this;
      _tickTimer_onError.OnTick += _onTick_error;

      try {
        await Task.Run(RefreshConfig);

        var userCollection = userProvider.GetOnlineUsers();
        foreach (var user in userCollection) {
          await skillUpdater.LoadExp(user.Player);
          
          UnturnedUserRecheckEvent userRecheck = new UnturnedUserRecheckEvent(user);
          await EventBus.EmitAsync(this, this, userRecheck);
        }

        _tickTimer.OnTick += _onTick;
        _tickTimer.ChangeTickInterval(skillConfig.GetTickInterval());
        _tickTimer.StartTick();

        _configChangeListener = ChangeToken.OnChange(() => m_Configuration.GetReloadToken(), _onConfigChanged, this);

        await UniTask.SwitchToMainThread();
        m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_start"]);
        await UniTask.SwitchToThreadPool();
      }
      catch(Exception ex) {
        PrintToError("Something wrong happened.");
        PrintToError(ex.ToString());
        PrintToError("");
        PrintToError("Please contact dev with server logs attached.");

        _startTickError(string.Format("{0} had problem loading up. Contact an admin.", this.ToString()));
      }
    }


    /// <summary>
    /// Invoked when the plugin unloading/"deconstruct" itself. Called when openmod restarted or the server shuts down
    /// </summary>
    protected override async UniTask OnUnloadAsync() {

      _tickTimer.StopTick();
      _tickTimer.OnTick -= _onTick;
      _stopTickError();
      _tickTimer_onError.OnTick -= _onTick_error;

      _tickCallbacks.Clear();

      _instance = null;
      await skillUpdater.SaveAll();

      _configChangeListener?.Dispose();

      permissionRolesDataStore.Dispose();
      userProvider.Dispose();
      await userDataStore.DisposeAsync();

      await UniTask.SwitchToMainThread();
      m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_stop"]);
      await UniTask.SwitchToThreadPool();
    }


    /**
     * For console outputs, it needs ILogger interface directly from openmod (passed when constructing the plugin class).
     */

    /// <summary>
    /// Printing to output. This only works when using debug build
    /// </summary>
    /// <param name="output">The string that the plugin wants to tell</param>
    public async void PrintToOutput(string output) {
#if DEBUG
      await UniTask.SwitchToMainThread();
      m_Logger.LogInformation(output);
      await UniTask.SwitchToThreadPool();
#endif
    }

    /// <summary>
    /// Print as warning messages
    /// </summary>
    /// <param name="output">The string that the plugin wants to tell</param>
    public async void PrintWarning(string output) {
      await UniTask.SwitchToMainThread();
      m_Logger.LogWarning(output);
      await UniTask.SwitchToThreadPool();
    }

    /// <summary>
    /// Print as error messages
    /// </summary>
    /// <param name="output">The string that the plugin wants to tell</param>
    public async void PrintToError(string output) {
      await UniTask.SwitchToMainThread();
      m_Logger.LogError(output);
      await UniTask.SwitchToThreadPool();
    }

    /// <summary>
    /// Calling event when a player connects
    /// </summary>
    /// <param name="playerData">Parameter for the event</param>
    public void CallEvent_OnPlayerConnected(PlayerData playerData) {
      OnPlayerConnected?.Invoke(this, playerData);
    }

    /// <summary>
    /// Calling event when a player disconnects
    /// </summary>
    /// <param name="playerData">Parameter for the event</param>
    public void CallEvent_OnPlayerDisconnected(PlayerData playerData) {
      OnPlayerDisconnected?.Invoke(this, playerData);
    }

    /// <summary>
    /// Calling event when a player respawning
    /// </summary>
    /// <param name="playerData">Parameter for the event</param>
    public void CallEvent_OnPlayerRespawned(PlayerData playerData) {
      OnPlayerRespawned?.Invoke(this, playerData);
    }

    /// <summary>
    /// Calling event when a player dies
    /// </summary>
    /// <param name="playerData">Parameter for the event</param>
    public void CallEvent_OnPlayerDied(PlayerData playerData) {
      OnPlayerDied?.Invoke(this, playerData);
    }

    /// <summary>
    /// Calling event when a player revived by other player
    /// </summary>
    /// <param name="playerData">Parameter for the event</param>
    public void CallEvent_OnPlayerRevived(PlayerData playerData) {
      OnPlayerRevived?.Invoke(this, playerData);
    }

    /// <summary>
    /// For when refreshing/re-reading configuration
    /// </summary>
    public async Task RefreshConfig() {
      if(skillConfig.RefreshConfig()) {
        _stopTickError();
        _tickTimer.ChangeTickInterval(skillConfig.GetTickInterval());

        var users = UnturnedUserProviderInstance.GetOnlineUsers();
        foreach(var user in users) {
          if(user != null) {
            await skillUpdater.RecalculateSpecialty(user.Player);
          }
        }
      }
      else
        _startTickError(string.Format("{0} had problem reading configuration. Contact an admin.", this.ToString()));
    }

    /// <summary>
    /// Adding/subscribing callbacks for tick event (an event that called every ticks. Look <see cref="TickTimer"/> class).
    /// </summary>
    /// <param name="playerID">The player's steam ID</param>
    /// <param name="spec">What specialty does it needs to keep track</param>
    /// <param name="idx">What skill index does it needs to keep track</param>
    /// <param name="cb">The callback when a tick happens</param>
    /// <param name="obj">The object that the subscriber uses</param>
    public async void AddCallbackOnTick(ulong playerID, EPlayerSpeciality spec, byte idx, OnTickInterval cb, object obj) {
      await Task.Run(() => {
        _tickMutex.WaitOne();
        KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
        _tickCallbacks[key] = new KeyValuePair<OnTickInterval, object>(cb, obj);
        _tickMutex.ReleaseMutex();
      });
    }

    /// <summary>
    /// Removing/unsubscribing callbacks from tick event
    /// </summary>
    /// <param name="playerID">The player's steam ID</param>
    /// <param name="spec">What specialty does it needs to keep track</param>
    /// <param name="idx">What skill index does it needs to keep track</param>
    public async void RemoveCallbackOnTick(ulong playerID, EPlayerSpeciality spec, byte idx) {
      await Task.Run(() => {
        _tickMutex.WaitOne();
        KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
        _tickCallbacks.Remove(key);
        _tickMutex.ReleaseMutex();
      });
    }

    /// <summary>
    /// To check if certain playerID and certain specialty and skill is subscribed
    /// </summary>
    /// <param name="playerID">The player's steam ID</param>
    /// <param name="spec">What specialty does it needs to keep track</param>
    /// <param name="idx">What skill index does it needs to keep track</param>
    public bool OnTickContainsKey(ulong playerID, EPlayerSpeciality spec, byte idx) {
      KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
      return _tickCallbacks.ContainsKey(key);
    }
  }
}