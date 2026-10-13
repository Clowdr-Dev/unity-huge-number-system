namespace HugeNumberSystem.Numerics.Presentation
{
    public interface IHugeNumberSuffixProvider
    {
        bool TryGetSuffix(long group, out string suffix);
    }
}
