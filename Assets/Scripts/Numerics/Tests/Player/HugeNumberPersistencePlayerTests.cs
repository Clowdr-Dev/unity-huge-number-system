using NUnit.Framework;
using UnityEngine;

using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Persistence;

namespace HugeNumberSystem.Numerics.Player.Tests
{
    public sealed class HugeNumberPersistencePlayerTests
    {
        [Test]
        public void VersionOneDtoRoundTripsThroughJsonUtility()
        {
            var original = HugeNumber.FromParts(1.2345678901234567d, 12000L);
            HugeNumberSaveData data = HugeNumberPersistence.Write(original);
            string json = JsonUtility.ToJson(data);
            StringAssert.Contains("\"mantissa\":\"", json);
            StringAssert.Contains("\"exponent\":\"12000\"", json);

            HugeNumberSaveData restoredData = JsonUtility.FromJson<HugeNumberSaveData>(json);
            Assert.That(HugeNumberPersistence.TryRead(restoredData, out HugeNumber restored, out string error), Is.True, error);
            Assert.That(restored, Is.EqualTo(original));
        }

        [Test]
        public void JsonUtilityPreservesExponentBeyondJavaScriptSafeIntegerRange()
        {
            const long exponent = 9_007_199_254_740_993L;
            var original = HugeNumber.FromParts(1d, exponent);
            string json = JsonUtility.ToJson(HugeNumberPersistence.Write(original));
            var data = JsonUtility.FromJson<HugeNumberSaveData>(json);
            Assert.That(HugeNumberPersistence.TryRead(data, out HugeNumber restored, out string error), Is.True, error);
            Assert.That(restored.Exponent, Is.EqualTo(exponent));
        }
    }
}
