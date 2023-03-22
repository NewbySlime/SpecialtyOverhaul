using System;
using System.Threading.Tasks;
using OpenMod.Unturned.Players;
using Cysharp.Threading.Tasks;
using SDG.Unturned;
using System.Diagnostics.Tracing;
using OpenMod.Core.Eventing;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Zombies;
using OpenMod.Unturned.Zombies.Events;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using System.Numerics;
using OpenMod.Unturned.Players.Crafting.Events;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Inventory.Events;
using OpenMod.Unturned.Players.Useables.Events;
using OpenMod.Unturned.Vehicles.Events;
using OpenMod.Unturned.Building.Events;
using OpenMod.Unturned.Players.Skills.Events;
using OpenMod.Unturned.Animals.Events;
using OpenMod.Unturned.Resources.Events;
using Steamworks;
using System.Collections.Generic;
using System.Diagnostics;
using OpenMod.Unturned.Players.Stats.Events;
using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.Unturned.Users.Events;
using OpenMod.Unturned.Players.Movement.Events;
using UnityEngine.Assertions.Must;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class NonAutoloadWatcher {
    public class FarmWatcher {
      private void _onHarvesting(InteractableFarm harvestable, SteamPlayer player, ref bool shouldAllow) {
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        if (plugin != null) {
          plugin.PrintToOutput("player harvesting");
          plugin.SkillUpdaterInstance.SumSkillExp(player.playerID.steamID, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.AGRICULTURE_ONFARM), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.AGRICULTURE);
        }
      }

      public FarmWatcher() { InteractableFarm.OnHarvestRequested_Global += _onHarvesting; }

      ~FarmWatcher() { InteractableFarm.OnHarvestRequested_Global -= _onHarvesting; }
    }

    private FarmWatcher farmWatcher = new FarmWatcher();
    private InventoryWatcher.ReloadWatcher reloadWatcher = new InventoryWatcher.ReloadWatcher();
  }

  public class DamageWatcher : IEventListener<UnturnedZombieDamagingEvent>, IEventListener<UnturnedPlayerDamagingEvent>, IEventListener<UnturnedVehicleDamagingTireEvent>, IEventListener<UnturnedAnimalDamagingEvent>, IEventListener<UnturnedResourceDamagingEvent> {
    private static float _calculateDistExp(float base_exp, float dist, float min_dist, float div) {
      if (dist >= min_dist)
        return (dist - min_dist) / div * base_exp;

      return base_exp;
    }

    public async Task HandleEventAsync(Object? obj, UnturnedZombieDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        SkillUpdater updater = plugin.SkillUpdaterInstance;
        SkillConfig config = plugin.SkillConfigInstance;

        UnturnedPlayer? player = @event.Instigator;
        UnturnedZombie? zombie = @event.Zombie;
        if (player != null && zombie != null)
          await Task.Run(() => {
            bool _usemelee = false;
            if (player.Player.equipment.isEquipped) {
              switch (player.Player.equipment.asset.type) {
                case EItemType.GUN: {
                  float dist = Vector3.Distance(player.Transform.Position, zombie.Transform.Position);
                  SkillConfig.ESkillEvent eSkillEvent;
                  if (zombie.IsAlive)
                    eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER_CRIT : SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER;
                  else
                    eSkillEvent = SkillConfig.ESkillEvent.SHARPSHOOTER_ZOMBIE_KILLED_GUN;

                  // sharpshooter
                  updater.SumSkillExp(player, _calculateDistExp(config.GetEventUpdate(eSkillEvent), dist, config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START), config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)), (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER);
                } break;

                case EItemType.MELEE: {
                  _usemelee = true;
                } break;
              }
            } else
              _usemelee = true;

            if (_usemelee) {
              SkillConfig.ESkillEvent eSkillEvent;
              if (zombie.IsAlive)
                eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.OVERKILL_MELEE_ZOMBIE_CRIT : SkillConfig.ESkillEvent.OVERKILL_MELEE_ZOMBIE;
              else
                eSkillEvent = SkillConfig.ESkillEvent.OVERKILL_ZOMBIE_KILLED_MELEE;

              float _expval = config.GetEventUpdate(eSkillEvent);
              if (eSkillEvent != SkillConfig.ESkillEvent.OVERKILL_ZOMBIE_KILLED_MELEE && (int)config.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // overkill
              updater.SumSkillExp(player, _expval, (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.OVERKILL);
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        SkillUpdater updater = plugin.SkillUpdaterInstance;
        SkillConfig config = plugin.SkillConfigInstance;
        UnturnedPlayer? player = plugin.UnturnedUserProviderInstance.GetUser(@event.Killer)?.Player;

        if (player != null)
          await Task.Run(() => {
            switch (@event.Cause) {
              case EDeathCause.GUN: {
                float dist = Vector3.Distance(player.Transform.Position, @event.Player.Transform.Position);
                SkillConfig.ESkillEvent eSkillEvent;
                if (player.PlayerLife.isDead)
                  eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER_CRIT : SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_PLAYER;
                else
                  eSkillEvent = SkillConfig.ESkillEvent.SHARPSHOOTER_PLAYER_KILLED_GUN;

                // sharpshooter
                updater.SumSkillExp(player, _calculateDistExp(config.GetEventUpdate(eSkillEvent), dist, config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_START), config.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_DIST_DIV)), (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER);
              } break;

              case EDeathCause.PUNCH:
              case EDeathCause.MELEE: {
                SkillConfig.ESkillEvent eSkillEvent;
                if (player.PlayerLife.isDead)
                  eSkillEvent = @event.Limb == ELimb.SKULL ? SkillConfig.ESkillEvent.OVERKILL_MELEE_PLAYER_CRIT : SkillConfig.ESkillEvent.OVERKILL_MELEE_PLAYER;
                else
                  eSkillEvent = SkillConfig.ESkillEvent.OVERKILL_PLAYER_KILLED_MELEE;

                float _expval = config.GetEventUpdate(eSkillEvent);
                if (eSkillEvent != SkillConfig.ESkillEvent.OVERKILL_PLAYER_KILLED_MELEE && (int)config.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                  _expval *= @event.DamageAmount;

                // overkill
                updater.SumSkillExp(player, _expval, (int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.OVERKILL);
              } break;
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedAnimalDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;
        UnturnedPlayer? player = plugin.UnturnedUserProviderInstance.GetUser(@event.Instigator)?.Player;

        if (player != null)
          await Task.Run(() => {
            bool _usemelee = false;
            if (player.Player.equipment.isEquipped) {
              switch (player.Player.equipment.asset.type) {
                case EItemType.GUN: {
                  if (@event.Animal.IsAlive) {
                    // outdoors
                    skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_ANIMAL_KILLED), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.OUTDOORS);
                  }

                  // sharpshooter
                  SkillConfig.ESkillEvent eSkillEvent = @event.Animal.IsAlive ? SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_ANIMAL : SkillConfig.ESkillEvent.SHARPSHOOTER_ANIMAL_KILLED_GUN;
                  skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(eSkillEvent), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.SHARPSHOOTER);
                } break;

                case EItemType.MELEE: {
                  _usemelee = true;
                } break;
              }
            } else
              _usemelee = true;

            if (_usemelee) {
              SkillConfig.ESkillEvent eSkillEvent = @event.Animal.IsAlive ? SkillConfig.ESkillEvent.OVERKILL_MELEE_ANIMAL : SkillConfig.ESkillEvent.OVERKILL_ANIMAL_KILLED_MELEE;
              float _expval = skillConfig.GetEventUpdate(eSkillEvent);
              if (eSkillEvent != SkillConfig.ESkillEvent.OVERKILL_ANIMAL_KILLED_MELEE && skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OVERKILL_MELEE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // overkill
              skillUpdater.SumSkillExp(player, _expval, (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.OVERKILL);
            }
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedVehicleDamagingTireEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

        UnturnedPlayer? player = null;
        CSteamID? steamID = @event.Instigator;
        if (steamID != null)
          player = plugin.UnturnedUserProviderInstance.GetUser(steamID.Value)?.Player;

        if (player != null && player.Player.equipment.asset.type == EItemType.GUN)
          await Task.Run(() => {
            // sharpshooter
            skillUpdater.SumSkillExp(player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.SHARPSHOOTER_SHOOT_TIRE), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.SHARPSHOOTER);
          });
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedResourceDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        SkillConfig skillConfig = plugin.SkillConfigInstance;
        SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

        if (@event.Instigator != null)
          await Task.Run(() => {
            if (!@event.Instigator.Player.equipment.isEquipped || @event.Instigator.Player.equipment.asset.type == EItemType.MELEE) {
              float _expval = skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_RESOURCE_DAMAGING);
              if ((int)skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.OUTDOORS_RESOURCE_DAMAGE_BASED) > 0)
                _expval *= @event.DamageAmount;

              // outdoors
              skillUpdater.SumSkillExp(@event.Instigator, _expval, (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.OUTDOORS);
            }
          });
      }
    }
  }

  public class CraftWatcher : IEventListener<UnturnedPlayerCraftingEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerCraftingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null)
        await Task.Run(() => {
          SkillConfig skillConfig = plugin.SkillConfigInstance;
          SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

          ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemId) as ItemAsset;
          if (asset != null) {
            switch (asset.type) {
              case EItemType.MAGAZINE:
              case EItemType.SUPPLY:
                break;

              case EItemType.FOOD:
                // cooking
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.COOKING_ON_COOK), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.COOKING);

                break;

              case EItemType.GUN:
              case EItemType.SIGHT:
              case EItemType.BARREL:
              case EItemType.TACTICAL:
              case EItemType.THROWABLE:
              case EItemType.TOOL:
              case EItemType.OPTIC:
              case EItemType.TRAP:
              case EItemType.GENERATOR:
              case EItemType.FISHER:
              case EItemType.BEACON:
              case EItemType.TANK:
              case EItemType.CHARGE:
              case EItemType.SENTRY:
              case EItemType.DETONATOR:
              case EItemType.FILTER:
              case EItemType.VEHICLE_REPAIR_TOOL:
              case EItemType.OIL_PUMP:
              case EItemType.COMPASS:
                // engineer
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.ENGINEER_CRAFTING), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.ENGINEER);

                goto default;

              case EItemType.FARM:
              case EItemType.GROWER:
                // agriculture
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.AGRICULTURE_CRAFTING), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.AGRICULTURE);

                goto default;

              case EItemType.MEDICAL:
                // healing
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_CRAFTING), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.HEALING);

                goto default;

              default:
                // crafting
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.CRAFTING_ON_CRAFT), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.CRAFTING);

                // dexterity
                skillUpdater.SumSkillExp(@event.Player, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_CRAFTING), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DEXTERITY);

                break;
            }
          }
        });
    }
  }

  public class InventoryWatcher : IEventListener<UnturnedPlayerTakingItemEvent>, IEventListener<UnturnedPlayerDroppedItemEvent>, IEventListener<UnturnedPlayerItemAddedEvent>, IEventListener<UnturnedPlayerItemRemovedEvent>, IEventListener<UnturnedPlayerConnectedEvent> {
    public class ReloadWatcher {
      // note: there's a reason why shouldn't this be normal sync func
      //  because it needs to be processed in one thread, even if this
      //  is a normal sync function, there's a chance that the events
      //  are processed in different threads
      private void _onReloading(UseableGun gun) { _async_onReloading(gun).Wait(); }

      private async Task _async_onReloading(UseableGun gun) {
        await UniTask.SwitchToMainThread();
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        plugin?.PrintToOutput("reloading gun");
        plugin?.PrintToOutput(gun.player.GetNetId().id.ToString());
        if (plugin != null) {
          // before: InventoryWatcher (UnturnedPlayerItemAddedEvent)
          // third sequence when reloading
          if (_mag_swap.TryGetValue(gun.player.GetNetId(), out KeyValuePair<UnturnedPlayer, byte> pair)) {
            plugin.SkillUpdaterInstance.SumSkillExp(pair.Key, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_PER_AMMO) * pair.Value), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DEXTERITY);

            _mag_swap.Remove(gun.player.GetNetId());
          }
        }

        await UniTask.SwitchToThreadPool();
      }

      public ReloadWatcher() { UseableGun.OnReloading_Global += _onReloading; }

      ~ReloadWatcher() { UseableGun.OnReloading_Global -= _onReloading; }
    }

    private enum ELastState { TAKING_ITEM = 0x1, DROPPING_ITEM = 0x2, ITEM_ADDED = 0x4, ITEM_REMOVED = 0x8, ANY = 0xff }

    private static readonly TimeSpan min_inventorychangems = new TimeSpan(0, 0, 0, 0, 50);
    private static Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>> _player_inventorystates = new Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>>();

    private static Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>> _mag_swap = new Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>>();

    private static void _changeData(ulong key, ELastState state) { _player_inventorystates[key] = new KeyValuePair<ELastState, Stopwatch>(state, Stopwatch.StartNew()); }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerTakingItemEvent @event) {
      await UniTask.SwitchToMainThread();
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.TAKING_ITEM);
      await UniTask.SwitchToThreadPool();
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDroppedItemEvent @event) {
      await UniTask.SwitchToMainThread();
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.DROPPING_ITEM);
      await UniTask.SwitchToThreadPool();
    }

    //
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerItemAddedEvent @event) {
      await UniTask.SwitchToMainThread();
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemJar.item.id) as ItemAsset;

      if (plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item added");
        plugin.PrintToOutput(@event.Player.Player.GetNetId().id.ToString());
        plugin.PrintToOutput(string.Format("ammo count {0}", @event.ItemJar.item.amount));

        if (pair.Value.Elapsed > min_inventorychangems) {
        } else {
          if (asset.type == EItemType.MAGAZINE) {
            if ((pair.Key & ELastState.ITEM_REMOVED) > 0) {
              // before: InventoryWatcher (UnturnedPlayerItemRemovedEvent)
              // second sequence when reloading
              // after: InvetoryWatcher.ReloadWatcher._onReloading
              if (@event.ItemJar.item.amount > 0 && plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_ALLOW_NOTEMPTY_MAGS) == 0) {
                _mag_swap.Remove(@event.Player.Player.GetNetId());
              }
            }
          }
        }
      }

      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ITEM_ADDED);
      await UniTask.SwitchToThreadPool();
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerItemRemovedEvent @event) {
      await UniTask.SwitchToMainThread();
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemJar.item.id) as ItemAsset;

      if (plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item removed");

        if (pair.Value.Elapsed > min_inventorychangems) {
        }

        // first sequence to do when reloading
        // after: InventoryWatcher (UnturnedPlayerItemAddedEvent)
        if (asset.type == EItemType.MAGAZINE) {
          _mag_swap[@event.Player.Player.GetNetId()] = new KeyValuePair<UnturnedPlayer, byte>(@event.Player, @event.ItemJar.item.amount);
        }
      }

      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ITEM_REMOVED);
      await UniTask.SwitchToThreadPool();
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerConnectedEvent @event) {
      await UniTask.SwitchToMainThread();
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ANY);
      await UniTask.SwitchToThreadPool();
    }

    public static void AddPlayer(UnturnedPlayer player) { _changeData(player.SteamId.m_SteamID, ELastState.ANY); }
  }

  // This listener only for resetting experience
  public class ExperienceWatcher : IEventListener<UnturnedPlayerExperienceUpdatedEvent> {
    public ExperienceWatcher() {}

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerExperienceUpdatedEvent @event) { @event.Player.Player.skills.ServerSetExperience(0); }
  }

  public class ConnectionWatcher : IEventListener<UnturnedPlayerConnectedEvent>, IEventListener<UnturnedPlayerDisconnectedEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerConnectedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.CallEvent_OnPlayerConnected(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDisconnectedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.CallEvent_OnPlayerDisconnected(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }
  }

  // TODO test this
  public class LifeWatcher : IEventListener<UnturnedPlayerSpawnedEvent>, IEventListener<UnturnedPlayerRevivedEvent>, IEventListener<UnturnedPlayerDeathEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerSpawnedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.CallEvent_OnPlayerRespawned(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerRevivedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.CallEvent_OnPlayerRevived(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDeathEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.CallEvent_OnPlayerDied(new SpecialtyOverhaul.PlayerData(@event.Player));
      }
    }
  }

  public class PlayerStatusWatcher : IEventListener<UnturnedPlayerOxygenUpdatedEvent>,
                                     IEventListener<UnturnedPlayerStaminaUpdatedEvent>,
                                     IEventListener<UnturnedPlayerFoodUpdatedEvent>,
                                     IEventListener<UnturnedPlayerWaterUpdatedEvent>,
                                     IEventListener<UnturnedPlayerVirusUpdatedEvent>,
                                     IEventListener<UnturnedPlayerHealthUpdatedEvent>,
                                     IEventListener<UnturnedPlayerTemperatureUpdatedEvent>,
                                     IEventListener<UnturnedPlayerBleedingUpdatedEvent>,
                                     IEventListener<UnturnedPlayerBrokenUpdatedEvent>,
                                     IEventListener<UnturnedPlayerFallDamagingEvent>,

                                     IEventListener<UnturnedPlayerStanceUpdatedEvent>,

                                     IEventListener<UnturnedPlayerDeathEvent>,

                                     IEventListener<UnturnedUserDisconnectedEvent>,
                                     IEventListener<UnturnedUserRecheckEvent>,
                                     IEventListener<UnturnedPlayerSpawnedEvent> {
    private class Byteref {
      public byte data;
    }

    private class ValueMaintainer {
      public UnturnedPlayer player;
      public float value;
      public SkillConfig.ESkillEvent eSkillEvent_persec;
      public EPlayerSpeciality spec;
      public byte idx;

      public ValueMaintainer(UnturnedPlayer player, float value, SkillConfig.ESkillEvent eSkillEvent_persec, EPlayerSpeciality spec, byte idx) {
        this.player = player;
        this.value = value;
        this.eSkillEvent_persec = eSkillEvent_persec;
        this.spec = spec;
        this.idx = idx;
      }
    }

    private readonly static byte _foodflag = 1;
    private readonly static byte _thirstflag = 2;

    private static Dictionary<ulong, Byteref> _survivalValueMaintain = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _vitalityValueMaintain = new Dictionary<ulong, Byteref>();

    private static Dictionary<ulong, Byteref> _lastHealth = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastStamina = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastOxygen = new Dictionary<ulong, Byteref>();
    private static Dictionary<ulong, Byteref> _lastVirus = new Dictionary<ulong, Byteref>();

    private static Dictionary<ulong, bool> _isAided = new Dictionary<ulong, bool>();

    private static void _addPlayerToWatch(UnturnedPlayer player) {
      _lastHealth[player.SteamId.m_SteamID] = new Byteref { data = player.PlayerLife.health };
      _lastStamina[player.SteamId.m_SteamID] = new Byteref { data = player.PlayerLife.stamina };
      _lastOxygen[player.SteamId.m_SteamID] = new Byteref { data = player.PlayerLife.oxygen };
      _lastVirus[player.SteamId.m_SteamID] = new Byteref { data = player.PlayerLife.virus };
      _vitalityValueMaintain[player.SteamId.m_SteamID] = new Byteref { data = 0 };
      _survivalValueMaintain[player.SteamId.m_SteamID] = new Byteref { data = 0 };
    }

    private static void _stopTickWatch(UnturnedPlayer player) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
        plugin.RemoveCallbackOnTick(player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
      }
    }

    private static void _onTick_valueMaintainer(Object obj, ref bool removeObj) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        ValueMaintainer? maintainer = obj as ValueMaintainer;
        if (maintainer != null) {
          maintainer.value += plugin.SkillConfigInstance.GetTickInterval() * plugin.SkillConfigInstance.GetEventUpdate(maintainer.eSkillEvent_persec);
          plugin.PrintToOutput(string.Format("val {0}", maintainer.value));
          int nval = (int)Math.Floor(maintainer.value);
          if (nval > 0) {
            maintainer.value -= nval;
            plugin.SkillUpdaterInstance.SumSkillExp(maintainer.player, nval, (byte)maintainer.spec, maintainer.idx);
          }
        } else
          removeObj = true;
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerOxygenUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        if (_lastOxygen.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref))
          await Task.Run(() => {
            int _delta = (int)@event.Oxygen - dataref.data;
            plugin.PrintToOutput(string.Format("delta {0}", _delta));
            if (_delta < 0) {
              // diving
              SkillConfig.ESkillEvent eSkillEvent = @event.Player.Player.stance.isBodyUnderwater ? SkillConfig.ESkillEvent.DIVING_OXYGEM_USE_IFSWIMMING : SkillConfig.ESkillEvent.DIVING_OXYGEN_USE;
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(eSkillEvent) * _delta * -1), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DIVING);
            } else if (_delta > 0) {
              // cardio
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.CARDIO_OXYGEN_REGEN) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.CARDIO);
            }

            dataref.data = @event.Oxygen;
          });
        else
          _lastOxygen[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerStaminaUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        if (_lastStamina.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref))
          await Task.Run(() => {
            int _delta = (int)@event.Stamina - dataref.data;

            if (_delta < 0) {
              _delta *= -1;

              // exercise
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.EXERCISE_STAMINA_USE) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.EXERCISE);

              if (@event.Player.Player.stance.stance == EPlayerStance.SPRINT) {
                plugin.PrintToOutput("sprinting");
                // parkour (sprint)
                plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.PARKOUR_STAMINA_USE_SPRINTING) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.PARKOUR);
              }
            } else if (_delta > 0) {
              // cardio
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.CARDIO_STAMINA_REGEN) * _delta), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.CARDIO);
            }

            dataref.data = @event.Stamina;
          });
        else
          _lastStamina[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerFoodUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        Byteref dataref;
        // survival
        if (_survivalValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if (@event.Food < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SURVIVAL_MAINTAIN_HUNGER_BELOW))
            dataref.data |= _foodflag;
          else
            dataref.data &= (byte)~_foodflag;

          if (plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL)) {
            if (dataref.data == 0)
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
          } else if (dataref.data > 0) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.SURVIVAL_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);

            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL, _onTick_valueMaintainer, maintainer);
          }
        } else
          _survivalValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };

        // vitality
        if (_vitalityValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if (plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY)) {
            if (@event.Food <= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_HUNGER_ABOVE)) {
              dataref.data &= (byte)~_foodflag;
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
            }
          } else {
            if (@event.Food > plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_HUNGER_ABOVE)) {
              dataref.data |= _foodflag;
              if (dataref.data == (_foodflag | _thirstflag)) {
                ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.VITALITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);

                plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY, _onTick_valueMaintainer, maintainer);
              }
            } else
              dataref.data &= (byte)~_foodflag;
          }
        } else
          _vitalityValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerWaterUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        Byteref dataref;
        // survival
        if (_survivalValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if (@event.Water < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SURVIVAL_MAINTAIN_THIRST_BELOW))
            dataref.data |= _thirstflag;
          else
            dataref.data &= (byte)~_thirstflag;

          if (plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL)) {
            if (dataref.data == 0)
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);
          } else if (dataref.data > 0) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.SURVIVAL_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL);

            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SURVIVAL, _onTick_valueMaintainer, maintainer);
          }
        } else
          _survivalValueMaintain[@event.Player.SteamId.m_SteamID] = new Byteref { data = 0 };

        // vitality
        if (_vitalityValueMaintain.TryGetValue(@event.Player.SteamId.m_SteamID, out dataref)) {
          if (plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY)) {
            if (@event.Water <= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_THIRST_ABOVE)) {
              dataref.data &= (byte)~_thirstflag;
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);
            }
          } else {
            if (@event.Water > plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.VITALITY_MAINTAIN_THIRST_ABOVE)) {
              dataref.data |= _thirstflag;
              if (dataref.data == (_foodflag | _thirstflag)) {
                ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.VITALITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY);

                plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.VITALITY, _onTick_valueMaintainer, maintainer);
              } else
                dataref.data &= (byte)~_thirstflag;
            }
          }
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerVirusUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        if (_lastVirus.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref)) {
          int _delta = (int)@event.Virus - dataref.data;
          if (_delta < 0) {
            _delta *= -1;
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_VIRUS_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
          } else if (_delta > 0) {
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_VIRUS_INCREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
          }
        } else
          _lastVirus[@event.Player.SteamId.m_SteamID] = new Byteref { data = @event.Player.Player.life.virus };

        // immunity
        if (!plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY)) {
          if (@event.Virus < plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_MAINTAIN_VIRUS_BELOW)) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, SkillConfig.ESkillEvent.IMMUNITY_INCREASE_PERSEC_MULT, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY, _onTick_valueMaintainer, maintainer);
          }
        } else if (@event.Virus >= plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.IMMUNITY_MAINTAIN_VIRUS_BELOW))
          plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.IMMUNITY);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerHealthUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        if (_lastHealth.TryGetValue(@event.Player.SteamId.m_SteamID, out Byteref dataref)) {
          int _delta = (int)@event.Health - dataref.data;
          if (_delta < 0) {
            _delta *= -1;

            // toughness
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_HEALTH_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

            // strength
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_HEALTH_DECREASE_MULT) * _delta), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
          } else if (_delta > 0) {
            // healing
            if (_isAided.ContainsKey(@event.Player.SteamId.m_SteamID))
              _isAided.Remove(@event.Player.SteamId.m_SteamID);
            else {
              plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_HEALTH_MULT) * _delta), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.HEALING);
            }
          }

          dataref.data = @event.Health;
        } else
          _lastHealth[@event.Player.SteamId.m_SteamID] = new Byteref { data = 100 };
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerTemperatureUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        if (plugin.OnTickContainsKey(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED)) {
          switch (@event.Temperature) {
            case EPlayerTemperature.WARM:
            case EPlayerTemperature.BURNING:
            case EPlayerTemperature.NONE:
            case EPlayerTemperature.COVERED:
            case EPlayerTemperature.ACID:
              plugin.RemoveCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
              break;
          }
        } else {
          SkillConfig.ESkillEvent eSkillEvent = SkillConfig.ESkillEvent.__len;
          switch (@event.Temperature) {
            case EPlayerTemperature.COLD:
              eSkillEvent = SkillConfig.ESkillEvent.WARMBLOODED_ON_COLD_PERSEC_MULT;
              break;

            case EPlayerTemperature.FREEZING:
              eSkillEvent = SkillConfig.ESkillEvent.WARMBLOODED_ON_FREEZING_PERSEC_MULT;
              break;
          }

          // warmblooded
          if (eSkillEvent != SkillConfig.ESkillEvent.__len) {
            ValueMaintainer maintainer = new ValueMaintainer(@event.Player, 0, eSkillEvent, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED);
            plugin.AddCallbackOnTick(@event.Player.SteamId.m_SteamID, EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.WARMBLOODED, _onTick_valueMaintainer, maintainer);
          }
        }
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerBleedingUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null && @event.IsBleeding) {
        // toughness
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_BLEEDING), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_BLEEDING), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerBrokenUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null && @event.IsBroken) {
        // toughness
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.TOUGHNESS_FRACTURED), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.TOUGHNESS);

        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_FRACTURED), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerFallDamagingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        // strength
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.STRENGTH_HEALTH_DECREASE_FALL_DAMAGE_MULT) * @event.DamageAmount), (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.STRENGTH);
      }
    }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerStanceUpdatedEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.PrintToOutput(string.Format("is jumping {0}", @event.Player.Player.movement.jump));
        plugin.PrintToOutput(string.Format("player stance {0}", @event.Player.Player.stance));
      }
    }

    // this is to reset all the variables in this class
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerDeathEvent @event) { _stopTickWatch(@event.Player); }

    public async Task HandleEventAsync(Object? obj, UnturnedUserDisconnectedEvent @event) { _stopTickWatch(@event.User.Player); }

    public async Task HandleEventAsync(Object? obj, UnturnedPlayerSpawnedEvent @event) { _addPlayerToWatch(@event.Player); }

    public async Task HandleEventAsync(Object? obj, UnturnedUserRecheckEvent @event) { _addPlayerToWatch(@event.user.Player); }

    public static void AddIsAided(ulong player) { _isAided[player] = true; }
  }

  public class AidingWatcher : IEventListener<UnturnedPlayerPerformingAidEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerPerformingAidEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        // to prevent leveling up by get aided by someone
        PlayerStatusWatcher.AddIsAided(@event.Target.SteamId.m_SteamID);

        // healing
        plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_ON_AIDING), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.HEALING);
      }
    }
  }

  public class AlertWatcher : IEventListener<UnturnedZombieAlertingPlayerEvent>, IEventListener<UnturnedAnimalAttackingPlayerEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedZombieAlertingPlayerEvent @event) {
      if (@event.Player == null)
        return;

      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null)
        await Task.Run(() => {
          float _dist = System.Numerics.Vector3.Distance(@event.Player.Transform.Position, @event.Zombie.Transform.Position);
          float _maxdist = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_MAX_DIST);
          if (_dist < _maxdist) {
            float _div = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ZOMBIE_DIST_DIV);
            // sneakybeaky
            plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (_maxdist - _dist) / _div, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
          }
        });
    }

    public async Task HandleEventAsync(object? obj, UnturnedAnimalAttackingPlayerEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        float _dist = System.Numerics.Vector3.Distance(@event.Player.Transform.Position, @event.Animal.Transform.Position);
        float _maxdist = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_MAX_DIST);
        if (_dist < _maxdist) {
          float _div = plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.SNEAKYBEAKY_ANIMAL_DIST_DIV);
          // sneakybeaky
          plugin.SkillUpdaterInstance.SumSkillExp(@event.Player, (_maxdist - _dist) / _div, (byte)EPlayerSpeciality.DEFENSE, (byte)EPlayerDefense.SNEAKYBEAKY);
        }
      }
    }
  }

  public class RepairingWatcher : IEventListener<UnturnedVehicleRepairingEvent> {
    public async Task HandleEventAsync(object? obj, UnturnedVehicleRepairingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if (plugin != null) {
        plugin.PrintToOutput(string.Format("healing {0}", @event.PendingTotalHealing));
        UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(@event.Instigator);

        if (user != null) {
          // mechanic
          plugin.SkillUpdaterInstance.SumSkillExp(user.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.MECHANIC_REPAIR_HEALTH) * @event.PendingTotalHealing), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.MECHANIC);

          // engineer
          plugin.SkillUpdaterInstance.SumSkillExp(user.Player, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.ENGINEER_REPAIR_HEALTH) * @event.PendingTotalHealing), (byte)EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.ENGINEER);
        }
      }
    }
  }
}
