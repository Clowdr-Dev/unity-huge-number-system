using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Authoring;

namespace HugeNumberSystem.Numerics.Authoring.Tests
{
    public sealed class SerializedHugeNumberTests
    {
        [Test]
        public void SerializedFieldsConvertThroughCoreNormalization()
        {
            var serialized = new SerializedHugeNumber(125d, 4L);
            HugeNumber value = serialized.ToHugeNumber();

            Assert.That(value, Is.EqualTo(HugeNumber.FromParts(1.25d, 6L)));
            Assert.That(SerializedHugeNumber.FromHugeNumber(value).Mantissa, Is.EqualTo(1.25d));
            Assert.That(SerializedHugeNumber.FromHugeNumber(value).Exponent, Is.EqualTo(6L));
        }

        [Test]
        public void InvalidSerializedValueFailsWithActionableContext()
        {
            var serialized = new SerializedHugeNumber(double.NaN, 4L);

            Assert.That(serialized.TryToHugeNumber(out HugeNumber value, out string error), Is.False);
            Assert.That(value, Is.EqualTo(HugeNumber.Zero));
            Assert.That(error, Does.Contain("mantissa").IgnoreCase);

            var issues = serialized.Validate("Enemy Definition: Tank", "reward.amount");
            Assert.That(issues, Has.Some.Property("Message").Contains("Enemy Definition: Tank"));
            Assert.That(issues, Has.Some.Property("Message").Contains("reward.amount"));
        }

        [Test]
        public void UnnormalizedAndHighDigitMantissasProduceWarningsNotErrors()
        {
            var serialized = new SerializedHugeNumber(1234567890123456d, 0L);
            var issues = serialized.Validate("Economy", "baseCost");

            Assert.That(issues, Has.Some.Property("Severity").EqualTo(HugeNumberAuthoringIssueSeverity.Warning));
            Assert.That(issues, Has.None.Property("Severity").EqualTo(HugeNumberAuthoringIssueSeverity.Error));
        }

        [Test]
        public void UnitySerializationPreservesRawBridgeFields()
        {
            var host = new SerializationHost { value = new SerializedHugeNumber(1.25d, 12000L) };
            string json = JsonUtility.ToJson(host);
            var restored = JsonUtility.FromJson<SerializationHost>(json);

            Assert.That(restored.value.Mantissa, Is.EqualTo(1.25d));
            Assert.That(restored.value.Exponent, Is.EqualTo(12000L));
        }

        [Test]
        public void SerializedObjectConfiguresStableRawFieldNamesWithoutReplacingTheHost()
        {
            var host = ScriptableObject.CreateInstance<SerializedObjectHost>();
            try
            {
                var serializedObject = new SerializedObject(host);
                SerializedProperty value = serializedObject.FindProperty("value");
                SerializedProperty mantissa = value.FindPropertyRelative("_mantissa");
                SerializedProperty exponent = value.FindPropertyRelative("_exponent");

                Assert.That(mantissa, Is.Not.Null);
                Assert.That(exponent, Is.Not.Null);

                mantissa.doubleValue = 125d;
                exponent.longValue = 4L;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(host.Value.Mantissa, Is.EqualTo(125d));
                Assert.That(host.Value.Exponent, Is.EqualTo(4L));
                Assert.That(host.Value.ToHugeNumber(), Is.EqualTo(HugeNumber.FromParts(1.25d, 6L)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Serializable]
        private sealed class SerializationHost
        {
            public SerializedHugeNumber value;
        }

        private sealed class SerializedObjectHost : ScriptableObject
        {
            [SerializeField] private SerializedHugeNumber value = new SerializedHugeNumber(1.25d, 12000L);

            public SerializedHugeNumber Value => value;
        }
    }
}
