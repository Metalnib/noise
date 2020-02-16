using System;
using System.Collections.Generic;
using System.Text;

namespace Noise
{
    /// <summary>
    /// Implementation of Anti-Replay Algorithm without Bit Shifting.
    /// <see href="https://tools.ietf.org/html/rfc6479">RFC 6479</see>).
    /// </summary>
    internal class ReplayFilterNoBitshift : ReplayFilter
    {
        private const int CounterBitsTotal = 2048;
        private const int WordsCount = CounterBitsTotal / sizeof(ulong);
        private const int RedundantBitShift = 6;
        private const int CounterRedundantBits = sizeof(ulong)*8;
        private const int WindowSize = CounterBitsTotal - CounterRedundantBits;

        private ulong[] ReplayBitmap = new ulong[WordsCount];

        public ulong CurrentCounter { get; private set; }
        
        public bool ValidateCounter(UInt64 counter)
        {
            ulong index = counter >> RedundantBitShift;
            if (counter > CurrentCounter)
            {
                ulong CurrentIndex = CurrentCounter >> RedundantBitShift;
                ulong diff = Math.Min(index - CurrentIndex, WordsCount);
                for (uint i = 1; i <= diff  ; i++)
                        ReplayBitmap[(CurrentIndex + i) % WordsCount] = 0;
                
                CurrentCounter = counter;
            }
            else if (CurrentCounter - counter > WindowSize)
                return false; //the packet is too old, no need to update

            index = index % WordsCount;
            ulong indexBit = counter & (ulong)(CounterRedundantBits - 1);
            ulong oldValue = ReplayBitmap[index];
            ulong newValue = oldValue | ((ulong)1 << Convert.ToInt32(indexBit));
            ReplayBitmap[index] = newValue;

            return oldValue != newValue;
        }
    }
}
