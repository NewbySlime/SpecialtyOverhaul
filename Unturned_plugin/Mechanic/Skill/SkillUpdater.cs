using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Nekos.SpecialtyPlugin.Binding;
using Nekos.SpecialtyPlugin.Persistance;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This class handles the leveling up of a player
  /// <br/><br/>
  /// A recap on how calculating the level <br/>
  /// Basically, <see cref="SpecialtyExpData"/> only holds total amount of experience the player has, not in levels <br/>
  /// If an exp has gone beyond the upper border (leveling up), the class call <see cref="_recalculateExpLevel(Player, ref SpecialtyExpData, PlayerSkills, byte, byte)"/> <br/>
  /// In that function, it recalculates for the new level and adjusting it if the level already maxed out or not
  /// </summary>
  public partial class SkillUpdater {
    // Used for parsing from skill data to readable skill data
    private readonly static int _barlength = 11;

    private readonly SpecialtyOverhaul plugin;
    private readonly Binder _binder = new();


    /// <param name="plugin">Current plugin object</param>
    public SkillUpdater(SpecialtyOverhaul plugin) {
      this.plugin = plugin;
    }



    /// <summary>
    /// Parsing exp data to a progress bar
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    /// <param name="speciality">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <param name="getskillname">Progress bar needs skill name or not</param>
    /// <param name="getspecname">Progress bar needs specialty name or not</param>
    /// <returns>A pair of strings that the first one contains the skill name, while the other one contains the progress bar</returns>
    public KeyValuePair<string, string> GetExp_AsProgressBar(UnturnedUser user, EPlayerSpeciality speciality, int skill_idx, bool getskillname = false, bool getspecname = false) {
      KeyValuePair<string, string> _res = new("", "");
      ISkillPersistance _persistance = SkillPersistancePool.GetSkillPersistance(user.Player.SteamPlayer.playerID, _binder, user);

      try {
        SpecialtyExpData expData = _persistance.ExpData;
        int _lowborder = expData.skillsets_expborderlow[(int)speciality][skill_idx];
        int _highborder = expData.skillsets_expborderhigh[(int)speciality][skill_idx];
         
        int barlen = _barlength;

        int _currentexp = expData.skillsets_exp[(int)speciality][skill_idx];
        float _range = (float)(_currentexp - _lowborder) / (Math.Abs(_highborder) - _lowborder);
        int _rangebar = (int)Math.Floor(_range * _barlength);


        string _lvlnum = "";
        if(_highborder < 0)
          _lvlnum = "X";
        else
          _lvlnum = user.Player.Player.skills.skills[(int)speciality][skill_idx].level.ToString();

        string _strbar = "";
        for(int i = 0; i < barlen; i++) {
          if(i < _rangebar)
            _strbar += '=';
          else
            _strbar += "  ";
        }

        string _strname = "";
        if(getskillname || getspecname) {
          var specpair = SkillConfig.specskill_indexer_inverse[speciality];
          if(getskillname)
            _strname = specpair.Value[(byte)skill_idx];

          if(getspecname)
            _strname = specpair.Key + "." + _strname;
        }

        _res = new KeyValuePair<string, string>(_strname, string.Format("Lvl {0} [{1}] {2}%", _lvlnum, _strbar, (_range * 100).ToString("F1")));
      }
      catch(Exception e) {
        plugin.PrintToError(string.Format("Something wrong when getting exp data. Error: {0}", e.ToString()));
      }

      SkillPersistancePool.UnbindSkillPersistance(user.Player.SteamPlayer.playerID, _binder);
      return _res;
    }


    public async Task GetModifier_WrapperFunction(UnturnedUser user, Func<ISkillModifier, Task> handler) {
      ISkillPersistance _persistance = SkillPersistancePool.GetSkillPersistance(user.Player.SteamPlayer.playerID, _binder);
      await handler.Invoke(new SkillModifier(_persistance, user));

      SkillPersistancePool.UnbindSkillPersistance(user.Player.SteamPlayer.playerID, _binder);
    }

    public void GetModifier_WrapperFunction(UnturnedUser user, Action<ISkillModifier> handler) {
      ISkillPersistance _persistance = SkillPersistancePool.GetSkillPersistance(user.Player.SteamPlayer.playerID, _binder);
      handler.Invoke(new SkillModifier(_persistance, user));

      SkillPersistancePool.UnbindSkillPersistance(user.Player.SteamPlayer.playerID, _binder);
    }
  }
}
