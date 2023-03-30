using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Inventory.Events;
using OpenMod.Unturned.Players;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenMod.Core.Plugins.Events;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class InventoryWatcher:
    IEventListener<UnturnedPlayerTakingItemEvent>,
    IEventListener<UnturnedPlayerDroppedItemEvent>,
    IEventListener<UnturnedPlayerItemAddedEvent>,
    IEventListener<UnturnedPlayerItemRemovedEvent>,

    IEventListener<UnturnedPlayerConnectedEvent>,

    IEventListener<PluginUnloadedEvent>
    {
    public class ReloadWatcher {
      // note: there's a reason why shouldn't this be normal sync func
      //  because it needs to be processed in one thread, even if this
      //  is a normal sync function, there's a chance that the events
      //  are processed in different threads
      private void _onReloading(UseableGun gun) {
        _async_onReloading(gun).Wait();
      }

      private async Task _async_onReloading(UseableGun gun) {
        await UniTask.SwitchToMainThread();
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        plugin?.PrintToOutput("reloading gun");
        plugin?.PrintToOutput(gun.player.GetNetId().id.ToString());
        if(plugin != null) {
          // before: InventoryWatcher (UnturnedPlayerItemAddedEvent)
          // third sequence when reloading
          if(_mag_swap.TryGetValue(gun.player.GetNetId(), out KeyValuePair<UnturnedPlayer, byte> pair)) {
            plugin.SkillUpdaterInstance.SumSkillExp(pair.Key, (float)(plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_PER_AMMO) * pair.Value), (byte)EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DEXTERITY);

            _mag_swap.Remove(gun.player.GetNetId());
          }
        }

        await UniTask.SwitchToThreadPool();
      }

      public ReloadWatcher() {
        UseableGun.OnReloading_Global += _onReloading;
      }

      ~ReloadWatcher() {
        UseableGun.OnReloading_Global -= _onReloading;
      }
    }




    private enum ELastState {
      TAKING_ITEM = 0x1, DROPPING_ITEM = 0x2, ITEM_ADDED = 0x4, ITEM_REMOVED = 0x8, ANY = 0xff
    }

    private static readonly TimeSpan min_inventorychangems = new TimeSpan(0, 0, 0, 0, 50);
    private static Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>> _player_inventorystates = new Dictionary<ulong, KeyValuePair<ELastState, Stopwatch>>();

    private static Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>> _mag_swap = new Dictionary<NetId, KeyValuePair<UnturnedPlayer, byte>>();

    private static void _changeData(ulong key, ELastState state) {
      _player_inventorystates[key] = new KeyValuePair<ELastState, Stopwatch>(state, Stopwatch.StartNew());
    }

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

      if(plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item added");
        plugin.PrintToOutput(@event.Player.Player.GetNetId().id.ToString());
        plugin.PrintToOutput(string.Format("ammo count {0}", @event.ItemJar.item.amount));

        if(pair.Value.Elapsed > min_inventorychangems) {
        }
        else {
          if(asset.type == EItemType.MAGAZINE) {
            if((pair.Key & ELastState.ITEM_REMOVED) > 0) {
              // before: InventoryWatcher (UnturnedPlayerItemRemovedEvent)
              // second sequence when reloading
              // after: InvetoryWatcher.ReloadWatcher._onReloading
              if(@event.ItemJar.item.amount > 0 && plugin.SkillConfigInstance.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_RELOAD_ALLOW_NOTEMPTY_MAGS) == 0) {
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

      if(plugin != null && asset != null && _player_inventorystates.TryGetValue(@event.Player.SteamId.m_SteamID, out KeyValuePair<ELastState, Stopwatch> pair)) {
        plugin.PrintToOutput("item removed");

        if(pair.Value.Elapsed > min_inventorychangems) {
        }

        // first sequence to do when reloading
        // after: InventoryWatcher (UnturnedPlayerItemAddedEvent)
        if(asset.type == EItemType.MAGAZINE) {
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

    public async Task HandleEventAsync(Object? obj, PluginUnloadedEvent @event) {
      
    }

    public static void AddPlayer(UnturnedPlayer player) {
      _changeData(player.SteamId.m_SteamID, ELastState.ANY);
    }
  }
}
