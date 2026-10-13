using System;
using System.Globalization;

using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Presentation
{
    public sealed class HugeNumberFormatter
    {
        private const int MaximumStabilizationPasses = 4;

        private readonly IHugeNumberSuffixProvider _suffixProvider;

        private enum CarryMode
        {
            Scientific,
            CompactGroup,
            EngineeringExponent,
        }

        private readonly struct RoundedMagnitude
        {
            public RoundedMagnitude(double coefficient, long exponent, int fractionalDigits, bool roundedToZero)
            {
                Coefficient = coefficient;
                Exponent = exponent;
                FractionalDigits = fractionalDigits;
                RoundedToZero = roundedToZero;
            }

            public double Coefficient { get; }
            public long Exponent { get; }
            public int FractionalDigits { get; }
            public bool RoundedToZero { get; }
        }

        public HugeNumberFormatter(IHugeNumberSuffixProvider suffixProvider = null)
        {
            _suffixProvider = suffixProvider ?? DefaultHugeNumberSuffixProvider.Instance;
        }

        public string Format(HugeNumber value, HugeNumberFormatOptions options)
        {
            ValidateOptions(options);
            if (value.IsZero)
                return "0";

            switch (options.Notation)
            {
                case HugeNumberNotation.Compact:
                    return FormatCompact(value, options);
                case HugeNumberNotation.Scientific:
                    return FormatScientific(value, options);
                case HugeNumberNotation.Engineering:
                    return FormatEngineering(value, options);
                default:
                    throw new ArgumentException("The number notation is invalid.", nameof(options));
            }
        }

        private string FormatCompact(HugeNumber value, HugeNumberFormatOptions options)
        {
            if (value.Exponent < -3L)
                return FormatScientific(value, options);

            if (value.Exponent < 3L)
                return FormatOrdinary(value, options);

            long group = value.Exponent / 3L;
            long groupExponent = checked(group * 3L);
            int remainder = checked((int)(value.Exponent - groupExponent));
            double coefficient = value.Mantissa * Math.Pow(10d, remainder);
            return FormatCompactMagnitude(value, coefficient, group, options);
        }

        private string FormatOrdinary(HugeNumber value, HugeNumberFormatOptions options)
        {
            double coefficient = value.Mantissa * Math.Pow(10d, (int)value.Exponent);
            RoundedMagnitude rounded = Stabilize(coefficient, 0L, CarryMode.CompactGroup, options);
            if (rounded.RoundedToZero)
                return FormatScientific(value, options);

            if (rounded.Exponent != 0L)
                return FormatCompactMagnitude(rounded.Coefficient, rounded.Exponent, rounded.FractionalDigits,
                    value, options);

            return FormatCoefficient(rounded.Coefficient, rounded.FractionalDigits, options,
                trimTrailingZeros: options.TrimOrdinaryTrailingZeros);
        }

        private string FormatCompactMagnitude(HugeNumber value, double coefficient, long group,
            HugeNumberFormatOptions options)
        {
            RoundedMagnitude rounded = Stabilize(coefficient, group, CarryMode.CompactGroup, options);
            if (rounded.RoundedToZero)
                return FormatScientific(value, options);

            return FormatCompactMagnitude(rounded.Coefficient, rounded.Exponent, rounded.FractionalDigits,
                value, options);
        }

        private string FormatCompactMagnitude(double coefficient, long group, int fractionalDigits,
            HugeNumber value, HugeNumberFormatOptions options)
        {
            if (!_suffixProvider.TryGetSuffix(group, out string suffix))
                return FormatScientific(value, options);

            return FormatCoefficient(coefficient, fractionalDigits, options, trimTrailingZeros: false) + suffix;
        }

        private string FormatScientific(HugeNumber value, HugeNumberFormatOptions options)
        {
            RoundedMagnitude rounded = Stabilize(value.Mantissa, value.Exponent,
                CarryMode.Scientific, options);
            if (rounded.RoundedToZero)
                throw new InvalidOperationException("Scientific formatting rounded a nonzero coefficient to zero.");

            return FormatCoefficient(rounded.Coefficient, rounded.FractionalDigits, options,
                trimTrailingZeros: false) + "e" + rounded.Exponent.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatEngineering(HugeNumber value, HugeNumberFormatOptions options)
        {
            if (!TryGetEngineeringExponent(value.Exponent, out long engineeringExponent))
                return FormatScientific(value, options);

            long remainder = value.Exponent - engineeringExponent;
            double coefficient = value.Mantissa * Math.Pow(10d, (int)remainder);
            RoundedMagnitude rounded = Stabilize(coefficient, engineeringExponent,
                CarryMode.EngineeringExponent, options);
            if (rounded.RoundedToZero)
                throw new InvalidOperationException("Engineering formatting rounded a nonzero coefficient to zero.");

            return FormatCoefficient(rounded.Coefficient, rounded.FractionalDigits, options,
                trimTrailingZeros: false) + "e" + rounded.Exponent.ToString(CultureInfo.InvariantCulture);
        }

        private static RoundedMagnitude Stabilize(double coefficient, long exponent, CarryMode carryMode,
            HugeNumberFormatOptions options)
        {
            for (int pass = 0; pass < MaximumStabilizationPasses; pass++)
            {
                int decimalOrder = GetDecimalOrder(coefficient);
                int fractionalDigits = GetFractionalDigits(decimalOrder, options);
                double rounded = (double)Math.Round((decimal)coefficient, fractionalDigits,
                    MidpointRounding.AwayFromZero);
                if (rounded == 0d)
                    return new RoundedMagnitude(0d, exponent, fractionalDigits, roundedToZero: true);

                int roundedDecimalOrder = GetDecimalOrder(rounded);
                int roundedFractionalDigits = GetFractionalDigits(roundedDecimalOrder, options);
                coefficient = rounded;
                if (roundedDecimalOrder != decimalOrder || roundedFractionalDigits != fractionalDigits)
                    continue;

                if (NeedsCarry(coefficient, carryMode))
                {
                    coefficient /= GetCarryFactor(carryMode);
                    exponent = checked(exponent + GetCarryIncrement(carryMode));
                    continue;
                }

                return new RoundedMagnitude(coefficient, exponent, roundedFractionalDigits, roundedToZero: false);
            }

            throw new InvalidOperationException("Huge number formatting did not converge.");
        }

        private static int GetDecimalOrder(double coefficient)
        {
            return (int)Math.Floor(Math.Log10(Math.Abs(coefficient)));
        }

        private static int GetFractionalDigits(int decimalOrder, HugeNumberFormatOptions options)
        {
            int impliedFractionalDigits = options.SignificantDigits - 1 - decimalOrder;
            return Math.Max(options.MinimumFractionalDigits,
                Math.Min(options.MaximumFractionalDigits, impliedFractionalDigits));
        }

        private static bool NeedsCarry(double coefficient, CarryMode carryMode)
        {
            double absoluteCoefficient = Math.Abs(coefficient);
            return carryMode == CarryMode.Scientific
                ? absoluteCoefficient >= 10d
                : absoluteCoefficient >= 1000d;
        }

        private static double GetCarryFactor(CarryMode carryMode)
        {
            return carryMode == CarryMode.Scientific ? 10d : 1000d;
        }

        private static long GetCarryIncrement(CarryMode carryMode)
        {
            return carryMode == CarryMode.EngineeringExponent ? 3L : 1L;
        }

        private static bool TryGetEngineeringExponent(long exponent, out long engineeringExponent)
        {
            long group = FloorDivideByThree(exponent);
            if (group < 0L && group < long.MinValue / 3L)
            {
                engineeringExponent = 0L;
                return false;
            }

            engineeringExponent = group * 3L;
            return true;
        }

        private static long FloorDivideByThree(long exponent)
        {
            long quotient = exponent / 3L;
            long remainder = exponent % 3L;
            return remainder < 0L ? quotient - 1L : quotient;
        }

        private static string FormatCoefficient(double coefficient, int fractionalDigits,
            HugeNumberFormatOptions options, bool trimTrailingZeros)
        {
            string format = "F" + fractionalDigits.ToString(CultureInfo.InvariantCulture);
            string text = coefficient.ToString(format, options.Culture);
            if (!trimTrailingZeros || fractionalDigits == 0)
                return text;

            string decimalSeparator = options.Culture.NumberFormat.NumberDecimalSeparator;
            int separatorIndex = text.IndexOf(decimalSeparator, StringComparison.Ordinal);
            if (separatorIndex < 0)
                return text;

            int end = text.Length;
            int fractionalStart = separatorIndex + decimalSeparator.Length;
            while (end > fractionalStart && text[end - 1] == '0')
                end--;
            if (end == fractionalStart)
                end = separatorIndex;
            return text.Substring(0, end);
        }

        private static void ValidateOptions(HugeNumberFormatOptions options)
        {
            if (!Enum.IsDefined(typeof(HugeNumberNotation), options.Notation) ||
                options.Culture == null ||
                options.SignificantDigits < 1 || options.SignificantDigits > 15 ||
                options.MinimumFractionalDigits < 0 || options.MinimumFractionalDigits > 15 ||
                options.MaximumFractionalDigits < 0 || options.MaximumFractionalDigits > 15 ||
                options.MinimumFractionalDigits > options.MaximumFractionalDigits)
            {
                throw new ArgumentException("The huge number format options are invalid.", nameof(options));
            }
        }
    }
}
