using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.CustomEvent;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Persistance {
  public interface ISkillPersistance: IDisposable {
    public enum PersistanceMsg {
      NONE = 0,
      PERSISTANCE_CORRUPTED = 0b1,
      PERSISTANCE_EDITED = 0b10,
      PERSISTANCE_STILL_OPENED = 0b100,
      PERSISTANCE_RETAINERROR = 0b1000,
      PERSISTANCE_OBSOLETE = 0b10000
    }



    public SpecialtyExpData ExpData {
      get;
    }

    public SteamPlayerID PlayerID {
      get;
    }


    public void Save();

    public void Reload();

    public void Erase();

    public void AddMsgFlag(PersistanceMsg Msg);
  }
}
