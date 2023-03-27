using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Crafting.Events;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Watcher {
  public class CraftWatcher: IEventListener<UnturnedPlayerCraftingEvent> {
    public async Task HandleEventAsync(Object? obj, UnturnedPlayerCraftingEvent @event) {
      SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
      if(plugin != null)
        await Task.Run(() => {
          SkillConfig skillConfig = plugin.SkillConfigInstance;
          SkillUpdater skillUpdater = plugin.SkillUpdaterInstance;

          ItemAsset? asset = Assets.find(EAssetType.ITEM, @event.ItemId) as ItemAsset;
          if(asset != null) {
            switch(asset.type) {
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
}
