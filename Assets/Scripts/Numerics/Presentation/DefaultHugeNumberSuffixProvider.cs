namespace HugeNumberSystem.Numerics.Presentation
{
    public sealed class DefaultHugeNumberSuffixProvider : IHugeNumberSuffixProvider
    {
        public static DefaultHugeNumberSuffixProvider Instance { get; } = new DefaultHugeNumberSuffixProvider();

        private DefaultHugeNumberSuffixProvider()
        {
        }

        public bool TryGetSuffix(long group, out string suffix)
        {
            switch (group)
            {
                case 1L:
                    suffix = "K";
                    return true;
                case 2L:
                    suffix = "M";
                    return true;
                case 3L:
                    suffix = "B";
                    return true;
                case 4L:
                    suffix = "T";
                    return true;
                case 0L:
                default:
                    if (group < 5L)
                    {
                        suffix = null;
                        return false;
                    }

                    break;
            }

            ulong remaining = checked((ulong)(group - 5L));
            int width = 2;
            ulong blockSize = 26UL * 26UL;
            while (remaining >= blockSize)
            {
                remaining -= blockSize;
                width++;
                blockSize = blockSize > ulong.MaxValue / 26UL
                    ? ulong.MaxValue
                    : blockSize * 26UL;
            }

            var characters = new char[width];
            for (int index = width - 1; index >= 0; index--)
            {
                characters[index] = (char)('a' + remaining % 26UL);
                remaining /= 26UL;
            }

            suffix = new string(characters);
            return true;
        }
    }
}
