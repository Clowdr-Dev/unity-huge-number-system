using System;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Persistence;

namespace HugeNumberSystem.Numerics.Persistence.Tests
{
    public sealed class HugeNumberPersistenceTests
    {
        [Test]
        public void WriteUsesVersionOneG17AndInvariantExponentStrings()
        {
            var value = HugeNumber.FromParts(1.2345678901234567d, 12000L);
            HugeNumberSaveData data = HugeNumberPersistence.Write(value);

            Assert.That(data.version, Is.EqualTo(1));
            Assert.That(data.mantissa, Is.EqualTo(value.Mantissa.ToString("G17", CultureInfo.InvariantCulture)));
            Assert.That(data.exponent, Is.EqualTo("12000"));
        }

        [Test]
        public void ReadRoundTripsAcrossCurrentCultures()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                var value = HugeNumber.FromParts(-9.876543210987654d, -12000L);
                HugeNumberSaveData data = HugeNumberPersistence.Write(value);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

                Assert.That(HugeNumberPersistence.TryRead(data, out HugeNumber restored, out string error), Is.True, error);
                Assert.That(restored, Is.EqualTo(value));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [TestCase(0, "1", "0", "version")]
        [TestCase(2, "1", "0", "version")]
        [TestCase(1, null, "0", "mantissa")]
        [TestCase(1, "NaN", "0", "mantissa")]
        [TestCase(1, "1", null, "exponent")]
        [TestCase(1, "1", "9223372036854775808", "exponent")]
        public void ReadRejectsInvalidDataWithActionableFieldName(
            int version, string mantissa, string exponent, string field)
        {
            var data = new HugeNumberSaveData
            {
                version = version,
                mantissa = mantissa,
                exponent = exponent,
            };

            Assert.That(HugeNumberPersistence.TryRead(data, out HugeNumber value, out string error), Is.False);
            Assert.That(value, Is.EqualTo(HugeNumber.Zero));
            Assert.That(error, Does.Contain(field).IgnoreCase);
        }

        [Test]
        public void ReadRejectsMissingDto()
        {
            Assert.That(HugeNumberPersistence.TryRead(null, out HugeNumber value, out string error), Is.False);
            Assert.That(value, Is.EqualTo(HugeNumber.Zero));
            Assert.That(error, Does.Contain("missing").IgnoreCase);
        }

        [Test]
        public void LegacyDoubleMigrationProducesCanonicalVersionOneData()
        {
            HugeNumberSaveData data = HugeNumberPersistence.MigrateLegacyDouble(1250000d);

            Assert.That(HugeNumberPersistence.TryRead(data, out HugeNumber value, out string error), Is.True, error);
            Assert.That(value, Is.EqualTo(HugeNumber.FromParts(1.25d, 6L)));
        }

        [Test]
        public void VersionOneDtoRoundTripsThroughJsonUtility()
        {
            HugeNumber original = HugeNumber.FromParts(-1.2345678901234567d, 12000L);
            HugeNumberSaveData data = HugeNumberPersistence.Write(original);
            string json = JsonUtility.ToJson(data);
            HugeNumberSaveData restoredData = JsonUtility.FromJson<HugeNumberSaveData>(json);

            Assert.That(HugeNumberPersistence.TryRead(restoredData, out HugeNumber restored, out string error), Is.True, error);
            Assert.That(restored, Is.EqualTo(original));
        }

        [Test]
        public void JsonUtilityPreservesExponentBeyondJavaScriptSafeIntegerRange()
        {
            HugeNumber original = HugeNumber.FromParts(1.25d, long.MaxValue);
            string json = JsonUtility.ToJson(HugeNumberPersistence.Write(original));
            HugeNumberSaveData restoredData = JsonUtility.FromJson<HugeNumberSaveData>(json);

            Assert.That(json, Does.Contain("\"exponent\":\"9223372036854775807\""));
            Assert.That(restoredData.exponent, Is.EqualTo("9223372036854775807"));
            Assert.That(HugeNumberPersistence.TryRead(restoredData, out HugeNumber restored, out string error), Is.True, error);
            Assert.That(restored, Is.EqualTo(original));
        }
    }
}
