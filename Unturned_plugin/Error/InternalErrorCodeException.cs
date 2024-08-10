using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekos.SpecialtyPlugin.Error {
  public class InternalErrorCodeException: Exception {
    public readonly int ErrorCode;

    public InternalErrorCodeException(int errorCode) : base("Unhandled internal exception.") {
      ErrorCode = errorCode;
    }
  }
}
