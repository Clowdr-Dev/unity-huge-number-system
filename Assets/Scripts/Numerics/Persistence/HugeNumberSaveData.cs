using System;

namespace HugeNumberSystem.Numerics.Persistence
{
    [Serializable]
    public sealed class HugeNumberSaveData
    {
        public int version;
        public string mantissa;
        public string exponent;

        public HugeNumberSaveData()
        {
        }
    }
}
