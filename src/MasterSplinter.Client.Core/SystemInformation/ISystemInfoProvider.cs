using System;
using System.Collections.Generic;

namespace MasterSplinter.Client.Core.SystemInformation
{
    public interface ISystemInfoProvider
    {
        IReadOnlyList<Tuple<string, string>> GetSystemInfo();
    }
}
