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
            skillUpdater.GetModifier_WrapperFunction(
              plugin.UnturnedUserProviderInstance.GetUser(@event.Player.Player),
              (ISkillModifier editor) => {
                switch(asset.type) {
                  case EItemType.MAGAZINE:
                  case EItemType.SUPPLY:
                    break;

                  case EItemType.FOOD:
                    // cooking
                    editor.ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.COOKING, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.COOKING_ON_COOK));

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
                    editor.ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.ENGINEER, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.ENGINEER_CRAFTING));

                    goto default;

                  case EItemType.FARM:
                  case EItemType.GROWER:
                    // agriculture
                    editor.ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.AGRICULTURE, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.AGRICULTURE_CRAFTING));

                    goto default;

                  case EItemType.MEDICAL:
                    // healing
                    editor.ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.HEALING, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.HEALING_CRAFTING));

                    goto default;

                  default:
                    // crafting
                    editor.ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.CRAFTING, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.CRAFTING_ON_CRAFT));

                    // dexterity
                    editor.ExpFractionIncrement(EPlayerSpeciality.OFFENSE, (byte)EPlayerOffense.DEXTERITY, skillConfig.GetEventUpdate(SkillConfig.ESkillEvent.DEXTERITY_CRAFTING));

                    break;
                }
              }
            );
          }
        });
    }
  }
}
