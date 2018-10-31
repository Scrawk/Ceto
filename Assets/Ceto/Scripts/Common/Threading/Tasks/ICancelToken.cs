
using System;

namespace Ceto.Common.Threading.Tasks
{
    public interface ICancelToken
    {

        bool Cancelled { get;  }

    }
}
