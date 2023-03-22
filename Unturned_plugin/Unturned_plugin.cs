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
using Steamworks;
using OpenMod.Unturned.Players;
using OpenMod.Core.Users;
using OpenMod.Core.Persistence;
using OpenMod.Core.Permissions;
using OpenMod.Core.Localization;
using OpenMod.Core.Helpers;
using Nekos.SpecialtyPlugin.Watcher;
using System.IO;
using OpenMod.Core.Eventing;
using OpenMod.API.Eventing;
using Nekos.SpecialtyPlugin.CustomEvent;
using System.Threading;

[assembly:PluginMetadata("Nekos.SpecialtyPlugin", DisplayName = "Specialty Overhaul")]
namespace Nekos.SpecialtyPlugin {
  public class SpecialtyOverhaul : OpenModUnturnedPlugin {
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

    private Dictionary<KeyValuePair<ulong, ushort>, KeyValuePair<OnTickInterval, object>> _tickCallbacks = new Dictionary<KeyValuePair<ulong, ushort>, KeyValuePair<OnTickInterval, object>>();

    private SkillUpdater skillUpdater;
    public SkillUpdater SkillUpdaterInstance {
      get { return skillUpdater; }
    }

    private SkillConfig skillConfig;
    public SkillConfig SkillConfigInstance {
      get { return skillConfig; }
    }

    private UnturnedUserProvider userProvider;
    public UnturnedUserProvider UnturnedUserProviderInstance {
      get { return userProvider; }
    }

    private static SpecialtyOverhaul? _instance = null;
    public static SpecialtyOverhaul? Instance {
      get { return _instance; }
    }

    private TickTimer _tickTimer;
    public TickTimer TickTimerInstance {
      get { return _tickTimer; }
    }

    public class PlayerData : EventArgs {
      public UnturnedPlayer player;

      public PlayerData(UnturnedPlayer player) { this.player = player; }
    }

    public event EventHandler<PlayerData>? OnPlayerConnected;
    public event EventHandler<PlayerData>? OnPlayerDisconnected;
    public event EventHandler<PlayerData>? OnPlayerDied;
    public event EventHandler<PlayerData>? OnPlayerRevived;
    public event EventHandler<PlayerData>? OnPlayerRespawned;

    public delegate void OnTickInterval(object obj, ref bool removeObj);

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

    private ushort _toUshort(EPlayerSpeciality spec, byte idx) { return (ushort)(((byte)spec << 8) | idx); }

    public SpecialtyOverhaul(IConfiguration configuration, IStringLocalizer stringLocalizer, ILogger<SpecialtyOverhaul> logger, IServiceProvider serviceProvider) : base(serviceProvider) {
      m_Configuration = configuration;
      m_StringLocalizer = stringLocalizer;
      m_Logger = logger;

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
    }

    protected override async UniTask OnLoadAsync() {
      _instance = this;

      await Task.Run(() => skillConfig.RefreshConfig());

      var userCollection = userProvider.GetOnlineUsers();
      foreach (var user in userCollection) {
        await skillUpdater.LoadExp(user.Player);

        UnturnedUserRecheckEvent userRecheck = new UnturnedUserRecheckEvent(user);
        await EventBus.EmitAsync(this, this, userRecheck);
      }

      _tickTimer.OnTick += _onTick;
      _tickTimer.ChangeTickInterval(skillConfig.GetTickInterval());
      _tickTimer.StartTick();

      await UniTask.SwitchToMainThread();
      m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_start"]);
      await UniTask.SwitchToThreadPool();
    }

    protected override async UniTask OnUnloadAsync() {
      _tickTimer.OnTick -= _onTick;
      _tickTimer.StopTick();

      _tickCallbacks.Clear();

      _instance = null;
      await skillUpdater.SaveAll();

      await UniTask.SwitchToMainThread();
      m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_stop"]);
      await UniTask.SwitchToThreadPool();
    }

    public async void PrintToOutput(string output) {
#if DEBUG
      await UniTask.SwitchToMainThread();
      m_Logger.LogInformation(output);
      await UniTask.SwitchToThreadPool();
#endif
    }

    public async void PrintWarning(string output) {
      await UniTask.SwitchToMainThread();
      m_Logger.LogWarning(output);
      await UniTask.SwitchToThreadPool();
    }

    public async void PrintToError(string output) {
      await UniTask.SwitchToMainThread();
      m_Logger.LogError(output);
      await UniTask.SwitchToThreadPool();
    }

    public void CallEvent_OnPlayerConnected(PlayerData playerData) {
      OnPlayerConnected?.Invoke(this, playerData);
    }

