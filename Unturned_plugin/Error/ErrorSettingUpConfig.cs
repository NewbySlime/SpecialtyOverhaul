using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Error {
  /// <summary>
  /// A custom Exception class that usually thrown when there's a problem when parsing config data
  /// </summary>
  public class ErrorSettingUpConfig: Exception {
    public ErrorSettingUpConfig(string what) : base(what) { }
  }
}
