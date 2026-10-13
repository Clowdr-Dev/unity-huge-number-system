using System;
using NUnit.Framework;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberInvariantTests
    {
        private const int RandomSeed = 424242;
        private const int SampleCount = 10_000;
        private const double RelativeTolerance = 5e-14d;

        [Test]
        public void RandomizedSamplesRemainNormalizedAndAgreeWithStableDoubleReferences()
        {
            var random = new Random(RandomSeed);

            for (int sample = 0; sample < SampleCount; sample++)
            {
                HugeNumber left = CreateRandomHugeNumber(random, out double leftDouble);
                HugeNumber right = CreateRandomHugeNumber(random, out double rightDouble);

                AssertNormalized(left, sample, "left");
                AssertNormalized(right, sample, "right");

                HugeNumber sum = left + right;
                AssertNormalized(sum, sample, "sum");
                double additionExpected = leftDouble + rightDouble;
                if (IsStableDoubleReference(additionExpected) &&
                    Math.Abs(additionExpected) >= 1e-6d * Math.Max(Math.Abs(leftDouble), Math.Abs(rightDouble)))
                {
                    Assert.That(
                        RelativeError(sum.ToDoubleChecked(), additionExpected),
                        Is.LessThanOrEqualTo(RelativeTolerance),
                        $"Sample {sample} addition diverged from the stable double reference.");
                }

                HugeNumber product = left * right;
                AssertNormalized(product, sample, "product");
                double multiplicationExpected = leftDouble * rightDouble;
                if (IsStableDoubleReference(multiplicationExpected))
                {
                    Assert.That(
                        RelativeError(product.ToDoubleChecked(), multiplicationExpected),
                        Is.LessThanOrEqualTo(RelativeTolerance),
                        $"Sample {sample} multiplication diverged from the stable double reference.");
                }

                Assert.That(
                    Math.Sign(left.CompareTo(right)),
                    Is.EqualTo(Math.Sign(leftDouble.CompareTo(rightDouble))),
                    $"Sample {sample} comparison disagreed with the double ordering.");
            }
        }

        [Test]
        public void RandomizedIntegerPowersComposeAcrossPositiveAndNegativeExponents()
        {
            var random = new Random(RandomSeed);

            for (int sample = 0; sample < SampleCount; sample++)
            {
                HugeNumber value = CreateRandomHugeNumber(random, out _);
                long firstPower = random.Next(-8, 9);
                long secondPower = random.Next(-8, 9);

                HugeNumber separated = HugeNumber.Pow(value, firstPower) * HugeNumber.Pow(value, secondPower);
                HugeNumber combined = HugeNumber.Pow(value, firstPower + secondPower);

                Assert.That(
                    separated.ApproximatelyEquals(combined, RelativeTolerance),
                    Is.True,
                    $"Sample {sample} failed Pow(x, {firstPower}) * Pow(x, {secondPower}) = Pow(x, {firstPower + secondPower}).");
            }
        }

        private static HugeNumber CreateRandomHugeNumber(Random random, out double doubleValue)
        {
            double sign = random.Next(0, 2) == 0 ? -1d : 1d;
            double mantissa = sign * (1d + random.NextDouble() * 8.999d);
            long exponent = random.Next(-250, 251);
            doubleValue = mantissa * Math.Pow(10d, exponent);
            return HugeNumber.FromParts(mantissa, exponent);
        }

        private static void AssertNormalized(HugeNumber value, int sample, string valueName)
        {
            if (value.IsZero)
                return;

            Assert.That(
                Math.Abs(value.Mantissa),
                Is.InRange(1d, 10d),
                $"Sample {sample} produced a non-normalized {valueName} value.");
        }

        private static double RelativeError(double actual, double expected)
        {
            return Math.Abs(actual - expected) / Math.Max(Math.Abs(expected), double.Epsilon);
        }

        private static bool IsStableDoubleReference(double expected)
        {
            return double.IsFinite(expected) &&
                Math.Abs(expected) >= 1e-290d &&
                Math.Abs(expected) <= 1e290d;
        }
    }
}
