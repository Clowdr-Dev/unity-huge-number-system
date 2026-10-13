using HugeNumberSystem.Numerics.Authoring;

using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Editor
{
    public static class HugeNumberEditorClipboard
    {
        public static string Copy(SerializedHugeNumber value)
            => value.ToHugeNumber().ToInvariantString();

        public static bool TryPaste(string text, out SerializedHugeNumber value, out string error)
        {
            if (!HugeNumber.TryParseInvariant(text, out HugeNumber parsed))
            {
                value = default;
                error = "Paste a finite invariant ordinary or scientific value; compact suffix text is not accepted.";
                return false;
            }

            value = SerializedHugeNumber.FromHugeNumber(parsed);
            error = null;
            return true;
        }
    }
}
