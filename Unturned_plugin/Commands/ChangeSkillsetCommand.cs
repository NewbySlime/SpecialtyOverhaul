using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Commands {
  [Command("ChangeSkillset")]
  [CommandAlias("skillset")]
  [CommandDescription("To change player's skillset")]
  [CommandSyntax("<name of skillset>")]
  [CommandActor(typeof(UnturnedUser))]
  public class ChangeSkillsetCommand: PromptableCommand {
    private struct ChangeData {
      public UnturnedUser user;
      public EPlayerSkillset skillset;
    }


    private readonly SpecialtyOverhaul plugin;



    public ChangeSkillsetCommand(SpecialtyOverhaul plugin, IServiceProvider provider): base("ChangeSkillset", provider) {
      this.plugin = plugin;  
    }


    protected override async Task OnConfirm(object obj) {
      ChangeData? data = obj as ChangeData?;
      if(data.HasValue) {
        await plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(data.Value.user, async (ISkillModifier editor) => {
          editor.SetPlayerSkillset(data.Value.skillset, true);

          await Context.Actor.PrintMessageAsync(string.Format("Your new skillset: {0}.", SkillConfig.skillset_indexer_inverse[(byte)data.Value.skillset]), System.Drawing.Color.Green);
        });
      }
    }

    protected override async UniTask OnExecuteAsync() {
      if(!await checkPrompt()) {
        UnturnedUser? user = Context.Actor as UnturnedUser;
        if(user != null) {
          try {
            await plugin.SkillUpdaterInstance.GetModifier_WrapperFunction(user, async (ISkillModifier editor) => {
              EPlayerSkillset _skillset = editor.GetPlayerSkillset();
              if(user.Player.SteamPlayer.isAdmin || plugin.SkillConfigInstance.GetAllowChangeSkillsetAfterChange() || _skillset == EPlayerSkillset.NONE) {
                string param = (await Context.Parameters.GetAsync<string>(0)).ToLower();

                CommandParameterParser.ParamResult_ToUserAndSkillset skillset_res;
                CommandParameterParser.ToUserAndSkillset(plugin.UnturnedUserProviderInstance.GetOnlineUsers(), out skillset_res, param);

                if(_skillset != skillset_res.skillset) {
                  bool _isChangeable = editor.IsSkillsetFulfilled(skillset_res.skillset);
                  if(_isChangeable || user.Player.SteamPlayer.isAdmin) {
                    ChangeData data = new ChangeData {
                      user = user,
                      skillset = skillset_res.skillset
                    };

                    if(!plugin.SkillConfigInstance.GetAllowChangeSkillsetAfterChange() && skillset_res.user == null && !user.Player.SteamPlayer.isAdmin)
                      await user.PrintMessageAsync(string.Format("Note that you can only change when you are a {0}.", SkillConfig.skillset_indexer_inverse[(byte)EPlayerSkillset.NONE]), System.Drawing.Color.Orange);

                    string _promptstr = "";
                    if(!_isChangeable)
                      _promptstr = "Some requirement aren't fullfilled, proceed if needed.";

                    await askPrompt(_promptstr, data);
                  }
                  else
                    await user.PrintMessageAsync(string.Format("You are not eligible to become {0}", SkillConfig.skillset_indexer_inverse[(byte)skillset_res.skillset]), System.Drawing.Color.OrangeRed);
                }
                else
                  await user.PrintMessageAsync(string.Format("Already {0}.", SkillConfig.skillset_indexer_inverse[(byte)_skillset]), System.Drawing.Color.Yellow);
              }
              else
                await user.PrintMessageAsync("You cannot change skillset.");
            });
          }
          catch(CommandParameterParser.ParsingException) {
            await Context.Actor.PrintMessageAsync("Wrong skillset name.", System.Drawing.Color.Red);
          }
        }
      }
    }
  }
}
