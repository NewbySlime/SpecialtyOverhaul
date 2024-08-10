using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Inventory.Events;
using OpenMod.Unturned.Players;
using SDG.Unturned;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using SmartFormat.Utilities;
using System;
using OpenMod.Core.Plugins.Events;
using UnityEngine.Rendering;
using System.Threading;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Core.Eventing;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class InventoryWatcher: 
    IEventListener<UnturnedPlayerTakingItemEvent>, 
    IEventListener<UnturnedPlayerDroppedItemEvent>, 
    IEventListener<UnturnedPlayerItemAddedEvent>, 
    IEventListener<UnturnedPlayerItemRemovedEvent>, 
    IEventListener<UnturnedPlayerItemUpdatedEvent>,

    IEventListener<UnturnedPlayerSpawnedEvent>,

    IEventListener<PluginOnReloadEvent>,

    IEventListener<UnturnedPlayerConnectedEvent>,
    IEventListener<UnturnedPlayerDisconnectedEvent>,

    IEventListener<PluginUserRecheckEvent>,
    IEventListener<PluginUnloadedEvent>
    {
    // note: there's a reason why shouldn't this be normal sync func
    //  because it needs to be processed in one thread, even if this
    //  is a normal sync function, there's a chance that the events
    //  are processed in different threads


    private class AmmoLookoutData {
      public Stopwatch _lastReload = Stopwatch.StartNew();
      public uint _currentAmmo = 0;
    }

    private enum ELastState {
      TAKING_ITEM = 0x1, DROPPING_ITEM = 0x2, ITEM_ADDED = 0x4, ITEM_REMOVED = 0x8, ANY = 0xff
    }

    private static readonly System.TimeSpan min_inventorychangems = new System.TimeSpan(0, 0, 0, 0, 50);
    private static readonly System.TimeSpan _minute_timespan = new System.TimeSpan(0, 1, 0);
    private static readonly System.TimeSpan _max_counts_as_fishing = new System.TimeSpan(0, 0, 0, 0, 30);


    private static Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>> _player_inventorystates = new Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>>();

    private static Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>> _mag_swap = new Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>>();

    private static Dictionary<ulong, Stopwatch> _lastFish = new Dictionary<ulong, Stopwatch>();

    private static Dictionary<NetId, AmmoLookoutData> _currentAmmoReload = new Dictionary<NetId, AmmoLookoutData>();

    private static Mutex _accessor_Mutex = new Mutex();


    public static event EventHandler<UnturnedPlayer>? OnPlayerFished;

    private static void _changeData(ulong key, ELastState state) {
      _player_inventorystates[key] = new KeyValuePair<ELastState, Stopwatch>(state, Stopwatch.StartNew());
    }

    private static void _resetDict(UnturnedPlayer player) {
      _accessor_Mutex.WaitOne();
      _mag_swap[player.Player.GetNetId()] = new KeyValuePair<UnturnedPlayer, byte>(player, 0);
      _changeData(player.SteamId.m_SteamID, ELastState.ANY);
      _currentAmmoReload.Remove(player.Player.GetNetId());
      _accessor_Mutex.ReleaseMutex();
    }

    private static void _deleteDict(UnturnedPlayer player) {
      _accessor_Mutex.WaitOne();
      _currentAmmoReload.Remove(player.Player.GetNetId());
      _mag_swap.Remove(player.Player.GetNetId());
      _player_inventorystates.Remove(player.SteamId.m_SteamID);
      _accessor_Mutex.ReleaseMutex();
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerTakingItemEvent @event) {
      _accessor_Mutex.WaitOne();
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.TAKING_ITEM);
      _accessor_Mutex.ReleaseMutex();
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerDroppedItemEvent @event) {
      _accessor_Mutex.WaitOne();
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.DROPPING_ITEM);
      _accessor_Mutex.ReleaseMutex();
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerItemAddedEvent @event) {
      _accessor_Mutex.WaitOne();
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemJar.item.id) as ItemAsset;

      if(plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item added");
        plugin.PrintToOutput(@event.Player.Player.GetNetId().id.ToString());
        plugin.PrintToOutput(string.Format("item id {0}", asset.type.ToString()));

        if(pair.Value.Elapsed > min_inventorychangems) {
          
        }
        else {

        }
      }

      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ITEM_ADDED);
      _accessor_Mutex.ReleaseMutex();
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerItemRemovedEvent @event) {
      _accessor_Mutex.WaitOne();
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemJar.item.id) as ItemAsset;

      if(plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item removed");

        if(pair.Value.Elapsed > min_inventorychangems) {
        }

        // first sequence to do when reloading
        // after: InventoryWatcher (PluginOnReloadEvent)
        if(asset.type == EItemType.MAGAZINE) {
          plugin.PrintToOutput(string.Format("ammo {0}, netid {1}", @event.ItemJar.item.amount, @event.Player.Player.GetNetId().id));
          _mag_swap[@event.Player.Player.GetNetId()] = new KeyValuePair<UnturnedPlayer, byte>(@event.Player, @event.ItemJar.item.amount);
        }
      }

      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ITEM_REMOVED);
      _accessor_Mutex.ReleaseMutex();
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerItemUpdatedEvent @event) {
      _accessor_Mutex.WaitOne();

      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null) {
        plugin.PrintToOutput("Item update");
        ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemJar.item.id) as ItemAsset;
        if(asset != null) {
          switch(asset.type) {
            case EItemType.FISHER: {
              plugin.PrintToOutput("player fishing");
            }
            break;
          }
        }
      }

      _accessor_Mutex.ReleaseMutex();
    }


    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerSpawnedEvent @event) {
      _mag_swap[@event.Player.Player.GetNetId()] = new KeyValuePair<UnturnedPlayer, byte>(@event.Player, 0);
      _changeData(@event.Player.SteamId.m_SteamID, ELastState.ANY);
    }


    // NOTE: System.NullReferenceException???
    public async Task HandleEventAsync(System.Object? obj, PluginOnReloadEvent @event) {
      _accessor_Mutex.WaitOne();
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null && @event.param.HasValue) {
        plugin.PrintToOutput("reloading gun");
        plugin.PrintToOutput(@event.param.Value.gun.player.GetNetId().id.ToString());

        /*
        // before: InventoryWatcher (UnturnedPlayerItemAddedEvent)
        // third sequence when reloading
        if(_mag_swap.TryGetValue(@event.param.Value.gun.player.GetNetId(), out KeyValuePair<UnturnedPlayer, byte> pair)) {
          plugin.SkillUpdaterInstance.SumSkillExp(pair.Key, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_PER_AMMO) * pair.Value), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DEXTERITY);

          _mag_swap.Remove(@event.param.Value.gun.player.GetNetId());
        }
         */

        // before: InventoryWatcher (UnturnedPlayerItemRemovedEvent)
        // second sequence when reloading
        NetId _playerNetId = @event.param.Value.gun.player.GetNetId();
        AmmoLookoutData _currentData;
        plugin.PrintToOutput(string.Format("contains {0}", _mag_swap.ContainsKey(_playerNetId)));

        if(_mag_swap.TryGetValue(_playerNetId, out KeyValuePair<UnturnedPlayer, byte> magdata)) {
          plugin.PrintToOutput(string.Format("reload ammo {0}", magdata.Value));
          int _maxammo = (int)Math.Round(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_MAXAMMO_PER_MINUTE));
          bool _calculateExp = false;
          long _beforeammo = 0;
          long _deltaammo = 0;

          if(_currentAmmoReload.TryGetValue(_playerNetId, out _currentData)) {
            if((int)Math.Round(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_ADAPTABLE_COOLDOWN)) >= 1) {
              int _ammosubstract = (int)Math.Round(_maxammo * Math.Min(_currentData._lastReload.Elapsed.TotalSeconds / _minute_timespan.TotalSeconds, 1.0));
              plugin.PrintToOutput(string.Format("ammosub {0}", _ammosubstract));
              _currentData._lastReload.Restart();
              _currentData._currentAmmo = (uint)Math.Max((int)_currentData._currentAmmo - _ammosubstract, 0);
            }

            if(_currentData._lastReload.Elapsed < _minute_timespan) {
              if(_maxammo <= 0 || _currentData._currentAmmo < _maxammo) {
                _calculateExp = true;

                uint _newammo = _currentData._currentAmmo + magdata.Value;
                _deltaammo = magdata.Value;
                if(_newammo >= _maxammo)
                  _deltaammo = _maxammo - _currentData._currentAmmo;

                _beforeammo = _currentData._currentAmmo;

                _mag_swap.Remove(_playerNetId);
                _currentData._currentAmmo = _newammo;
              }
            }
            else {
              _currentData._lastReload.Restart();
              _currentData._currentAmmo = magdata.Value;
              _calculateExp = true;
              _beforeammo = 0;
              _deltaammo = magdata.Value;
            }
          }
          else {
            _currentData = new AmmoLookoutData { _currentAmmo = magdata.Value };
            _currentAmmoReload[_playerNetId] = _currentData;
            _calculateExp = true; 
            _beforeammo = 0;
            _deltaammo = magdata.Value;
          }


          if(_calculateExp) {
            plugin.PrintToOutput(string.Format("ammo {0}, delta {1}", _currentData._currentAmmo, _deltaammo));

            float _mult = 1;
            if((int)Math.Round(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_MULT_CONSTANT)) < 1)
              _mult = (float)(_maxammo - _beforeammo) / _maxammo;

            plugin.PrintToOutput(string.Format("skill amount {0}", (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_PER_AMMO) * _deltaammo * _mult)));

            plugin.SkillUpdaterInstance.SumSkillExp(
              magdata.Key,
              (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_PER_AMMO) * _deltaammo * _mult),
              (byte)EPlayerSpeciality.OFFENSE,
              (byte)EPlayerOffense.DEXTERITY
            );
          }
        }
      }

      _accessor_Mutex.ReleaseMutex();
    }


    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerConnectedEvent @event) {
      _resetDict(@event.Player);
    }

    public async Task HandleEventAsync(System.Object? obj, UnturnedPlayerDisconnectedEvent @event) {
      _deleteDict(@event.Player);
    }


    public async Task HandleEventAsync(System.Object? obj, PluginUserRecheckEvent @event) {
      if(@event.param != null) 
        _resetDict(@event.param.Value.user.Player);
    }

    public async Task HandleEventAsync(System.Object? obj, PluginUnloadedEvent @event) {
      _mag_swap.Clear();
      _currentAmmoReload.Clear();
    }
  }
}
