using NUnit.Framework;

namespace HugeNumberSystem.Numerics.Presentation.Tests
{
    public sealed class DefaultHugeNumberSuffixProviderTests
    {
        [TestCase(0L)]
        [TestCase(-1L)]
        public void NonSuffixGroupsReturnFalse(long group)
        {
            Assert.That(DefaultHugeNumberSuffixProvider.Instance.TryGetSuffix(group, out string suffix), Is.False);
            Assert.That(suffix, Is.Null);
        }

        [TestCase(1L, "K")]
        [TestCase(2L, "M")]
        [TestCase(3L, "B")]
        [TestCase(4L, "T")]
        [TestCase(5L, "aa")]
        [TestCase(6L, "ab")]
        [TestCase(30L, "az")]
        [TestCase(31L, "ba")]
        [TestCase(680L, "zz")]
        [TestCase(681L, "aaa")]
        [TestCase(682L, "aab")]
        public void GroupsMapToStableSuffixes(long group, string expected)
        {
            Assert.That(DefaultHugeNumberSuffixProvider.Instance.TryGetSuffix(group, out string suffix), Is.True);
            Assert.That(suffix, Is.EqualTo(expected));
        }

        [Test]
        public void LargestSupportedGroupUsesOverflowSafeGeneration()
        {
            long group = long.MaxValue / 3L;
            Assert.That(DefaultHugeNumberSuffixProvider.Instance.TryGetSuffix(group, out string suffix), Is.True);
            Assert.That(suffix, Is.Not.Empty);
            Assert.That(suffix, Does.Match("^[a-z]+$"));
        }
    }
}
