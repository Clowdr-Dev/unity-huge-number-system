using System;
using NUnit.Framework;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberArithmeticTests
    {
        [Test]
        public void AdditionAlignsThroughSixteenAndDiscardsAtSeventeen()
        {
            var large = HugeNumber.FromParts(1d, 20L);
            var aligned = large + HugeNumber.FromParts(9d, 4L);
            var discarded = HugeNumber.AddWithDiagnostics(large, HugeNumber.FromParts(9d, 3L));

            Assert.That(aligned, Is.GreaterThan(large));
            Assert.That(discarded.Value, Is.EqualTo(large));
            Assert.That(discarded.Disposition, Is.EqualTo(HugeNumberAdditionDisposition.LeftDominated));
        }

        [Test]
        public void OppositeSignsCancelAndNormalize()
        {
            Assert.That(HugeNumber.FromParts(4.2d, 30L) + HugeNumber.FromParts(-4.2d, 30L), Is.EqualTo(HugeNumber.Zero));
            Assert.That(HugeNumber.FromParts(1d, 20L) - HugeNumber.FromParts(2d, 20L), Is.EqualTo(HugeNumber.FromParts(-1d, 20L)));
        }

        [Test]
        public void MultiplicationAndDivisionNormalizeAndCheckErrors()
        {
            Assert.That(HugeNumber.FromParts(5d, 10L) * HugeNumber.FromParts(4d, 20L), Is.EqualTo(HugeNumber.FromParts(2d, 31L)));
            Assert.That(HugeNumber.FromParts(6d, 10L) / HugeNumber.FromParts(2d, 5L), Is.EqualTo(HugeNumber.FromParts(3d, 5L)));
            Assert.Throws<DivideByZeroException>(() => { _ = HugeNumber.One / HugeNumber.Zero; });
            Assert.Throws<OverflowException>(() => { _ = HugeNumber.FromParts(1d, long.MaxValue) * HugeNumber.Ten; });
        }

        [Test]
        public void OperatorsAndMagnitudeHelpersAgreeWithComparison()
        {
            var negative = HugeNumber.FromParts(-3d, 8L);
            var positive = HugeNumber.FromParts(2d, 8L);

            Assert.That(-HugeNumber.Zero, Is.EqualTo(HugeNumber.Zero));
            Assert.That(HugeNumber.Abs(negative), Is.EqualTo(HugeNumber.FromParts(3d, 8L)));
            Assert.That(HugeNumber.Min(negative, positive), Is.EqualTo(negative));
            Assert.That(HugeNumber.Max(negative, positive), Is.EqualTo(positive));
            Assert.That(negative < positive && positive >= negative, Is.True);
        }
    }
}
