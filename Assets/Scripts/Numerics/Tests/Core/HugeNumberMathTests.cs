using System;
using NUnit.Framework;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberMathTests
    {
        [Test]
        public void IntegerPowerUsesDocumentedZeroAndSignRules()
        {
            Assert.That(HugeNumber.Pow(HugeNumber.FromDouble(-2d), 3L), Is.EqualTo(HugeNumber.FromDouble(-8d)));
            Assert.That(HugeNumber.Pow(HugeNumber.FromDouble(-2d), 4L), Is.EqualTo(HugeNumber.FromDouble(16d)));
            Assert.That(HugeNumber.Pow(HugeNumber.FromDouble(2d), -3L), Is.EqualTo(HugeNumber.FromDouble(0.125d)));
            Assert.That(HugeNumber.Pow(HugeNumber.Zero, 0L), Is.EqualTo(HugeNumber.One));
            Assert.That(HugeNumber.Pow(HugeNumber.Zero, 3L), Is.EqualTo(HugeNumber.Zero));
            Assert.Throws<DivideByZeroException>(() => HugeNumber.Pow(HugeNumber.Zero, -1L));
        }

        [Test]
        public void IntegerPowerPreservesHugePowerOfTenExponents()
        {
            Assert.That(HugeNumber.Pow(HugeNumber.FromParts(1d, 1_000_000L), 3L),
                Is.EqualTo(HugeNumber.FromParts(1d, 3_000_000L)));
            Assert.That(HugeNumber.Pow(HugeNumber.Ten, long.MaxValue),
                Is.EqualTo(HugeNumber.FromParts(1d, long.MaxValue)));
            Assert.Throws<OverflowException>(() => HugeNumber.Pow(HugeNumber.FromParts(1d, 2L), long.MaxValue));
            Assert.DoesNotThrow(() => HugeNumber.Pow(HugeNumber.One, long.MinValue));
        }

        [Test]
        public void RealPowerHandlesZeroIdentitiesAndRejectsInvalidDomains()
        {
            Assert.That(HugeNumber.Pow(HugeNumber.FromParts(1d, long.MaxValue), 0d), Is.EqualTo(HugeNumber.One));
            Assert.That(HugeNumber.Pow(HugeNumber.Zero, 2.5d), Is.EqualTo(HugeNumber.Zero));
            Assert.That(HugeNumber.Pow(HugeNumber.FromParts(1d, 12000L), 0.5d),
                Is.EqualTo(HugeNumber.FromParts(1d, 6000L)));
            Assert.That(HugeNumber.Pow(HugeNumber.FromParts(1d, long.MaxValue), 1d),
                Is.EqualTo(HugeNumber.FromParts(1d, long.MaxValue)));
            Assert.Throws<DivideByZeroException>(() => HugeNumber.Pow(HugeNumber.Zero, -0.5d));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.FromDouble(-2d), 3d));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.FromDouble(-2d), 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.FromDouble(-2d), 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.One, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.FromParts(1d, 9_007_199_254_740_992L), 0.5d));
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Pow(HugeNumber.FromParts(1d, 4_503_599_627_370_496L), 2d));
        }

        [Test]
        public void LogarithmDocumentsLargeExponentResolution()
        {
            Assert.That(HugeNumber.FromParts(1d, 12000L).Log10(), Is.EqualTo(12000d));
            Assert.That(double.IsFinite(HugeNumber.FromParts(1d, long.MaxValue).Log10()), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.Zero.Log10());
            Assert.Throws<ArgumentOutOfRangeException>(() => HugeNumber.FromDouble(-1d).Log10());
        }

        [Test]
        public void RealPowerUsesDocumentedReducedToleranceAtLargeExponents()
        {
            var result = HugeNumber.Pow(HugeNumber.FromParts(2d, 1_000_000_000_000L), 0.5d);
            Assert.That(result.Exponent, Is.EqualTo(500_000_000_000L));
            Assert.That(result.Mantissa, Is.EqualTo(Math.Sqrt(2d)).Within(1e-3d));
        }

        [Test]
        public void RelativeApproximateEqualityIsExplicit()
        {
            var left = HugeNumber.FromParts(1d, 100L);
            var close = HugeNumber.FromParts(1.000000000001d, 100L);
            Assert.That(left.ApproximatelyEquals(close, 1e-11d), Is.True);
            Assert.That(left.ApproximatelyEquals(close, 1e-13d), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => left.ApproximatelyEquals(close, -1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => left.ApproximatelyEquals(close, double.PositiveInfinity));
        }
    }
}
