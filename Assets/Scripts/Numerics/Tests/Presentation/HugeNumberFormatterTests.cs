using System;
using System.Globalization;
using NUnit.Framework;

using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Presentation.Tests
{
    public sealed class HugeNumberFormatterTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly HugeNumberFormatter _formatter = new HugeNumberFormatter();

        [TestCase(0d, "0")]
        [TestCase(12.345d, "12.3")]
        [TestCase(999d, "999")]
        [TestCase(1250d, "1.25K")]
        [TestCase(9999000d, "10.0M")]
        public void CompactFormatsOrdinaryAndNamedGroups(double source, string expected)
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant);
            Assert.That(_formatter.Format(HugeNumber.FromDouble(source), options), Is.EqualTo(expected));
        }

        [Test]
        public void CompactUsesScientificFallbackAndAlphabeticGroups()
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant);
            Assert.That(_formatter.Format(HugeNumber.FromParts(5d, -4L), options), Is.EqualTo("5.00e-4"));
            Assert.That(_formatter.Format(HugeNumber.FromParts(1d, -3L), options), Is.EqualTo("0.001"));
            Assert.That(_formatter.Format(HugeNumber.FromParts(1d, 15L), options), Is.EqualTo("1.00aa"));
            Assert.That(_formatter.Format(HugeNumber.FromParts(-4.2d, 18L), options), Is.EqualTo("-4.20ab"));
        }

        [Test]
        public void AwayFromZeroRoundingCarriesAcrossMagnitudeGroups()
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant);
            Assert.That(_formatter.Format(HugeNumber.FromDouble(999.5d), options), Is.EqualTo("1.00K"));
            Assert.That(_formatter.Format(HugeNumber.FromDouble(-999500d), options), Is.EqualTo("-1.00M"));
        }

        [TestCase(999.5d, "1.00K")]
        [TestCase(-999.5d, "-1.00K")]
        [TestCase(999.499999999999d, "999")]
        [TestCase(-999.499999999999d, "-999")]
        [TestCase(999.500000000001d, "1.00K")]
        [TestCase(-999.500000000001d, "-1.00K")]
        public void DecimalMidpointsRoundAwayFromZeroWithoutMovingNearbyValues(double source, string expected)
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant);
            Assert.That(_formatter.Format(HugeNumber.FromDouble(source), options), Is.EqualTo(expected));
        }

        [Test]
        public void EngineeringUsesFloorDivisionForNegativeExponents()
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Engineering, Invariant);
            Assert.That(_formatter.Format(HugeNumber.FromParts(5d, -4L), options), Is.EqualTo("500e-6"));
            Assert.That(_formatter.Format(HugeNumber.FromParts(1d, long.MinValue), options),
                Is.EqualTo("1.00e-9223372036854775808"));
        }

        [Test]
        public void RoundedScientificAndEngineeringCoefficientsCarryIntoTheExponent()
        {
            var scientific = new HugeNumberFormatOptions(HugeNumberNotation.Scientific, Invariant);
            var engineering = new HugeNumberFormatOptions(HugeNumberNotation.Engineering, Invariant);
            var value = HugeNumber.FromParts(9.999d, 5L);
            Assert.That(_formatter.Format(value, scientific), Is.EqualTo("1.00e6"));
            Assert.That(_formatter.Format(value, engineering), Is.EqualTo("1.00e6"));
        }

        [Test]
        public void CultureChangesDecimalTextButNotMagnitudeSelection()
        {
            var french = CultureInfo.GetCultureInfo("fr-FR");
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, french);
            Assert.That(_formatter.Format(HugeNumber.FromDouble(1250d), options), Is.EqualTo("1,25K"));
        }

        [Test]
        public void CultureSuppliesTheNegativeSignWhileExponentTextStaysInvariant()
        {
            var culture = (CultureInfo)Invariant.Clone();
            culture.NumberFormat.NegativeSign = "~";
            var compact = new HugeNumberFormatOptions(HugeNumberNotation.Compact, culture);
            var scientific = new HugeNumberFormatOptions(HugeNumberNotation.Scientific, culture);
            Assert.That(_formatter.Format(HugeNumber.FromDouble(-1250d), compact), Is.EqualTo("~1.25K"));
            Assert.That(_formatter.Format(HugeNumber.FromParts(1.25d, -6L), scientific), Is.EqualTo("1.25e-6"));
        }

        [Test]
        public void TrailingZeroPolicyDoesNotMutateTheSource()
        {
            var source = HugeNumber.FromDouble(12.3d);
            var keepZeros = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant,
                significantDigits: 5, minimumFractionalDigits: 0, maximumFractionalDigits: 3,
                trimOrdinaryTrailingZeros: false);
            var trimZeros = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant,
                significantDigits: 5, minimumFractionalDigits: 0, maximumFractionalDigits: 3,
                trimOrdinaryTrailingZeros: true);
            Assert.That(_formatter.Format(source, keepZeros), Is.EqualTo("12.300"));
            Assert.That(_formatter.Format(source, trimZeros), Is.EqualTo("12.3"));
            Assert.That(source, Is.EqualTo(HugeNumber.FromDouble(12.3d)));
        }

        [Test]
        public void MissingCustomSuffixFallsBackToScientificNotation()
        {
            var formatter = new HugeNumberFormatter(new NoSuffixProvider());
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant);
            Assert.That(formatter.Format(HugeNumber.FromParts(1d, 6L), options), Is.EqualTo("1.00e6"));
        }

        private sealed class NoSuffixProvider : IHugeNumberSuffixProvider
        {
            public bool TryGetSuffix(long group, out string suffix)
            {
                suffix = null;
                return false;
            }
        }

        [Test]
        public void FractionalBoundsOverrideSignificantDigits()
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Scientific, Invariant,
                significantDigits: 5, minimumFractionalDigits: 1, maximumFractionalDigits: 1);
            Assert.That(_formatter.Format(HugeNumber.FromParts(1.26d, 50L), options), Is.EqualTo("1.3e50"));
        }

        [Test]
        public void OrdinaryFormattingNeverRoundsANonzeroValueToDisplayZero()
        {
            var options = new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant,
                significantDigits: 3, minimumFractionalDigits: 0, maximumFractionalDigits: 0);
            Assert.That(_formatter.Format(HugeNumber.FromParts(1d, -3L), options), Is.EqualTo("1e-3"));
        }

        [Test]
        public void InvalidOptionsFailAtConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new HugeNumberFormatOptions(HugeNumberNotation.Compact, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant, significantDigits: 0));
            Assert.Throws<ArgumentException>(() => new HugeNumberFormatOptions(HugeNumberNotation.Compact, Invariant,
                minimumFractionalDigits: 3, maximumFractionalDigits: 2));
            Assert.Throws<ArgumentException>(() => _formatter.Format(HugeNumber.One, default));
        }
    }
}
