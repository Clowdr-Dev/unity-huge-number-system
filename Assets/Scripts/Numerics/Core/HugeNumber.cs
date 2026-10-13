using System;

namespace HugeNumberSystem.Numerics
{
    public readonly partial struct HugeNumber : IEquatable<HugeNumber>, IComparable<HugeNumber>
    {
        public const int AdditionAlignmentDigits = 16;
        private const double Chunk = 1e308d;
        private readonly double _mantissa;
        private readonly long _exponent;

        private HugeNumber(double normalizedMantissa, long normalizedExponent, byte canonical)
        {
            _mantissa = normalizedMantissa;
            _exponent = normalizedExponent;
        }

        public double Mantissa => _mantissa;
        public long Exponent => _exponent;
        public bool IsZero => _mantissa == 0d;
        public int Sign => Math.Sign(_mantissa);

        public static readonly HugeNumber Zero = new HugeNumber(0d, 0L, canonical: 0);
        public static readonly HugeNumber One = new HugeNumber(1d, 0L, canonical: 0);
        public static readonly HugeNumber Ten = new HugeNumber(1d, 1L, canonical: 0);

        public static HugeNumber FromDouble(double value) => FromParts(value, 0L);
        public static HugeNumber FromInt64(long value) => FromParts(value, 0L);

        public static HugeNumber FromParts(double mantissa, long exponent)
        {
            if (!double.IsFinite(mantissa))
                throw new ArgumentException("Mantissa must be finite.", nameof(mantissa));
            if (mantissa == 0d)
                return Zero;

            int adjustment = checked((int)Math.Floor(Math.Log10(Math.Abs(mantissa))));
            double normalized = ScaleByPowerOfTen(mantissa, -adjustment);
            long normalizedExponent = checked(exponent + adjustment);

            while (Math.Abs(normalized) >= 10d)
            {
                normalized /= 10d;
                normalizedExponent = checked(normalizedExponent + 1L);
            }
            while (Math.Abs(normalized) < 1d)
            {
                normalized *= 10d;
                normalizedExponent = checked(normalizedExponent - 1L);
            }
            return new HugeNumber(normalized, normalizedExponent, canonical: 0);
        }

        public static bool TryFromParts(double mantissa, long exponent, out HugeNumber value)
        {
            try
            {
                value = FromParts(mantissa, exponent);
                return true;
            }
            catch (ArgumentException)
            {
                value = Zero;
                return false;
            }
            catch (OverflowException)
            {
                value = Zero;
                return false;
            }
        }

        internal static double ScaleByPowerOfTen(double value, int power)
        {
            while (power > 308)
            {
                value *= Chunk;
                power -= 308;
            }
            while (power < -308)
            {
                value /= Chunk;
                power += 308;
            }
            // Division avoids the extra rounding introduced by an approximate reciprocal.
            if (power < 0)
                return value / Math.Pow(10d, -power);

            return power == 0 ? value : value * Math.Pow(10d, power);
        }

        public bool Equals(HugeNumber other) => _mantissa == other._mantissa && _exponent == other._exponent;

        public override bool Equals(object obj) => obj is HugeNumber other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_mantissa, _exponent);

        public int CompareTo(HugeNumber other)
        {
            if (Sign != other.Sign)
                return Sign.CompareTo(other.Sign);
            if (Sign == 0)
                return 0;
            if (_exponent != other._exponent)
            {
                int exponentComparison = _exponent.CompareTo(other._exponent);
                return Sign > 0 ? exponentComparison : -exponentComparison;
            }
            return _mantissa.CompareTo(other._mantissa);
        }

        public static bool operator ==(HugeNumber left, HugeNumber right) => left.Equals(right);
        public static bool operator !=(HugeNumber left, HugeNumber right) => !left.Equals(right);
        public static bool operator <(HugeNumber left, HugeNumber right) => left.CompareTo(right) < 0;
        public static bool operator >(HugeNumber left, HugeNumber right) => left.CompareTo(right) > 0;
        public static bool operator <=(HugeNumber left, HugeNumber right) => left.CompareTo(right) <= 0;
        public static bool operator >=(HugeNumber left, HugeNumber right) => left.CompareTo(right) >= 0;
    }
}
