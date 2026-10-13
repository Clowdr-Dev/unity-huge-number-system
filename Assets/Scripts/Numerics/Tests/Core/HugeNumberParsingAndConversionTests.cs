using System;
using System.Globalization;
using NUnit.Framework;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberParsingAndConversionTests
    {
        [TestCase("1234", 1.234d, 3L)]
        [TestCase(" -0.025 ", -2.5d, -2L)]
        [TestCase("+1.234e567", 1.234d, 567L)]
        [TestCase("000.0012300E+8", 1.23d, 5L)]
        public void ParserAcceptsInvariantOrdinaryAndScientificText(string text, double mantissa, long exponent)
        {
            var value = HugeNumber.ParseInvariant(text);
            Assert.That(value.Mantissa, Is.EqualTo(mantissa).Within(1e-14));
            Assert.That(value.Exponent, Is.EqualTo(exponent));
        }

        [TestCase("1,234")]
        [TestCase("1.2aa")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("1.2.3")]
        public void ParserRejectsForbiddenOrMalformedText(string text)
        {
            Assert.That(HugeNumber.TryParseInvariant(text, out _), Is.False);
            Assert.Throws<FormatException>(() => HugeNumber.ParseInvariant(text));
        }

        [Test]
        public void ParserDistinguishesExponentOverflowFromMalformedText()
        {
            Assert.That(HugeNumber.TryParseInvariant("1e9223372036854775808", out _), Is.False);
            Assert.Throws<OverflowException>(() => HugeNumber.ParseInvariant("1e9223372036854775808"));
            Assert.That(HugeNumber.TryParseInvariant("99e9223372036854775807", out _), Is.False);
            Assert.Throws<OverflowException>(() => HugeNumber.ParseInvariant("99e9223372036854775807"));
            Assert.That(HugeNumber.TryParseInvariant("0.9e-9223372036854775808", out _), Is.False);
            Assert.Throws<OverflowException>(() => HugeNumber.ParseInvariant("0.9e-9223372036854775808"));
            Assert.Throws<ArgumentNullException>(() => HugeNumber.ParseInvariant(null));
        }

        [Test]
        public void OrdinaryTextCanExceedDoubleRangeWithoutOverflowingTheParser()
        {
            HugeNumber value = HugeNumber.ParseInvariant(new string('9', 400));
            Assert.That(value, Is.EqualTo(HugeNumber.FromParts(1d, 400L)));
        }

        [Test]
        public void G17InvariantTextRoundTripsCanonicalFields()
        {
            var values = new[]
            {
                HugeNumber.Zero,
                HugeNumber.FromParts(1.2345678901234567d, 12000L),
                HugeNumber.FromParts(-9.876543210987654d, -12000L),
            };
            foreach (var value in values)
                Assert.That(HugeNumber.ParseInvariant(value.ToInvariantString()), Is.EqualTo(value));
        }

        [TestCase(long.MinValue)]
        [TestCase(long.MinValue + 1L)]
        [TestCase(long.MaxValue - 1L)]
        [TestCase(long.MaxValue)]
        public void InvariantTextRoundTripsAtExponentLimits(long exponent)
        {
            var original = HugeNumber.FromParts(-1.25d, exponent);
            string text = original.ToInvariantString();

            Assert.That(HugeNumber.TryParseInvariant(text, out HugeNumber restored), Is.True, text);
            Assert.That(restored, Is.EqualTo(original));
            Assert.That(HugeNumber.ParseInvariant(text), Is.EqualTo(original));
        }

        [TestCase("01.25e9223372036854775807", 1.25d, long.MaxValue)]
        [TestCase("125e9223372036854775805", 1.25d, long.MaxValue)]
        [TestCase("0.125e-9223372036854775807", 1.25d, long.MinValue)]
        public void ParserAppliesTheDecimalOffsetBeforeCheckingExponentRange(
            string text, double mantissa, long exponent)
        {
            Assert.That(HugeNumber.ParseInvariant(text), Is.EqualTo(HugeNumber.FromParts(mantissa, exponent)));
        }

        [Test]
        public void SmallIntegersRoundTripThroughExactAndTruncatingConversions()
        {
            for (long integer = -10000; integer <= 10000; integer++)
            {
                HugeNumber value = HugeNumber.FromInt64(integer);
                Assert.That(value.TryToInt64Exact(out long exact), Is.True, $"Exact conversion of {integer}");
                Assert.That(exact, Is.EqualTo(integer));
                Assert.That(value.TryToInt64Truncating(out long truncated), Is.True);
                Assert.That(truncated, Is.EqualTo(integer), $"Truncating conversion of {integer}");
            }
        }

        [TestCase("12", 12L)]
        [TestCase("116", 116L)]
        [TestCase("-205", -205L)]
        public void ParsedIntegersHaveTheSameConversionPolicyAsIntegerFactories(string text, long expected)
        {
            HugeNumber parsed = HugeNumber.ParseInvariant(text);
            Assert.That(parsed, Is.EqualTo(HugeNumber.FromInt64(expected)));
            Assert.That(parsed.TryToInt64Exact(out long exact), Is.True);
            Assert.That(exact, Is.EqualTo(expected));
        }

        [TestCase("1.1599999999999997e2", 115L)]
        [TestCase("1.1600000000000001e2", 116L)]
        [TestCase("-1.1599999999999997e2", -115L)]
        [TestCase("-1.1600000000000001e2", -116L)]
        public void NearbyFractionalValuesAreNotSnappedToAnInteger(string text, long expectedTruncation)
        {
            HugeNumber value = HugeNumber.ParseInvariant(text);
            Assert.That(value.TryToInt64Exact(out long exact), Is.False);
            Assert.That(exact, Is.Zero);
            Assert.That(value.TryToInt64Truncating(out long truncated), Is.True);
            Assert.That(truncated, Is.EqualTo(expectedTruncation));
        }

        [Test]
        public void TruncatingTinyValuesDoesNotRequireDoubleRange()
        {
            foreach (double mantissa in new[] { -1d, 1d })
            {
                HugeNumber tiny = HugeNumber.FromParts(mantissa, long.MinValue);
                Assert.That(tiny.TryToInt64Exact(out _), Is.False);
                Assert.That(tiny.TryToInt64Truncating(out long truncated), Is.True);
                Assert.That(truncated, Is.Zero);
            }
        }

        [Test]
        public void IntegerConversionsCheckSignedRange()
        {
            Assert.That(HugeNumber.FromInt64(long.MinValue).TryToInt64Exact(out long minimum), Is.True);
            Assert.That(minimum, Is.EqualTo(long.MinValue));
            foreach (var outside in new[]
                     {
                         HugeNumber.FromParts(9.223372036854776d, 18L),
                         HugeNumber.FromParts(-9.223372036854778d, 18L),
                         HugeNumber.FromParts(1d, 100L),
                     })
            {
                Assert.That(outside.TryToInt64Exact(out long exact), Is.False);
                Assert.That(exact, Is.Zero);
                Assert.That(outside.TryToInt64Truncating(out long truncated), Is.False);
                Assert.That(truncated, Is.Zero);
            }
        }

        [Test]
        public void ParsingAndRoundTripTextIgnoreTheCurrentCulture()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var value = HugeNumber.ParseInvariant("1.25e12000");
                Assert.That(value.ToInvariantString(), Is.EqualTo("1.25e12000"));
                Assert.That(HugeNumber.TryParseInvariant("1,25e12000", out _), Is.False);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void PrimitiveConversionsDistinguishRangeRoundingAndExactness()
        {
            Assert.That(HugeNumber.FromParts(1.25d, 3L).TryToDouble(out double finite), Is.True);
            Assert.That(finite, Is.EqualTo(1250d));
            Assert.That(HugeNumber.FromParts(5d, -324L).TryToDouble(out double subnormal), Is.True);
            Assert.That(subnormal, Is.GreaterThan(0d));
            Assert.That(HugeNumber.FromParts(1d, 309L).TryToDouble(out _), Is.False);
            Assert.Throws<OverflowException>(() => HugeNumber.FromParts(1d, 309L).ToDoubleChecked());
            Assert.That(HugeNumber.FromParts(1d, -324L).TryToDouble(out _), Is.False);
            Assert.That(HugeNumber.FromParts(1d, -400L).TryToDouble(out _), Is.False);
            Assert.That(HugeNumber.FromDouble(42d).TryToInt64Exact(out long exact), Is.True);
            Assert.That(exact, Is.EqualTo(42L));
            Assert.That(HugeNumber.FromDouble(42.9d).TryToInt64Exact(out _), Is.False);
            Assert.That(HugeNumber.FromDouble(42.9d).TryToInt64Truncating(out long truncated), Is.True);
            Assert.That(truncated, Is.EqualTo(42L));
            Assert.That(HugeNumber.FromDouble(-42.9d).TryToInt64Truncating(out truncated), Is.True);
            Assert.That(truncated, Is.EqualTo(-42L));
        }
    }
}