    public void CallEvent_OnPlayerDisconnected(PlayerData playerData) {
      OnPlayerDisconnected?.Invoke(this, playerData);
    }

    public void CallEvent_OnPlayerRespawned(PlayerData playerData) {
      OnPlayerRespawned?.Invoke(this, playerData);
    }

    public void CallEvent_OnPlayerDied(PlayerData playerData) {
      OnPlayerDied?.Invoke(this, playerData);
    }

    public void CallEvent_OnPlayerRevived(PlayerData playerData) {
      OnPlayerRevived?.Invoke(this, playerData);
    }

    public void RefreshConfig() {
      skillConfig.RefreshConfig();
    }

    public async void AddCallbackOnTick(ulong playerID, EPlayerSpeciality spec, byte idx, OnTickInterval cb, object obj) {
      await Task.Run(() => {
        _tickMutex.WaitOne();
        KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
        _tickCallbacks[key] = new KeyValuePair<OnTickInterval, object>(cb, obj);
        _tickMutex.ReleaseMutex();
      });
    }

    public async void RemoveCallbackOnTick(ulong playerID, EPlayerSpeciality spec, byte idx) {
      await Task.Run(() => {
        _tickMutex.WaitOne();
        KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
        _tickCallbacks.Remove(key);
        _tickMutex.ReleaseMutex();
      });
    }

    public bool OnTickContainsKey(ulong playerID, EPlayerSpeciality spec, byte idx) {
      KeyValuePair<ulong, ushort> key = new KeyValuePair<ulong, ushort>(playerID, _toUshort(spec, idx));
      return _tickCallbacks.ContainsKey(key);
    }
  }

