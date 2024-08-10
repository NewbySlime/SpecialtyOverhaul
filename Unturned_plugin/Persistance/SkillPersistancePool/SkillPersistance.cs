using Cysharp.Threading.Tasks;
using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Binding;
using Nito.AsyncEx;
using NuGet.Protocol.Plugins;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Xml.Linq;
using OpenMod.API.Users;
using YamlDotNet.Core.Tokens;
using Nekos.SpecialtyPlugin.CustomEvent;

namespace Nekos.SpecialtyPlugin.Persistance
{
  public partial class SkillPersistancePool {
    // NOTES: File structure of saving files in Unturned is as simple as using newline to know whether it is a key or not. If using bytes of array to save a data, it's best to put it as hex or other types of information.
    private class SkillPersistance : Bindable, ISkillPersistance, IDisposable {
      /// <summary>
      /// Path of the save file
      /// </summary>
      private readonly static string _savedata_path = "/Player/nekos.specovh.dat";

      // for savedata, keys needed to be a length of 4 chars
      // I don't know why I decided this, probably just me too scared of being inefficient
      private readonly static string _savedata_speckey = "sp.e";
      private readonly static string _savedata_speckey_lvl = "sp.l";
      private readonly static string _savedata_skillsetkey = "ss.v";
      private readonly static string _savedata_excessexp = "ex.e";
      private readonly static string _savedata_persistance_error = "pl.m";
      private readonly static string _savedata_savefile_version = "sf.v";

      private readonly static Mutex _persistanceMutex = new();

      private readonly static int _savefile_version_current = 1;


      protected SteamPlayerID _playerID;
      public SteamPlayerID PlayerID {
        get {
          return _playerID; 
        } 
      }


      protected SpecialtyExpData _expData;
      public SpecialtyExpData ExpData {
        get {
          return _expData;
        }
      }


      public UnturnedUser? _user;
      public ISkillPersistance.PersistanceMsg _pending_msg;



      /// <summary>
      /// Getting a key for save files by certain specialty and skill
      /// </summary>
      /// <param name="spec">What specialty</param>
      /// <param name="skill_idx">What skill</param>
      /// <returns></returns>
      private string _getDataKey(string key, EPlayerSpeciality spec, int skill_idx) {
        return key + (char)spec + (char)skill_idx;
      }

      /// <summary>
      /// Getting a key of skillset for save files
      /// </summary>
      /// <returns></returns>
      private string _getSkillsetDataKey() {
        return _savedata_skillsetkey;
      }

      private void _writeAsBytes(string key, Data file, byte[] dataBytes) {
        string _datastr = BitConverter.ToString(dataBytes).Replace("-", "");
        file.writeString(key, _datastr);
      }

      private byte[] _readAsBytes(string key, Data file) {
        string _datastr = file.readString(key);
        byte[] _databytes = new byte[_datastr.Length / 2];
        for(int i = 0; i < _datastr.Length; i += 2)
          _databytes[i / 2] = byte.Parse(_datastr.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);

        return _databytes;
      }

      private async Task _saveData() {
        await Task.Run(() => {
          ICalculationUtils? calcutil = SpecialtyOverhaul.Instance?.SkillConfigInstance.Calculation;

          if(calcutil == null)
            return;

          try {
            Data data = new Data();

            // current savefile version
            data.writeInt32(_savedata_savefile_version, _savefile_version_current);

            // saving skillset
            _writeAsBytes(_getSkillsetDataKey(), data, new byte[] {(byte)_expData.skillset});

            SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
              // saving player exp in integer
              _writeAsBytes(_getDataKey(_savedata_speckey, spec, skill_idx), data, BitConverter.GetBytes(_expData.skillsets_exp[(byte)spec][skill_idx]));

              // saving player level in float
              float _lvl = calcutil.CalculateLevelFloat(_expData, spec, (byte)skill_idx);

              SpecialtyOverhaul.Instance?.PrintToOutput(string.Format("f {0}, n {1}", _lvl, _expData.skillsets_exp[(byte)spec][skill_idx]));

              _writeAsBytes(_getDataKey(_savedata_speckey_lvl, spec, skill_idx), data, BitConverter.GetBytes(_lvl));
            });

            // saving excess exp
            _writeAsBytes(_savedata_excessexp, data, BitConverter.GetBytes(_expData.excess_exp));

            // saving error msg
            SpecialtyOverhaul.Instance?.PrintToOutput(string.Format("error: {0}", (int)_pending_msg));
            _writeAsBytes(_savedata_persistance_error, data, BitConverter.GetBytes((int)_pending_msg));

            _persistanceMutex.WaitOne();
            PlayerSavedata.writeData(_playerID, _savedata_path, data);
            _persistanceMutex.ReleaseMutex();
          }
          catch(Exception e) {
            SpecialtyOverhaul.Instance?.PrintToError(string.Format("Something went wrong when saving user file. PlayerID: {0}, Error: {1}", _playerID.steamID.m_SteamID, e.ToString()));
          }
        });
      }

