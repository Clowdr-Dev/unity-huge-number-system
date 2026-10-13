using System;
using System.Globalization;

using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Persistence
{
    public static class HugeNumberPersistence
    {
        public const int CurrentVersion = 1;

        public static HugeNumberSaveData Write(HugeNumber value)
        {
            return new HugeNumberSaveData
            {
                version = CurrentVersion,
                mantissa = value.Mantissa.ToString("G17", CultureInfo.InvariantCulture),
                exponent = value.Exponent.ToString(CultureInfo.InvariantCulture),
            };
        }

        public static bool TryRead(HugeNumberSaveData data, out HugeNumber value, out string error)
        {
            value = HugeNumber.Zero;
            if (data == null)
            {
                error = "Huge number data is missing.";
                return false;
            }

            if (data.version != CurrentVersion)
            {
                error = $"Huge number version '{data.version}' is unsupported; expected '{CurrentVersion}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.mantissa) ||
                !double.TryParse(
                    data.mantissa,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double mantissa) ||
                !double.IsFinite(mantissa))
            {
                error = $"Huge number mantissa '{FormatInput(data.mantissa)}' must be a finite invariant G17 value.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.exponent) ||
                !long.TryParse(
                    data.exponent,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out long exponent))
            {
                error = $"Huge number exponent '{FormatInput(data.exponent)}' must fit a signed 64-bit invariant integer.";
                return false;
            }

            if (!HugeNumber.TryFromParts(mantissa, exponent, out value))
            {
                error = $"Huge number fields mantissa='{FormatInput(data.mantissa)}' exponent='{FormatInput(data.exponent)}' overflow during normalization.";
                return false;
            }

            error = null;
            return true;
        }

        public static HugeNumberSaveData MigrateLegacyDouble(double value)
        {
            return Write(HugeNumber.FromDouble(value));
        }

        private static string FormatInput(string input)
        {
            return input ?? "<null>";
        }
    }
}
