using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Mechanic {
  // NOTE: enum values should be bits (flags)
  public enum EEnvironment {
    // NORMAL enum is when no flags are present
    NORMAL        =         0b0,
    BURNING       =         0b1,
    COLD          =        0b10,
    FREEZING      =       0b100,

    _temp_flags   =       0b111,
    
    DEADZONE      =      0b1000,
    SAFEZONE      =     0b10000,
    FULLMOON      =    0b100000,

    DAYTIME       =   0b1000000,
    NIGHTTIME     =  0b10000000,
    
    _time_flags   =  0b11000000,

    UNDERWATER    = 0b100000000
  }
}