  public class SkillConfig {
    public static Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>> specskill_indexer = new Dictionary<string, KeyValuePair<byte, Dictionary<string, byte>>>(){
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

    public static Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>> specskill_indexer_inverse = new Dictionary<EPlayerSpeciality, KeyValuePair<string, Dictionary<byte, string>>>(){
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

    public static Dictionary<string, byte> skillset_indexer = new Dictionary<string, byte>(){
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
      {"all", 255 },
      {"admin", 255 }
    };

    private static Dictionary<string, ESkillEvent> skillevent_indexer = new Dictionary<string, ESkillEvent>(){
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

    private class config_data {
      public class skillset_updateconfig {
        public int[][] _base_level = specialtyExpData.InitArrayT<int>();
        public float[][] _mult_level = specialtyExpData.InitArrayT<float>();
        public float[][] _multmult_level = specialtyExpData.InitArrayT<float>();
        public float[][] _ondied_edit_level_value = specialtyExpData.InitArrayT<float>();
        public EOnDiedEditType[][] _ondied_edit_level_type = specialtyExpData.InitArrayT<EOnDiedEditType>();

        public static void CopyData(ref skillset_updateconfig dst, in skillset_updateconfig src) {
          specialtyExpData.CopyArrayT<int>(ref dst._base_level, in src._base_level);
          specialtyExpData.CopyArrayT<float>(ref dst._mult_level, in src._mult_level);
          specialtyExpData.CopyArrayT<float>(ref dst._multmult_level, in src._multmult_level);
          specialtyExpData.CopyArrayT<float>(ref dst._ondied_edit_level_value, in src._ondied_edit_level_value);
          specialtyExpData.CopyArrayT<EOnDiedEditType>(ref dst._ondied_edit_level_type, in src._ondied_edit_level_type);
        }
      }

      public float[] skillevent_exp = new float[(int)ESkillEvent.__len];
      public skillset_updateconfig[] skillupdate_configs = new skillset_updateconfig[12];

      public float tickinterval = 0.1f;

      public config_data() {
        for (int i = 0; i < skillupdate_configs.Length; i++) {
          skillupdate_configs[i] = new skillset_updateconfig();
        }
      }
    }
    private config_data config_Data;

    public enum EOnDiedEditType { OFFSET, MULT, BASE }

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

    private class ErrorSettingUpConfig : Exception {
      public ErrorSettingUpConfig(string what) : base(what) {}
    }

    private void _process_configdata_copytoarray<T>(IConfigurationSection section, ref T[][] values, bool create_error) {
      var _iter = section.GetChildren();
      const int _supposedlen = specialtyExpData._skill_offense_count + specialtyExpData._skill_defense_count + specialtyExpData._skill_support_count;
      int _assigned = 0;

      foreach (var child in _iter) {
        string[] keys = child.Key.Split('.');
        if (specskill_indexer.ContainsKey(keys[0]) && keys.Length == 2) {
          var skill_indexer = specskill_indexer[keys[0]];
          if (skill_indexer.Value.ContainsKey(keys[1])) {
            int skill_idx = skill_indexer.Value[keys[1]];
            values[skill_indexer.Key][skill_idx] = child.Get<T>();

            _assigned++;
          } else if (create_error)
            throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
        } else if (create_error)
          throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
      }

      if (_assigned < _supposedlen && create_error)
        throw new ErrorSettingUpConfig(string.Format("The value(s) isn't sufficient enough to fill {0}.", section.Key));
    }

    private void _process_configdata_copytoarray_ondied_edit(IConfigurationSection section, ref config_data.skillset_updateconfig skillset_data, bool create_error) {
      var _iter = section.GetChildren();
      const int _supposedlen = (specialtyExpData._skill_offense_count + specialtyExpData._skill_defense_count + specialtyExpData._skill_support_count) * 2;
      int _assigned = 0;

      foreach (var child in _iter) {
        string[] keys = child.Key.Split('.');
        if (specskill_indexer.ContainsKey(keys[0]) && keys.Length == 3) {
          var skill_indexer = specskill_indexer[keys[0]];
          if (skill_indexer.Value.ContainsKey(keys[1])) {
            int skill_idx = skill_indexer.Value[keys[1]];
            switch (keys[2]) {
              case "value":
                skillset_data._ondied_edit_level_value[skill_indexer.Key][skill_idx] = child.Get<float>();
                break;

              case "type": {
                EOnDiedEditType type = EOnDiedEditType.OFFSET;
                switch (child.Value) {
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
              } break;

              default:
                if (create_error)
                  throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
                break;
            }

            _assigned++;
          } else if (create_error)
            throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
        } else if (create_error)
          throw new ErrorSettingUpConfig(string.Format("Key: '{0}' is an invalid key.", child.Key));
      }

      if (_assigned < _supposedlen && create_error)
        throw new ErrorSettingUpConfig(string.Format("The value(s) isn't sufficient enough to fill {0}.", section.Key));
    }

    private void _process_configdata_skillset(EPlayerSkillset skillset, IConfigurationSection section, bool create_error = false) {
      ref var skillset_data = ref config_Data.skillupdate_configs[(int)skillset];
      var _iter = section.GetChildren();
      foreach (var child in _iter) {
        switch (child.Key) {
          case "base_level_exp":
            _process_configdata_copytoarray<int>(child, ref skillset_data._base_level, create_error);
            break;

          case "mult_level_exp":
            _process_configdata_copytoarray<float>(child, ref skillset_data._mult_level, create_error);
            break;

          case "multmult_level_exp": {
            for (int i_specs = 0; i_specs < specialtyExpData._speciality_count; i_specs++) {
              int _skilllen = 0;
              switch ((EPlayerSpeciality)_skilllen) {
                case EPlayerSpeciality.OFFENSE:
                  _skilllen = specialtyExpData._skill_offense_count;
                  break;

                case EPlayerSpeciality.DEFENSE:
                  _skilllen = specialtyExpData._skill_defense_count;
                  break;

                case EPlayerSpeciality.SUPPORT:
                  _skilllen = specialtyExpData._skill_support_count;
                  break;
              }

              for (int i_skill = 0; i_skill < _skilllen; i_skill++) {
                skillset_data._multmult_level[i_specs][i_skill] = 1.0f;
              }
            }

            _process_configdata_copytoarray<float>(child, ref skillset_data._multmult_level, false);
          } break;

          case "ondied_edit_level_exp":
            _process_configdata_copytoarray_ondied_edit(child, ref skillset_data, create_error);
            break;
        }
      }
    }

    private void _process_configdata_skillsets(IConfigurationSection section) {
      // process default data first
      plugin.PrintToOutput("default key");
      _process_configdata_skillset(EPlayerSkillset.NONE, section.GetSection("default"), true);

      var _iter = section.GetChildren();
      foreach (var child in _iter) {
        if (skillset_indexer.ContainsKey(child.Key)) {
          EPlayerSkillset eSkillset = (EPlayerSkillset)skillset_indexer[child.Key];
          if (eSkillset == EPlayerSkillset.NONE)
            continue;

          if ((int)eSkillset == 255)
            eSkillset = (EPlayerSkillset)config_Data.skillupdate_configs.Length - 1;

          config_data.skillset_updateconfig.CopyData(ref config_Data.skillupdate_configs[(int)eSkillset], in config_Data.skillupdate_configs[(int)EPlayerSkillset.NONE]);
          _process_configdata_skillset(eSkillset, child);
        }
      }
    }

    private void _process_configdata_eventskill(IConfigurationSection section) {
      var _iter = section.GetChildren();
      int _supposedlen = (int)ESkillEvent.__len;
      int _assigned = 0;
      foreach (var child in _iter) {
        if (skillevent_indexer.ContainsKey(child.Key))
          config_Data.skillevent_exp[(int)skillevent_indexer[child.Key]] = child.Get<float>();
        else
          throw new ErrorSettingUpConfig(string.Format("Key: {0} is an invalid key", child.Key));

        _assigned++;
      }

      if (_assigned < _supposedlen)
        throw new ErrorSettingUpConfig(string.Format("The value(s) isn't sufficient enough to fill {0}.", section.Key));
    }

    public SkillConfig(SpecialtyOverhaul plugin, IConfiguration configuration) {
      this.configuration = configuration;
      this.plugin = plugin;

      config_Data = new config_data();
    }

    public void RefreshConfig() {
      try {
        var _iter = configuration.GetChildren();

        foreach (var child in _iter) {
          switch (child.Key) {
            case "skillset_config":
              _process_configdata_skillsets(child);
              break;

            case "eventskill_updatesumexp":
              _process_configdata_eventskill(child);
              break;

            case "tick_interval":
              config_Data.tickinterval = child.Get<float>();
              break;
          }
        }
      } catch (ErrorSettingUpConfig e) {
        plugin.PrintToError(string.Format("Error occured when setting up default config. Error: {0}", e.ToString()));
        plugin.PrintToError("The plugin will not start.");
      }
    }

    public int GetMaxLevel(Player player, EPlayerSpeciality spec, byte idx) { return player.skills.skills[(byte)spec][idx].max; }

    public int GetBaseLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) { return config_Data.skillupdate_configs[(int)skillset]._base_level[(int)spec][idx]; }

    public float GetMultLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) { return config_Data.skillupdate_configs[(int)skillset]._mult_level[(int)spec][idx]; }

    public float GetMultMultLevelExp(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) { return config_Data.skillupdate_configs[(int)skillset]._multmult_level[(int)spec][idx]; }

    public float GetOnDiedValue(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) { return config_Data.skillupdate_configs[(int)skillset]._ondied_edit_level_value[(int)spec][idx]; }

    public EOnDiedEditType GetOnDiedType(EPlayerSkillset skillset, EPlayerSpeciality spec, byte idx) { return config_Data.skillupdate_configs[(int)skillset]._ondied_edit_level_type[(int)spec][idx]; }

    public float GetEventUpdate(ESkillEvent skillevent) { return config_Data.skillevent_exp[(int)skillevent]; }

    public float GetTickInterval() { return config_Data.tickinterval; }
  }

  public class specialtyExpData {
    public const int _speciality_count = 3;
    public const int _skill_offense_count = (int)EPlayerOffense.PARKOUR + 1;
    public const int _skill_defense_count = (int)EPlayerDefense.SURVIVAL + 1;
    public const int _skill_support_count = (int)EPlayerSupport.ENGINEER + 1;

    // data for checking if the player has leveled up or not
    public int[][] skillsets_expborderhigh = InitArrayT<int>();
    public int[][] skillsets_expborderlow = InitArrayT<int>();

    public int[][] skillsets_exp = InitArrayT<int>();
    public EPlayerSkillset skillset = EPlayerSkillset.NONE;

    public static T[][] InitArrayT<T>() {
      return new T[_speciality_count][] {// EPlayerspeciality.OFFENSE
                                         new T[_skill_offense_count],

                                         // EPlayerspeciality.DEFENSE
                                         new T[_skill_defense_count],

                                         // EPlayerspeciality.SUPPORT
                                         new T[_skill_support_count]
      };
    }

    public static void CopyArrayT<T>(ref T[][] dst, in T[][] src) {
      for (int i = 0; i < _speciality_count; i++) {
        int len = 0;
        switch ((EPlayerSpeciality)i) {
          case EPlayerSpeciality.OFFENSE:
            len = _skill_offense_count;
            break;

          case EPlayerSpeciality.DEFENSE:
            len = _skill_defense_count;
            break;

          case EPlayerSpeciality.SUPPORT:
            len = _skill_support_count;
            break;
        }

        for (int o = 0; o < len; o++) {
          dst[i][o] = src[i][o];
        }
      }
    }
  }

  public class SkillUpdater {
    private static string _savedata_path = "/Player/nekos.specovh.dat";

    // for savedata, keys needed to be a length of 4 chars
    private static string _savedata_speckey = "sp.e";
    private static string _savedata_skillsetkey = "ss.v";

    private static int _barlength = 11;
    private static string _bar_onMax = "MAX";

    private readonly SpecialtyOverhaul plugin;
    private Dictionary<ulong, specialtyExpData> playerExp;
    private Dictionary<ulong, bool> player_isDead;

    // calculation:
    // float res = base * powf(mult * i, multmult) + base;
    // float res = Math.Pow(E, Math.Log(((dataf - basef)/basef)/multmultf, Math.E))/multf;

    private int _calculateLevelBorderExp(ref specialtyExpData data, EPlayerSpeciality spec, byte skill_idx, byte level) {
      SkillConfig skillConfig = plugin.SkillConfigInstance;
      float basef = (float)skillConfig.GetBaseLevelExp(data.skillset, spec, skill_idx);
      float multf = skillConfig.GetMultLevelExp(data.skillset, spec, skill_idx);
      float multmultf = skillConfig.GetMultMultLevelExp(data.skillset, spec, skill_idx);

      return (int)Math.Round(basef * Math.Pow(multf * Math.Max(level - 1, 0), multmultf) + basef);
    }

    private byte _calculateLevel(Player player, ref specialtyExpData data, EPlayerSpeciality spec, byte skill_idx) {
      plugin.PrintToOutput(string.Format("skillset {0}, spec {1}, skill {2}", data.skillset, (int)spec, skill_idx));
      SkillConfig skillConfig = plugin.SkillConfigInstance;
      int maxlevel = skillConfig.GetMaxLevel(player, spec, skill_idx);
      float dataf = (float)data.skillsets_exp[(int)spec][skill_idx];
      float basef = (float)skillConfig.GetBaseLevelExp(data.skillset, spec, skill_idx);
      plugin.PrintToOutput(string.Format("Exp = {0} {1}", dataf, basef));
      float multf = skillConfig.GetMultLevelExp(data.skillset, spec, skill_idx);
      float multmultf = skillConfig.GetMultMultLevelExp(data.skillset, spec, skill_idx);

      if (dataf < basef)
        return 0;

      dataf += 1;
      return (byte)Math.Min(Math.Floor(Math.Pow(Math.E, Math.Log(dataf / basef / multmultf, Math.E)) / multf), maxlevel);
    }

    private void _recalculateExpLevel(Player player, ref specialtyExpData spc, PlayerSkills playerSkills, byte speciality, byte index) {
      byte _newlevel = _calculateLevel(player, ref spc, (EPlayerSpeciality)speciality, index);
      plugin.PrintToOutput(string.Format("_newlevel = {0} {1}", _newlevel, plugin.SkillConfigInstance.GetMaxLevel(player, (EPlayerSpeciality)speciality, index)));
      playerSkills.ServerSetSkillLevel(speciality, index, _newlevel);

      if (_newlevel < plugin.SkillConfigInstance.GetMaxLevel(player, (EPlayerSpeciality)speciality, index))
        spc.skillsets_expborderhigh[speciality][index] = _calculateLevelBorderExp(ref spc, (EPlayerSpeciality)speciality, index, (byte)(_newlevel + 1));
      else
        spc.skillsets_expborderhigh[speciality][index] = Int32.MaxValue;

      if (_newlevel > 0)
        spc.skillsets_expborderlow[speciality][index] = _calculateLevelBorderExp(ref spc, (EPlayerSpeciality)speciality, index, _newlevel);
      else
        spc.skillsets_expborderlow[speciality][index] = 0;
    }

    private string _getSpecialtyDataKey(EPlayerSpeciality spec, int skill_idx) { return _savedata_speckey + (char)spec + (char)skill_idx; }

    private string _getSkillsetDataKey() { return _savedata_skillsetkey; }

    private async void _OnPlayerConnected(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      player_isDead[eventData.player.SteamId.m_SteamID] = false;
      await LoadExp(eventData.player);
    }

    private async void _OnPlayerDisconnected(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      await Save(eventData.player);
      playerExp.Remove(eventData.player.SteamId.m_SteamID);
    }

    private async void _OnPlayerDied(object? obj, SpecialtyOverhaul.PlayerData eventData) { player_isDead[eventData.player.SteamId.m_SteamID] = true; }

    private async void _OnPlayerRevived(object? obj, SpecialtyOverhaul.PlayerData eventData) { player_isDead[eventData.player.SteamId.m_SteamID] = false; }

    private async void _OnPlayerRespawned(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      // to check if the event is caused by player connected or actually respawned
      if (player_isDead[eventData.player.SteamId.m_SteamID]) {
        await Task.Run(() => {
          specialtyExpData expData = playerExp[eventData.player.SteamId.m_SteamID];
          for (int i_specs = 0; i_specs < specialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch ((EPlayerSpeciality)i_specs) {
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

            for (int i_skill = 0; i_skill < skilllen; i_skill++) {
              int _currentexp = expData.skillsets_exp[i_specs][i_skill];
              float value = plugin.SkillConfigInstance.GetOnDiedValue(expData.skillset, (EPlayerSpeciality)i_specs, (byte)i_skill);
              switch (plugin.SkillConfigInstance.GetOnDiedType(expData.skillset, (EPlayerSpeciality)i_specs, (byte)i_skill)) {
                case SkillConfig.EOnDiedEditType.OFFSET:
                  _currentexp -= (int)Math.Round(value);
                  break;

                case SkillConfig.EOnDiedEditType.MULT:
                  _currentexp *= (int)Math.Round(value * _currentexp);
                  break;

                case SkillConfig.EOnDiedEditType.BASE: {
                  byte _level = _calculateLevel(eventData.player.Player, ref expData, (EPlayerSpeciality)i_specs, (byte)i_skill);
                  int _delta = _calculateLevelBorderExp(ref expData, (EPlayerSpeciality)i_specs, (byte)i_skill, (byte)(_level + 1)) - _calculateLevelBorderExp(ref expData, (EPlayerSpeciality)i_specs, (byte)i_skill, _level);

                  _currentexp -= _delta;
                } break;
              }

              if (_currentexp < 0)
                _currentexp = 0;

              expData.skillsets_exp[i_specs][i_skill] = _currentexp;
            }
          }

          player_isDead[eventData.player.SteamId.m_SteamID] = false;
        });
      }
    }

    public SkillUpdater(SpecialtyOverhaul plugin) {
      this.plugin = plugin;
      playerExp = new Dictionary<ulong, specialtyExpData>();
      player_isDead = new Dictionary<ulong, bool>();

      plugin.OnPlayerConnected += _OnPlayerConnected;
      plugin.OnPlayerDisconnected += _OnPlayerDisconnected;
    }

    ~SkillUpdater() {
      plugin.OnPlayerConnected -= _OnPlayerConnected;
      plugin.OnPlayerDisconnected -= _OnPlayerDisconnected;
    }

    public void SumSkillExp(CSteamID playerID, float sumexp, byte speciality, byte index) {
      try {
        UnturnedUserProvider? userSearch = plugin.UnturnedUserProviderInstance;
        if (userSearch == null)
          throw new KeyNotFoundException();

        UnturnedUser? user = userSearch.GetUser(playerID);
        if (user == null)
          throw new KeyNotFoundException();

        SumSkillExp(user.Player, sumexp, speciality, index);
      } catch (KeyNotFoundException) {
        plugin.PrintToError(string.Format("Player with user ID {0} cannot be found. Skill will not be updated.", playerID));
      }
    }

    public void SumSkillExp(UnturnedPlayer player, float sumexp, byte speciality, byte index) {
      int sumexp_int = (int)Math.Round(sumexp);
      if (sumexp_int <= 0)
        return;

      try {
        specialtyExpData spc = playerExp[player.SteamId.m_SteamID];
        if (spc.skillsets_expborderhigh[speciality][index] == int.MaxValue)
          return;

        int newexp = spc.skillsets_exp[speciality][index] + sumexp_int;
        if (newexp < 0)
          newexp = 0;

        plugin.PrintToOutput(string.Format("spec: {0}, skill: {1}", ((EPlayerSpeciality)speciality).ToString(), (int)index));
        plugin.PrintToOutput(string.Format("Level {0}, Exp {1}, MaxExp {2}", player.Player.skills.skills[speciality][index].level, newexp, spc.skillsets_expborderhigh[speciality][index]));

        if (speciality == (byte)EPlayerSpeciality.SUPPORT && index == (byte)EPlayerSupport.OUTDOORS)
          SumSkillExp(player, sumexp, speciality, (byte)EPlayerSupport.FISHING);

        spc.skillsets_exp[speciality][index] = newexp;
        if (newexp >= spc.skillsets_expborderhigh[speciality][index] || newexp < spc.skillsets_expborderlow[speciality][index]) {
          _recalculateExpLevel(player.Player, ref spc, player.Player.skills, speciality, index);
        }
      } catch (KeyNotFoundException) {
        plugin.PrintToError(string.Format("Player with user ID {0} didn't have the specialty data initialized/loaded. Skill will not be udpated.", player.SteamId.m_SteamID.ToString()));
      }
    }

    public void GivePlayerLevel(UnturnedPlayer player, byte spec, byte skill, int level) {
      specialtyExpData data = playerExp[player.SteamId.m_SteamID];

      if (level == 0)
        return;

      int newlevel;
      {
        int maxlevel = plugin.SkillConfigInstance.GetMaxLevel(player.Player, (EPlayerSpeciality)spec, skill);
        int currentlevel = _calculateLevel(player.Player, ref data, (EPlayerSpeciality)spec, skill);

        newlevel = currentlevel + level;
        if (newlevel > maxlevel)
          newlevel = maxlevel;
        else if (newlevel < 0)
          newlevel = 0;
      }

      data.skillsets_exp[spec][skill] = _calculateLevelBorderExp(ref data, (EPlayerSpeciality)spec, skill, (byte)newlevel);
      _recalculateExpLevel(player.Player, ref data, player.Player.skills, spec, skill);
    }

    public void SetPlayerLevel(UnturnedPlayer player, byte spec, byte skill, int level) {
      specialtyExpData data = playerExp[player.SteamId.m_SteamID];

      {
        int currentlevel = _calculateLevel(player.Player, ref data, (EPlayerSpeciality)spec, skill);
        if (currentlevel == level)
          return;

        int maxlevel = plugin.SkillConfigInstance.GetMaxLevel(player.Player, (EPlayerSpeciality)spec, skill);
        if (level < 0)
          level = 0;
        else if (level > maxlevel)
          level = maxlevel;
      }

      data.skillsets_exp[spec][skill] = _calculateLevelBorderExp(ref data, (EPlayerSpeciality)spec, skill, (byte)level);
      _recalculateExpLevel(player.Player, ref data, player.Player.skills, spec, skill);
    }

    // this function is used when a player joins the server
    public async UniTask<bool> LoadExp(UnturnedPlayer player) {
      plugin.PrintToOutput("Loading exp");
      ulong playerID = player.SteamId.m_SteamID;
      try {
        specialtyExpData expData = new specialtyExpData();

        // if there's a data about the specialty
        plugin.PrintToOutput(string.Format("player id: {0}", player.SteamId.m_SteamID));
        if (PlayerSavedata.fileExists(player.SteamPlayer.playerID, _savedata_path)) {
          await UniTask.SwitchToMainThread();
          Data userData = PlayerSavedata.readData(player.SteamPlayer.playerID, _savedata_path);
          await UniTask.SwitchToThreadPool();

          // getting skillset
          expData.skillset = (EPlayerSkillset)userData.readByte(_getSkillsetDataKey());

          // getting exp
          for (int i_specs = 0; i_specs < specialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch ((EPlayerSpeciality)i_specs) {
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

            for (int i_skill = 0; i_skill < skilllen; i_skill++) {
              string key = _getSpecialtyDataKey((EPlayerSpeciality)i_specs, i_skill);
              expData.skillsets_exp[i_specs][i_skill] = userData.readInt32(key);
            }
          }
        } else {
          // init skillset
          expData.skillset = EPlayerSkillset.NONE;

          // init specialty
          for (int i_specs = 0; i_specs < specialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch ((EPlayerSpeciality)i_specs) {
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

            for (int i_skill = 0; i_skill < skilllen; i_skill++) {
              expData.skillsets_exp[i_specs][i_skill] = 0;
            }
          }
        }

        playerExp[playerID] = expData;
      } catch (Exception e) {
        plugin.PrintToError(string.Format("Something went wrong when loading user file. PlayerID: {0}, Error: {1}", playerID, e.ToString()));
        return false;
      }

      await RecalculateSpecialty(player);
      return true;
    }

    public async UniTask Save(UnturnedPlayer player) {
      ulong playerID = player.SteamId.m_SteamID;

      try {
        Data data = new Data();
        specialtyExpData expData = playerExp[playerID];

        data.writeByte(_getSkillsetDataKey(), (byte)expData.skillset);

        for (int i_specs = 0; i_specs < specialtyExpData._speciality_count; i_specs++) {
          int skilllen = 0;
          switch ((EPlayerSpeciality)i_specs) {
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

          for (int i_skill = 0; i_skill < skilllen; i_skill++) {
            data.writeInt32(_getSpecialtyDataKey((EPlayerSpeciality)i_specs, i_skill), expData.skillsets_exp[i_specs][i_skill]);
          }
        }

        await UniTask.SwitchToMainThread();
        PlayerSavedata.writeData(player.SteamPlayer.playerID, _savedata_path, data);
        await UniTask.SwitchToThreadPool();
      } catch (Exception e) {
        plugin.PrintToError(string.Format("Something went wrong when saving user file. PlayerID: {0}, Error: {1}", playerID, e.ToString()));
      }
    }

    // this function used when needed to save all data to a storage
    public async UniTask SaveAll() {
      foreach (var data in playerExp) {
        UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(new CSteamID(data.Key));
        if (user != null) {
          await Save(user.Player);
        }
      }
    }

    public async UniTask RecalculateSpecialty(UnturnedPlayer player) {
      plugin.PrintToOutput("recalculating speciality");
      try {
        PlayerSkills userSkill = player.Player.skills;
        specialtyExpData data = playerExp[player.SteamId.m_SteamID];

        for (byte i_spec = 0; i_spec < specialtyExpData._speciality_count; i_spec++) {
          int skill_len = 0;
          switch ((EPlayerSpeciality)i_spec) {
            case EPlayerSpeciality.OFFENSE:
              skill_len = specialtyExpData._skill_offense_count;
              break;

            case EPlayerSpeciality.DEFENSE:
              skill_len = specialtyExpData._skill_defense_count;
              break;

            case EPlayerSpeciality.SUPPORT:
              skill_len = specialtyExpData._skill_support_count;
              break;
          }

          for (byte i_skill = 0; i_skill < skill_len; i_skill++) {
            _recalculateExpLevel(player.Player, ref data, userSkill, i_spec, i_skill);
          }
        }
      } catch (KeyNotFoundException) {
        plugin.PrintToError(string.Format("User cannot be found (ID: {0}). Will loading the data.", player.SteamId.m_SteamID));
        bool res = (await LoadExp(player));
        if (res) {
          await RecalculateSpecialty(player);
        }
      } catch (Exception e) {
        plugin.PrintToError(string.Format("User ID {0} is invalid. Cannot search for user data.", player.SteamId.m_SteamID));
        plugin.PrintToError(e.ToString());
      }
    }

    public KeyValuePair<string, string> GetExp_AsProgressBar(UnturnedPlayer player, EPlayerSpeciality speciality, int skill_idx, bool getskillname = false, bool getspecname = false) {
      try {
        specialtyExpData expData = playerExp[player.SteamId.m_SteamID];
        int _lowborder = expData.skillsets_expborderlow[(int)speciality][skill_idx];
        int _highborder = expData.skillsets_expborderhigh[(int)speciality][skill_idx];

        int _currentexp = 0;
        float _range = 0.0f;
        int _rangebar = 0;

        int barlen = _barlength;
        if (player.Player.skills.skills[(int)speciality][skill_idx].level >= plugin.SkillConfigInstance.GetBaseLevelExp(expData.skillset, speciality, (byte)skill_idx)) {
          barlen -= _bar_onMax.Length;
          _rangebar = barlen;
          _range = 1.0f;
        } else {
          _currentexp = expData.skillsets_exp[(int)speciality][skill_idx];
          _range = (float)(_currentexp - _lowborder) / (_highborder - _lowborder);
          _rangebar = (int)Math.Floor(_range * _barlength);
        }

        string _strbar = "";
        for (int i = 0; i < barlen; i++) {
          if (i < _rangebar)
            _strbar += '=';
          else
            _strbar += ' ';
        }

        if (barlen < _barlength) {
          _strbar = _strbar.Insert(_strbar.Length / 2, _bar_onMax);
        }

        string _strname = "";
        if (getskillname || getspecname) {
          var specpair = SkillConfig.specskill_indexer_inverse[speciality];
          if (getskillname)
            _strname = specpair.Value[(byte)skill_idx];

          if (getspecname)
            _strname = specpair.Key + "." + _strname;
        }

        return new KeyValuePair<string, string>(_strname, string.Format("Lvl {0} [{1}] {2}%", player.Player.skills.skills[(int)speciality][skill_idx].level, _strbar, (_range * 100).ToString("F1")));
      } catch (Exception e) {
        plugin.PrintToError(string.Format("Something wrong when getting exp data. Error: {0}", e.ToString()));
      }

      return new KeyValuePair<string, string>();
    }
  }

  public class TickTimer {
    private Task? _tickTask;
    private bool _keepTick = false;
    private float _tickInterval;

    public event EventHandler? OnTick;

    private async void _tickHandler() {
      while (_keepTick) {
        Task _timerTask = Task.Delay((int)(_tickInterval * 1000));
        OnTick?.Invoke(this, EventArgs.Empty);
        await _timerTask;
      }
    }

    public TickTimer(float tickIntervalS) { _tickInterval = tickIntervalS; }

    ~TickTimer() { StopTick(); }

    public void ChangeTickInterval(float tickIntervalS) { _tickInterval = tickIntervalS; }

    public void StartTick() {
      _keepTick = true;
      _tickTask = Task.Run(_tickHandler);
    }

    public void StopTick() {
      _keepTick = false;
      if (_tickTask != null) {
        _tickTask.Wait();
        _tickTask.Dispose();
        _tickTask = null;
      }
    }
  }
}