      private void _initNewData() {
        // init skillset
        _expData.skillset = EPlayerSkillset.NONE;

        // init excessdata
        _expData.excess_exp = 0;

        // init skills
        SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
          _expData.skillsets_exp[(byte)spec][skill_idx] = 0;
          _expData.skillsets_exp_fraction[(byte)spec][skill_idx] = 0;
        });

        // init error msg
        _pending_msg = ISkillPersistance.PersistanceMsg.NONE;
      }

      /// <summary>
      /// This function loads player's exp data. Can also be used for reloading, if the file is edited. This uses <see cref="PlayerSavedata"/> to save and load player data
      /// </summary>
      private async Task _loadData() {
        SpecialtyOverhaul? plugin = SpecialtyOverhaul.Instance;
        if(plugin == null)
          return;

        bool allowRetain = plugin.SkillConfigInstance.GetPlayerRetainLevel();
        ICalculationUtils calcutil = plugin.SkillConfigInstance.Calculation;

        // if there's a data about the specialty, or creating new data
        if(PlayerSavedata.fileExists(_playerID, _savedata_path)) {
          try {
            _persistanceMutex.WaitOne();
            Data userData = PlayerSavedata.readData(_playerID, _savedata_path);
            _persistanceMutex.ReleaseMutex();

            // getting savefile version
            int _version = userData.readInt32(_savedata_savefile_version);
            switch(_version) {
              // current file version
              case 1: {
                // getting skillset
                _expData.skillset = (EPlayerSkillset)_readAsBytes(_getSkillsetDataKey(), userData)[0];

                int _retaincount = 0;
                int _count = 0;
                // getting exp
                SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skill_idx) => {
                  _count++;

                  // if player can retain their levels
                  string key = _getDataKey(_savedata_speckey_lvl, spec, skill_idx);
                  if(allowRetain && userData.has(key)) {
                    float _lvl = BitConverter.ToSingle(_readAsBytes(key, userData), 0);

                    plugin?.PrintToOutput(string.Format("f: {0}", _lvl));

                    _expData.skillsets_exp[(byte)spec][skill_idx] = calcutil.CalculateLevelExp(_expData, spec, skill_idx, _lvl);

                    _retaincount++;
                  }
                  else {
                    key = _getDataKey(_savedata_speckey, spec, skill_idx);
                    _expData.skillsets_exp[(byte)spec][skill_idx] = BitConverter.ToInt32(_readAsBytes(key, userData), 0);
                  }
                });

                // getting excess data
                _expData.excess_exp = BitConverter.ToInt32(_readAsBytes(_savedata_excessexp, userData), 0);

                // loading error msg
                _pending_msg = (ISkillPersistance.PersistanceMsg)BitConverter.ToInt32(_readAsBytes(_savedata_persistance_error, userData), (int)ISkillPersistance.PersistanceMsg.NONE);


                if(allowRetain && _retaincount != _count) {
                  plugin?.PrintToOutput("pending warning to player");

                  AddMsgFlag(ISkillPersistance.PersistanceMsg.PERSISTANCE_CORRUPTED | ISkillPersistance.PersistanceMsg.PERSISTANCE_RETAINERROR);

                  plugin?.PrintWarning(string.Format("Player (id: {0}) levels can't be retained to new values.", _playerID.steamID));
                }
                else
                  _pending_msg &= ~(ISkillPersistance.PersistanceMsg.PERSISTANCE_CORRUPTED | ISkillPersistance.PersistanceMsg.PERSISTANCE_RETAINERROR);

                break;
              }

            
              // first version of filesave
              default: {
                _initNewData();

                // getting skillset
                _expData.skillset = (EPlayerSkillset)userData.readByte(_getSkillsetDataKey());

                // getting exp
                SpecialtyExpData.IterateArray((EPlayerSpeciality spec, byte skillidx) => {
                  string key = _getDataKey(_savedata_speckey, spec, skillidx);
                  _expData.skillsets_exp[(byte)spec][skillidx] = userData.readInt32(key);
                });

                // set error msg
                _pending_msg |= ISkillPersistance.PersistanceMsg.PERSISTANCE_OBSOLETE;
            
                break;
              }
            }
          }
          catch(Exception e) {
            if(plugin != null) {
              plugin.PrintToError(string.Format("Error thrown when loading save data for UserID {0}, CharID {1}.", _playerID.steamID.m_SteamID, _playerID.characterID));
              plugin.PrintToError(e.ToString());
            }

            _initNewData();
          }
        }
        else
          _initNewData();

        if((int)(_pending_msg & ISkillPersistance.PersistanceMsg.PERSISTANCE_OBSOLETE) > 0)
          plugin?.PrintWarning(string.Format("Player {0} has an obsolete filesave.", _playerID.steamID.m_SteamID));

        ReApplyData();
      }

      private static async void _send_msg_to_user(UnturnedUser user, ISkillPersistance.PersistanceMsg _msg) {
        if((int)(_msg & ISkillPersistance.PersistanceMsg.PERSISTANCE_CORRUPTED) > 0) {
          if((int)(_msg & ISkillPersistance.PersistanceMsg.PERSISTANCE_RETAINERROR) > 0)
            await user.PrintMessageAsync("Your level data seems corrupted/edited, some levels will not retain to new configuration.", System.Drawing.Color.Red);
          else
            await user.PrintMessageAsync("Your level data seems corrupted/edited.", System.Drawing.Color.Red);
        }

        if((int)(_msg & ISkillPersistance.PersistanceMsg.PERSISTANCE_EDITED) > 0)
          await user.PrintMessageAsync("Your level data has been edited by the admin.", System.Drawing.Color.Yellow);
      }


      protected override void OnNotBinded() {
        Dispose();
      }



      public SkillPersistance(SteamPlayerID steamPlayerID, UnturnedUser? user = null) {
        _user = user;
        _playerID = steamPlayerID;
        _expData = new();

        _loadData().Wait();
      }

      ~SkillPersistance() {
        Dispose();
      }


      public void Save() {
        Task.Run(async () => { await _saveData(); }).Wait();
      }

      public async Task SaveAsync() {
        await _saveData();
      }

      public void Reload() {
        Task.Run(async () => { await _loadData(); }).Wait();
      }

      public async Task ReloadAsync() {
        await _loadData();
      }

      public void Erase() {
        _expData = new SpecialtyExpData();
        Save();
      }

      public void AddMsgFlag(ISkillPersistance.PersistanceMsg msg) {
        _pending_msg |= msg;

        if(_user != null && (int)_pending_msg > 0) {
          SpecialtyOverhaul.Instance?.PrintToOutput("sending pending msg");
          Task.Run(() => _send_msg_to_user(_user, _pending_msg)).Wait();
          _pending_msg = ISkillPersistance.PersistanceMsg.NONE;
        }
      }


      public void Dispose() {
        _skillDataPool.Remove((_playerID.steamID, _playerID.characterID));
      }


      public static bool FileExists(SteamPlayerID playerID) {
        return PlayerSavedata.fileExists(playerID, _savedata_path);
      }


      public void ReApplyData() {
        SpecialtyOverhaul.Instance?.SkillConfigInstance.Calculation.ReCalculateAllSkillTo(_user, _expData);
      }
    }
  }
}
