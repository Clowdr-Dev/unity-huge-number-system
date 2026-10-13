using NUnit.Framework;

using HugeNumberSystem.Numerics.Authoring;
using HugeNumberSystem.Numerics.Editor;

namespace HugeNumberSystem.Numerics.Authoring.Tests
{
    public sealed class HugeNumberEditorClipboardTests
    {
        [Test]
        public void CopyUsesCanonicalInvariantText()
        {
            var serialized = new SerializedHugeNumber(125d, 4L);

            Assert.That(HugeNumberEditorClipboard.Copy(serialized), Is.EqualTo("1.25e6"));
        }

        [Test]
        public void PasteNormalizesValidTextAndRejectsDisplaySuffixes()
        {
            Assert.That(
                HugeNumberEditorClipboard.TryPaste(
                    "1.25e12000", out SerializedHugeNumber value, out string error),
                Is.True,
                error);
            Assert.That(value.Mantissa, Is.EqualTo(1.25d));
            Assert.That(value.Exponent, Is.EqualTo(12000L));

            Assert.That(
                HugeNumberEditorClipboard.TryPaste("1.25aa", out _, out error),
                Is.False);
            Assert.That(error, Does.Contain("scientific").IgnoreCase);
        }
    }
}
