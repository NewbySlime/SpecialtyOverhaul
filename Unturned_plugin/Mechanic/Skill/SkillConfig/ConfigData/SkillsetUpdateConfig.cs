using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill{
  public partial class SkillConfig {
    private partial class ConfigData {
      /// <summary>
      /// Class containing configuration data
      /// </summary>
      public class SkillsetUpdateConfig {
        public byte[][] _max_level = SpecialtyExpData.InitArrayT<byte>();
        public byte[][] _start_level = SpecialtyExpData.InitArrayT<byte>();
        public int[][] _base_level = SpecialtyExpData.InitArrayT<int>();
        public float[][] _mult_level = SpecialtyExpData.InitArrayT<float>();
        public float[][] _multmult_level = SpecialtyExpData.InitArrayT<float>();
        public float[][] _ondied_edit_level_value = SpecialtyExpData.InitArrayT<float>();
        public EOnDiedEditType[][] _ondied_edit_level_type = SpecialtyExpData.InitArrayT<EOnDiedEditType>();
        public int[][] _excess_exp_increment = SpecialtyExpData.InitArrayT<int>();
        public Dictionary<(EPlayerSpeciality, byte), byte> _level_requirements = new Dictionary<(EPlayerSpeciality, byte), byte>();
        public bool _is_demoteable = false;

        public static void CopyData(ref SkillsetUpdateConfig dst, in SkillsetUpdateConfig src) {
          SpecialtyExpData.CopyArrayT<byte>(dst._max_level, src._max_level);
          SpecialtyExpData.CopyArrayT<byte>(dst._start_level, src._start_level);
          SpecialtyExpData.CopyArrayT<int>(dst._base_level, src._base_level);
          SpecialtyExpData.CopyArrayT<float>(dst._mult_level, src._mult_level);
          SpecialtyExpData.CopyArrayT<float>(dst._multmult_level, src._multmult_level);
          SpecialtyExpData.CopyArrayT<float>(dst._ondied_edit_level_value, src._ondied_edit_level_value);
          SpecialtyExpData.CopyArrayT<EOnDiedEditType>(dst._ondied_edit_level_type, src._ondied_edit_level_type);
          SpecialtyExpData.CopyArrayT<int>(dst._excess_exp_increment, src._excess_exp_increment);
          dst._level_requirements = new Dictionary<(EPlayerSpeciality, byte), byte>(src._level_requirements);
          dst._is_demoteable = src._is_demoteable;
        }
      }
    }
  }
}
