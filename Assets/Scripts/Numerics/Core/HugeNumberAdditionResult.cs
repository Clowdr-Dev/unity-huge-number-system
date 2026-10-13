namespace HugeNumberSystem.Numerics
{
    public enum HugeNumberAdditionDisposition
    {
        Combined,
        LeftDominated,
        RightDominated,
    }

    public readonly struct HugeNumberAdditionResult
    {
        public HugeNumberAdditionResult(HugeNumber value, HugeNumberAdditionDisposition disposition)
        {
            Value = value;
            Disposition = disposition;
        }

        public HugeNumber Value { get; }
        public HugeNumberAdditionDisposition Disposition { get; }
    }
}
