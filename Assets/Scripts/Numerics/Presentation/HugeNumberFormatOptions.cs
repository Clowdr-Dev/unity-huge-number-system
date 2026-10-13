using System;
using System.Globalization;

namespace HugeNumberSystem.Numerics.Presentation
{
    public readonly struct HugeNumberFormatOptions
    {
        public HugeNumberFormatOptions(HugeNumberNotation notation, CultureInfo culture,
            int significantDigits = 3, int minimumFractionalDigits = 0,
            int maximumFractionalDigits = 3, bool trimOrdinaryTrailingZeros = true)
        {
            if (!Enum.IsDefined(typeof(HugeNumberNotation), notation))
                throw new ArgumentOutOfRangeException(nameof(notation));
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
            if (significantDigits < 1 || significantDigits > 15)
                throw new ArgumentOutOfRangeException(nameof(significantDigits));
            if (minimumFractionalDigits < 0 || minimumFractionalDigits > 15)
                throw new ArgumentOutOfRangeException(nameof(minimumFractionalDigits));
            if (maximumFractionalDigits < 0 || maximumFractionalDigits > 15)
                throw new ArgumentOutOfRangeException(nameof(maximumFractionalDigits));
            if (minimumFractionalDigits > maximumFractionalDigits)
                throw new ArgumentException("Minimum fractional digits must not exceed maximum fractional digits.");

            Notation = notation;
            SignificantDigits = significantDigits;
            MinimumFractionalDigits = minimumFractionalDigits;
            MaximumFractionalDigits = maximumFractionalDigits;
            TrimOrdinaryTrailingZeros = trimOrdinaryTrailingZeros;
        }

        public HugeNumberNotation Notation { get; }
        public CultureInfo Culture { get; }
        public int SignificantDigits { get; }
        public int MinimumFractionalDigits { get; }
        public int MaximumFractionalDigits { get; }
        public bool TrimOrdinaryTrailingZeros { get; }
    }
}
