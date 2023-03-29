using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This class handles the leveling up of a player
  /// <br/><br/>
  /// A recap on how calculating the level <br/>
  /// Basically, <see cref="SpecialtyExpData"/> only holds total amount of experience the player has, not in levels <br/>
  /// If an exp has gone beyond the upper border (leveling up), the class call <see cref="_recalculateExpLevel(Player, ref SpecialtyExpData, PlayerSkills, byte, byte)"/> <br/>
  /// In that function, it recalculates for the new level and adjusting it if the level already maxed out or not
  /// </summary>
  public class SkillUpdater {
    /// <summary>
    /// Path of the save file
    /// </summary>
    private static string _savedata_path = "/Player/nekos.specovh.dat";

    // for savedata, keys needed to be a length of 4 chars
    // I don't know why I decided this, probably just me too scared of being inefficient
    private static string _savedata_speckey = "sp.e";
    private static string _savedata_skillsetkey = "ss.v";

    // Used for parsing from skill data to readable skill data
    private static int _barlength = 11;
    private static string _bar_onMax = "MAX";

    private readonly SpecialtyOverhaul plugin;
    private Dictionary<ulong, SpecialtyExpData> playerExp;
    private Dictionary<ulong, bool> player_isDead;


    /** Note for calculation:
     *  For when getting how much exp needed to level up
     *    float res = base * Math.Pow(mult * i, multmult);
     *  
     *  For getting current level based on how many exp the player has
     *    float res = Math.Pow(dataf/basef, 1.0/multmultf)/multf
     */

    /// <summary>
    /// For calculating border of the level. Or in another meaning, calculating how much exp needed to level up
    /// </summary>
    /// <param name="data">Current player exp data</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <param name="level">Current level</param>
    /// <returns>Total exp in integer</returns>
    private int _calculateLevelBorderExp(ref SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx, byte level) {
      SkillConfig skillConfig = plugin.SkillConfigInstance;
      float basef = (float)skillConfig.GetBaseLevelExp(data.skillset, spec, skill_idx);
      float multf = skillConfig.GetMultLevelExp(data.skillset, spec, skill_idx);
      float multmultf = skillConfig.GetMultMultLevelExp(data.skillset, spec, skill_idx);

      return (int)Math.Ceiling(basef * Math.Pow(multf * level, multmultf));
    }

    /// <summary>
    /// For calculating current level based on current total exp
    /// </summary>
    /// <param name="player">Current player object</param>
    /// <param name="data">Current player exp data</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <returns>Level in integer</returns>
    private byte _calculateLevel(Player player, ref SpecialtyExpData data, EPlayerSpeciality spec, byte skill_idx) {
      SkillConfig skillConfig = plugin.SkillConfigInstance;
      int maxlevel = skillConfig.GetMaxLevel(player, data.skillset, spec, skill_idx);
      float dataf = (float)data.skillsets_exp[(int)spec][skill_idx];
      float basef = (float)skillConfig.GetBaseLevelExp(data.skillset, spec, skill_idx);

      if(dataf < basef)
        return 0;

      float multf = skillConfig.GetMultLevelExp(data.skillset, spec, skill_idx);
      float multmultf = skillConfig.GetMultMultLevelExp(data.skillset, spec, skill_idx);

      // getting fraction of the result
      float _lvl = (float)Math.Pow(dataf / basef, 1.0 / multmultf) / multf;
      int nlvl = (int)Math.Floor(_lvl);
      _lvl -= nlvl;

      // check floating-point error
      if(_lvl > 0.99)
        nlvl++;

      return (byte)Math.Min(nlvl, maxlevel);
    }

    /// <summary>
    /// Recalculating if there are level changes, and applied it to player
    /// </summary>
    /// <param name="player">Current player object</param>
    /// <param name="spc">Current player exp data</param>
    /// <param name="playerSkills">Current player's PlayerSkills object</param>
    /// <param name="speciality">What specialty</param>
    /// <param name="index">What index</param>
    private void _recalculateExpLevel(Player player, ref SpecialtyExpData spc, PlayerSkills playerSkills, byte speciality, byte index) {
      byte _newlevel = _calculateLevel(player, ref spc, (EPlayerSpeciality)speciality, index);
      byte _minlevel = plugin.SkillConfigInstance.GetStartLevel(player, spc.skillset, (EPlayerSpeciality)speciality, index);

      if(_newlevel < _minlevel)
        _newlevel = _minlevel;

      playerSkills.ServerSetSkillLevel(speciality, index, _newlevel);

      if(_newlevel < plugin.SkillConfigInstance.GetMaxLevel(player, spc.skillset, (EPlayerSpeciality)speciality, index))
        spc.skillsets_expborderhigh[speciality][index] = _calculateLevelBorderExp(ref spc, (EPlayerSpeciality)speciality, index, (byte)(_newlevel + 1));
      else
        spc.skillsets_expborderhigh[speciality][index] = Int32.MaxValue;

      spc.skillsets_expborderlow[speciality][index] = _calculateLevelBorderExp(ref spc, (EPlayerSpeciality)speciality, index, _newlevel);


      if(spc.skillsets_exp[speciality][index] < spc.skillsets_expborderlow[speciality][index])
        spc.skillsets_exp[speciality][index] = spc.skillsets_expborderlow[speciality][index];
    }

    /// <summary>
    /// Getting a key for save files by certain specialty and skill
    /// </summary>
    /// <param name="spec">What specialty</param>
    /// <param name="skill_idx">What skill</param>
    /// <returns></returns>
    private string _getSpecialtyDataKey(EPlayerSpeciality spec, int skill_idx) {
      return _savedata_speckey + (char)spec + (char)skill_idx;
    }

    /// <summary>
    /// Getting a key of skillset for save files
    /// </summary>
    /// <returns></returns>
    private string _getSkillsetDataKey() {
      return _savedata_skillsetkey;
    }

    /// <summary>
    /// A callback when player is connected. It will load the player's exp data
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="eventData"></param>
    private async void _OnPlayerConnected(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      player_isDead[eventData.player.SteamId.m_SteamID] = false;
      await LoadExp(eventData.player);
    }

    /// <summary>
    /// A callback when player is disconnected. It will save the player's exp data
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="eventData"></param>
    private async void _OnPlayerDisconnected(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      plugin.PrintToOutput("on disconnect event");
      await Save(eventData.player);
      playerExp.Remove(eventData.player.SteamId.m_SteamID);
    }

    /// <summary>
    /// A callback when player is dead
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="eventData"></param>
    private async void _OnPlayerDied(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      player_isDead[eventData.player.SteamId.m_SteamID] = true;
    }

    /// <summary>
    /// A callback when player being revived
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="eventData"></param>
    private async void _OnPlayerRevived(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      player_isDead[eventData.player.SteamId.m_SteamID] = false;
    }

    /// <summary>
    /// A callback when player being respawned. It will recalculating player's exp and modify it
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="eventData"></param>
    private async void _OnPlayerRespawned(object? obj, SpecialtyOverhaul.PlayerData eventData) {
      // to check if the event is caused by player connected or actually respawned
      if(player_isDead[eventData.player.SteamId.m_SteamID]) {
        await Task.Run(() => {
          SpecialtyExpData expData = playerExp[eventData.player.SteamId.m_SteamID];
          for(int i_specs = 0; i_specs < SpecialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch((EPlayerSpeciality)i_specs) {
              case EPlayerSpeciality.OFFENSE:
                skilllen = SpecialtyExpData._skill_offense_count;
                break;

              case EPlayerSpeciality.DEFENSE:
                skilllen = SpecialtyExpData._skill_defense_count;
                break;

              case EPlayerSpeciality.SUPPORT:
                skilllen = SpecialtyExpData._skill_support_count;
                break;
            }

            for(int i_skill = 0; i_skill < skilllen; i_skill++) {
              int _currentexp = expData.skillsets_exp[i_specs][i_skill];
              float value = plugin.SkillConfigInstance.GetOnDiedValue(expData.skillset, (EPlayerSpeciality)i_specs, (byte)i_skill);
              switch(plugin.SkillConfigInstance.GetOnDiedType(expData.skillset, (EPlayerSpeciality)i_specs, (byte)i_skill)) {
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
                }
                break;
              }

              if(_currentexp < 0)
                _currentexp = 0;

              expData.skillsets_exp[i_specs][i_skill] = _currentexp;
            }
          }

          player_isDead[eventData.player.SteamId.m_SteamID] = false;
        });
      }
    }

    /// <param name="plugin">Current plugin object</param>
    public SkillUpdater(SpecialtyOverhaul plugin) {
      this.plugin = plugin;
      playerExp = new Dictionary<ulong, SpecialtyExpData>();
      player_isDead = new Dictionary<ulong, bool>();

      plugin.OnPlayerConnected += _OnPlayerConnected;
      plugin.OnPlayerDisconnected += _OnPlayerDisconnected;
    }

    ~SkillUpdater() {
      plugin.OnPlayerConnected -= _OnPlayerConnected;
      plugin.OnPlayerDisconnected -= _OnPlayerDisconnected;
    }

    /// <summary>
    /// Adding experience to a skill
    /// </summary>
    /// <param name="playerID">Player's steam ID</param>
    /// <param name="sumexp">Amount of experience</param>
    /// <param name="speciality">What specialty</param>
    /// <param name="index">What skill</param>
    public void SumSkillExp(CSteamID playerID, float sumexp, byte speciality, byte index) {
      if(!plugin.SkillConfigInstance.ConfigLoadProperly)
        return;

      try {
        UnturnedUserProvider? userSearch = plugin.UnturnedUserProviderInstance;
        if(userSearch == null)
          throw new KeyNotFoundException();

        UnturnedUser? user = userSearch.GetUser(playerID);
        if(user == null)
          throw new KeyNotFoundException();

        SumSkillExp(user.Player, sumexp, speciality, index);
      }
      catch(KeyNotFoundException) {
        plugin.PrintToError(string.Format("Player with user ID {0} cannot be found. Skill will not be updated.", playerID));
      }
    }

    /// <summary>
    /// Adding experience to a skill
    /// </summary>
    /// <param name="player">Current player object</param>
    /// <param name="sumexp">Amount of experience</param>
    /// <param name="speciality">What specialty</param>
    /// <param name="index">What skill</param>
    public void SumSkillExp(UnturnedPlayer player, float sumexp, byte speciality, byte index) {
      if(!plugin.SkillConfigInstance.ConfigLoadProperly)
        return;

      try {
        SpecialtyExpData spc = playerExp[player.SteamId.m_SteamID];
        if(spc.skillsets_expborderhigh[speciality][index] == int.MaxValue)
          return;

        float _fexp = spc.skillsets_exp_fraction[speciality][index] + sumexp;
        if(_fexp > 1) {
          int sumexp_int = (int)Math.Floor(_fexp);
          _fexp -= sumexp_int;
        
          int newexp = spc.skillsets_exp[speciality][index] + sumexp_int;
          if(newexp < 0)
            newexp = 0;

          plugin.PrintToOutput(string.Format("spec: {0}, skill: {1}", ((EPlayerSpeciality)speciality).ToString(), (int)index));
          plugin.PrintToOutput(string.Format("Level {0}, Exp {1}, MaxExp {2}", player.Player.skills.skills[speciality][index].level, newexp, spc.skillsets_expborderhigh[speciality][index]));

          if(speciality == (byte)EPlayerSpeciality.SUPPORT && index == (byte)EPlayerSupport.OUTDOORS)
            SumSkillExp(player, sumexp, speciality, (byte)EPlayerSupport.FISHING);

          spc.skillsets_exp[speciality][index] = newexp;
          if(newexp >= spc.skillsets_expborderhigh[speciality][index] || newexp < spc.skillsets_expborderlow[speciality][index]) {
            _recalculateExpLevel(player.Player, ref spc, player.Player.skills, speciality, index);
          }
        }

        spc.skillsets_exp_fraction[speciality][index] = _fexp;
      }
      catch(KeyNotFoundException) {
        plugin.PrintToError(string.Format("Player with user ID {0} didn't have the specialty data initialized/loaded. Skill will not be udpated.", player.SteamId.m_SteamID.ToString()));
      }
    }

    /// <summary>
    /// Giving player n level for certain skill
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill">What skill</param>
    /// <param name="level">Amount of level</param>
    public void GivePlayerLevel(UnturnedPlayer player, byte spec, byte skill, int level) {
      SpecialtyExpData data = playerExp[player.SteamId.m_SteamID];

      if(level == 0)
        return;

      int newlevel;
      {
        int maxlevel = plugin.SkillConfigInstance.GetMaxLevel(player.Player, data.skillset, (EPlayerSpeciality)spec, skill);
        int currentlevel = _calculateLevel(player.Player, ref data, (EPlayerSpeciality)spec, skill);

        newlevel = currentlevel + level;
        if(newlevel > maxlevel)
          newlevel = maxlevel;
        else if(newlevel < 0)
          newlevel = 0;
      }

      data.skillsets_exp[spec][skill] = _calculateLevelBorderExp(ref data, (EPlayerSpeciality)spec, skill, (byte)newlevel);
      _recalculateExpLevel(player.Player, ref data, player.Player.skills, spec, skill);
    }

    /// <summary>
    /// Setting player's skill levels
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    /// <param name="spec">What specialty</param>
    /// <param name="skill">What skill</param>
    /// <param name="level">Amount of level</param>
    public void SetPlayerLevel(UnturnedPlayer player, byte spec, byte skill, int level) {
      SpecialtyExpData data = playerExp[player.SteamId.m_SteamID];

      {
        int currentlevel = _calculateLevel(player.Player, ref data, (EPlayerSpeciality)spec, skill);
        if(currentlevel == level)
          return;

        int maxlevel = plugin.SkillConfigInstance.GetMaxLevel(player.Player, data.skillset, (EPlayerSpeciality)spec, skill);
        if(level < 0)
          level = 0;
        else if(level > maxlevel)
          level = maxlevel;
      }

      plugin.PrintToOutput(string.Format("currentlevel {0}", level));

      data.skillsets_exp[spec][skill] = _calculateLevelBorderExp(ref data, (EPlayerSpeciality)spec, skill, (byte)level);
      _recalculateExpLevel(player.Player, ref data, player.Player.skills, spec, skill);
      plugin.PrintToOutput(string.Format("new exp {0}", data.skillsets_exp[spec][skill]));
    }

    /// <summary>
    /// This function loads player's exp data. Can also be used for reloading, if the file is edited. This uses <see cref="PlayerSavedata"/> to save and load player data
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    /// <returns>If load successfully</returns>
    public async UniTask<bool> LoadExp(UnturnedPlayer player) {
      plugin.PrintToOutput("Loading exp");
      ulong playerID = player.SteamId.m_SteamID;
      try {
        SpecialtyExpData expData = new SpecialtyExpData();

        // if there's a data about the specialty
        plugin.PrintToOutput(string.Format("player id: {0}", player.SteamId.m_SteamID));
        if(PlayerSavedata.fileExists(player.SteamPlayer.playerID, _savedata_path)) {
          await UniTask.SwitchToMainThread();
          Data userData = PlayerSavedata.readData(player.SteamPlayer.playerID, _savedata_path);
          await UniTask.SwitchToThreadPool();

          // getting skillset
          expData.skillset = (EPlayerSkillset)userData.readByte(_getSkillsetDataKey());

          // getting exp
          for(int i_specs = 0; i_specs < SpecialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch((EPlayerSpeciality)i_specs) {
              case EPlayerSpeciality.OFFENSE:
                skilllen = SpecialtyExpData._skill_offense_count;
                break;

              case EPlayerSpeciality.DEFENSE:
                skilllen = SpecialtyExpData._skill_defense_count;
                break;

              case EPlayerSpeciality.SUPPORT:
                skilllen = SpecialtyExpData._skill_support_count;
                break;
            }

            for(int i_skill = 0; i_skill < skilllen; i_skill++) {
              string key = _getSpecialtyDataKey((EPlayerSpeciality)i_specs, i_skill);
              expData.skillsets_exp[i_specs][i_skill] = userData.readInt32(key);
            }
          }
        }
        else {
          // init skillset
          expData.skillset = EPlayerSkillset.NONE;

          // init specialty
          for(int i_specs = 0; i_specs < SpecialtyExpData._speciality_count; i_specs++) {
            int skilllen = 0;
            switch((EPlayerSpeciality)i_specs) {
              case EPlayerSpeciality.OFFENSE:
                skilllen = SpecialtyExpData._skill_offense_count;
                break;

              case EPlayerSpeciality.DEFENSE:
                skilllen = SpecialtyExpData._skill_defense_count;
                break;

              case EPlayerSpeciality.SUPPORT:
                skilllen = SpecialtyExpData._skill_support_count;
                break;
            }

            for(int i_skill = 0; i_skill < skilllen; i_skill++) {
              expData.skillsets_exp[i_specs][i_skill] = 0;
            }
          }
        }

        playerExp[playerID] = expData;
      }
      catch(Exception e) {
        plugin.PrintToError(string.Format("Something went wrong when loading user file. PlayerID: {0}, Error: {1}", playerID, e.ToString()));
        return false;
      }

      await RecalculateSpecialty(player);
      return true;
    }

    /// <summary>
    /// This function saves player's current exp data. This uses <see cref="PlayerSavedata"/> to save and load player data
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    public async UniTask Save(UnturnedPlayer player) {
      ulong playerID = player.SteamId.m_SteamID;
      plugin.PrintToOutput(string.Format("saving {0}", player.SteamId.m_SteamID));

      try {
        Data data = new Data();
        SpecialtyExpData expData = playerExp[playerID];

        data.writeByte(_getSkillsetDataKey(), (byte)expData.skillset);

        for(int i_specs = 0; i_specs < SpecialtyExpData._speciality_count; i_specs++) {
          int skilllen = 0;
          switch((EPlayerSpeciality)i_specs) {
            case EPlayerSpeciality.OFFENSE:
              skilllen = SpecialtyExpData._skill_offense_count;
              break;

            case EPlayerSpeciality.DEFENSE:
              skilllen = SpecialtyExpData._skill_defense_count;
              break;

            case EPlayerSpeciality.SUPPORT:
              skilllen = SpecialtyExpData._skill_support_count;
              break;
          }

          for(int i_skill = 0; i_skill < skilllen; i_skill++) {
            data.writeInt32(_getSpecialtyDataKey((EPlayerSpeciality)i_specs, i_skill), expData.skillsets_exp[i_specs][i_skill]);
          }
        }

        await UniTask.SwitchToMainThread();
        PlayerSavedata.writeData(player.SteamPlayer.playerID, _savedata_path, data);
        await UniTask.SwitchToThreadPool();
      }
      catch(Exception e) {
        plugin.PrintToError(string.Format("Something went wrong when saving user file. PlayerID: {0}, Error: {1}", playerID, e.ToString()));
      }
    }

    /// <summary>
    /// Saves all currently connected player's data
    /// </summary>
    public async UniTask SaveAll() {
      foreach(var data in playerExp) {
        UnturnedUser? user = plugin.UnturnedUserProviderInstance.GetUser(new CSteamID(data.Key));
        if(user != null) {
          await Save(user.Player);
        }
      }
    }

    /// <summary>
    /// Recalculating player's level data. Typically this used when all skills is edited or when player has just connected
    /// </summary>
    /// <param name="player">Current UnturnedPlayer object</param>
    public async UniTask RecalculateSpecialty(UnturnedPlayer player) {
      plugin.PrintToOutput("recalculating speciality");
      try {
        PlayerSkills userSkill = player.Player.skills;
        SpecialtyExpData data = playerExp[player.SteamId.m_SteamID];

        for(byte i_spec = 0; i_spec < SpecialtyExpData._speciality_count; i_spec++) {
          int skill_len = 0;
          switch((EPlayerSpeciality)i_spec) {
            case EPlayerSpeciality.OFFENSE:
              skill_len = SpecialtyExpData._skill_offense_count;
              break;

            case EPlayerSpeciality.DEFENSE:
              skill_len = SpecialtyExpData._skill_defense_count;
              break;

            case EPlayerSpeciality.SUPPORT:
              skill_len = SpecialtyExpData._skill_support_count;
              break;
          }

          for(byte i_skill = 0; i_skill < skill_len; i_skill++) {
            _recalculateExpLevel(player.Player, ref data, userSkill, i_spec, i_skill);
          }
        }
      }
      catch(KeyNotFoundException) {
        plugin.PrintToError(string.Format("User cannot be found (ID: {0}). Will loading the data.", player.SteamId.m_SteamID));
        bool res = (await LoadExp(player));
        if(res) {
          await RecalculateSpecialty(player);
        }
      }
      catch(Exception e) {
        plugin.PrintToError(string.Format("User ID {0} is invalid. Cannot search for user data.", player.SteamId.m_SteamID));
        plugin.PrintToError(e.ToString());
      }
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
    public KeyValuePair<string, string> GetExp_AsProgressBar(UnturnedPlayer player, EPlayerSpeciality speciality, int skill_idx, bool getskillname = false, bool getspecname = false) {
      try {
        SpecialtyExpData expData = playerExp[player.SteamId.m_SteamID];
        int _lowborder = expData.skillsets_expborderlow[(int)speciality][skill_idx];
        int _highborder = expData.skillsets_expborderhigh[(int)speciality][skill_idx];

        int _currentexp = 0;
        float _range = 0.0f;
        int _rangebar = 0;

        int barlen = _barlength;
        if(player.Player.skills.skills[(int)speciality][skill_idx].level >= plugin.SkillConfigInstance.GetBaseLevelExp(expData.skillset, speciality, (byte)skill_idx)) {
          barlen -= _bar_onMax.Length;
          _rangebar = barlen;
          _range = 1.0f;
        }
        else {
          _currentexp = expData.skillsets_exp[(int)speciality][skill_idx];
          _range = (float)(_currentexp - _lowborder) / (_highborder - _lowborder);
          _rangebar = (int)Math.Floor(_range * _barlength);
        }

        string _strbar = "";
        for(int i = 0; i < barlen; i++) {
          if(i < _rangebar)
            _strbar += '=';
          else
            _strbar += ' ';
        }

        if(barlen < _barlength) {
          _strbar = _strbar.Insert(_strbar.Length / 2, _bar_onMax);
        }

        string _strname = "";
        if(getskillname || getspecname) {
          var specpair = SkillConfig.specskill_indexer_inverse[speciality];
          if(getskillname)
            _strname = specpair.Value[(byte)skill_idx];

          if(getspecname)
            _strname = specpair.Key + "." + _strname;
        }

        return new KeyValuePair<string, string>(_strname, string.Format("Lvl {0} [{1}] {2}%", player.Player.skills.skills[(int)speciality][skill_idx].level, _strbar, (_range * 100).ToString("F1")));
      }
      catch(Exception e) {
        plugin.PrintToError(string.Format("Something wrong when getting exp data. Error: {0}", e.ToString()));
      }

      return new KeyValuePair<string, string>();
    }
  }
}
