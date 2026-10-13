using System;
using NUnit.Framework;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberRepresentationTests
    {
        [Test]
        public void DefaultAndBothFloatingZerosAreCanonicalZero()
        {
            Assert.That(default(HugeNumber), Is.EqualTo(HugeNumber.Zero));
            Assert.That(HugeNumber.FromDouble(0d).Mantissa, Is.EqualTo(0d));
            Assert.That(HugeNumber.FromDouble(-0d).Exponent, Is.EqualTo(0L));
            Assert.That(HugeNumber.FromDouble(-0d).Sign, Is.EqualTo(0));
        }

        [TestCase(1250000d, 1.25d, 6L)]
        [TestCase(-0.0042d, -4.2d, -3L)]
        [TestCase(7.4d, 7.4d, 0L)]
        public void FromDoubleNormalizes(double source, double mantissa, long exponent)
        {
            var value = HugeNumber.FromDouble(source);
            Assert.That(value.Mantissa, Is.EqualTo(mantissa).Within(1e-14));
            Assert.That(value.Exponent, Is.EqualTo(exponent));
        }

        [Test]
        public void SmallestAndLargestSubnormalsNormalizeWithoutInvalidIntermediates()
        {
            var smallest = HugeNumber.FromDouble(double.Epsilon);
            var largest = HugeNumber.FromDouble(BitConverter.Int64BitsToDouble(0x000f_ffff_ffff_ffff));
            Assert.That(smallest.Mantissa, Is.InRange(1d, 10d));
            Assert.That(smallest.Exponent, Is.EqualTo(-324L));
            Assert.That(largest.Mantissa, Is.InRange(1d, 10d));
            Assert.That(double.IsFinite(largest.Mantissa), Is.True);
        }

        [Test]
        public void InvalidPartsAndExponentOverflowFail()
        {
            Assert.Throws<ArgumentException>(() => HugeNumber.FromParts(double.NaN, 0L));
            Assert.Throws<ArgumentException>(() => HugeNumber.FromParts(double.PositiveInfinity, 0L));
            Assert.Throws<OverflowException>(() => HugeNumber.FromParts(10d, long.MaxValue));
            Assert.That(HugeNumber.TryFromParts(double.NaN, 0L, out _), Is.False);
        }

        [Test]
        public void EquivalentCanonicalValuesHaveExactEqualityAndHashes()
        {
            var left = HugeNumber.FromParts(125d, 4L);
            var right = HugeNumber.FromParts(1.25d, 6L);
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ComparisonUsesSignExponentAndMantissa()
        {
            Assert.That(HugeNumber.FromParts(-9d, 20L), Is.LessThan(HugeNumber.FromParts(-1d, 20L)));
            Assert.That(HugeNumber.FromParts(-1d, 20L), Is.LessThan(HugeNumber.Zero));
            Assert.That(HugeNumber.FromParts(9d, 19L), Is.LessThan(HugeNumber.FromParts(1d, 20L)));
            Assert.That(HugeNumber.FromParts(5d, 20L), Is.GreaterThan(HugeNumber.FromParts(4d, 20L)));
        }

        [Test]
        public void CoreAssemblyDoesNotReferenceUnity()
        {
            foreach (var reference in typeof(HugeNumber).Assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("UnityEngine"));
                Assert.That(reference.Name, Does.Not.StartWith("UnityEditor"));
            }
        }
    }
}
