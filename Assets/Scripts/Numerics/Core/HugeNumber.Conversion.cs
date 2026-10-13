using System;

namespace HugeNumberSystem.Numerics
{
    public readonly partial struct HugeNumber
    {
        public bool TryToDouble(out double value)
        {
            value = 0d;
            if (IsZero)
                return true;
            if (Exponent > 308L || Exponent < -324L)
                return false;

            double converted = ScaleByPowerOfTen(Mantissa, checked((int)Exponent));
            if (!double.IsFinite(converted) || converted == 0d)
                return false;

            value = converted;
            return true;
        }

        public double ToDoubleChecked()
        {
            if (!TryToDouble(out double value))
                throw new OverflowException("The huge number cannot be represented as a nonzero finite double.");

            return value;
        }

        /// <summary>
        /// Converts an integer when normalizing the candidate reproduces this value exactly.
        /// </summary>
        /// <remarks>
        /// This tests equality in the HugeNumber representation, not arbitrary-precision integer equality.
        /// Low-order integer digits already lost during construction cannot be recovered.
        /// </remarks>
        public bool TryToInt64Exact(out long value)
        {
            value = 0L;
            if (!TryToDouble(out double converted))
                return false;

            double rounded = Math.Round(converted);
            if (rounded < long.MinValue || rounded >= 9223372036854775808d)
                return false;

            long candidate = (long)rounded;
            if (FromInt64(candidate) != this)
                return false;

            value = candidate;
            return true;
        }

        /// <summary>
        /// Preserves represented integers and otherwise truncates toward zero, failing outside Int64 range.
        /// </summary>
        public bool TryToInt64Truncating(out long value)
        {
            value = 0L;
            if (Exponent < 0L)
                return true;
            if (TryToInt64Exact(out value))
                return true;

            if (!TryToDouble(out double converted) ||
                converted < long.MinValue ||
                converted >= 9223372036854775808d)
            {
                return false;
            }

            value = (long)Math.Truncate(converted);
            return true;
        }
    }
}
