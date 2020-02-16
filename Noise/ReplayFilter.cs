using System;
using System.Collections.Generic;
using System.Text;

namespace Noise
{
    internal interface ReplayFilter
    {
        /// <summary>
        /// Anti-Replay check for package number aka nonce
        /// </summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        bool ValidateCounter(UInt64 counter);
    }
}
