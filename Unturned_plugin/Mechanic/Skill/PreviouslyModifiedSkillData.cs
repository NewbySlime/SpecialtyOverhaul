using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill {
  /// <summary>
  /// This will hold all the initial data that has been changed
  /// </summary>
  public class PreviouslyModifiedSkillData {
    public enum ChangeCodes {
      TYPE_EXP,
      TYPE_SKILLSET,
      TYPE_EXCESS
    }

    public class ChangeExp {
      public EPlayerSpeciality Speciality;
      public int SkillIdx;
      public int Exp;
    }

    public class ChangeSkillset {
      public EPlayerSkillset Skillset;
    }

    public class ChangeExcessExp {
      public int ExcessExp;
    }



    public List<(ChangeCodes, object)> ModifiedData = new();

    public PreviouslyModifiedSkillData SetToData(ref SpecialtyExpData data) {
      SkillConfig? config = SpecialtyOverhaul.Instance?.SkillConfigInstance;
      PreviouslyModifiedSkillData _res = new();

      if(config != null) {
        ICalculationUtils calculationUtils = config.Calculation;

        foreach(var modData in ModifiedData) {
          switch(modData.Item1) {
            case ChangeCodes.TYPE_EXP: {
              if(modData.Item2 is ChangeExp _change) {
                _res.ModifiedData.Add((
                  modData.Item1,
                  new ChangeExp() {
                    Speciality = _change.Speciality,
                    SkillIdx = _change.SkillIdx,
                    Exp = data.skillsets_exp[(byte)_change.Speciality][_change.SkillIdx]
                  }
                ));
                
                data.skillsets_exp[(byte)_change.Speciality][_change.SkillIdx] =
                  _change.Exp;
              }

              break;
            }

            case ChangeCodes.TYPE_SKILLSET: {
              if(modData.Item2 is ChangeSkillset _change) {
                _res.ModifiedData.Add((
                  modData.Item1,
                  new ChangeSkillset() {
                    Skillset = data.skillset
                  }
                ));

                data.skillset = _change.Skillset;
              }

              break;
            }

            case ChangeCodes.TYPE_EXCESS: {
              if(modData.Item2 is ChangeExcessExp _change) {
                _res.ModifiedData.Add((
                  modData.Item1,
                  new ChangeExcessExp() {
                    ExcessExp = data.excess_exp
                  }
                ));

                data.excess_exp = _change.ExcessExp;
              }

              break;
            }
          }
        }
      }

      return _res;
    }


    public void CombineWithAnother(PreviouslyModifiedSkillData pmsd) {
      for(int i = 0; i < pmsd.ModifiedData.Count; i++)
        ModifiedData.Add(pmsd.ModifiedData[i]);
    }
  }
}
