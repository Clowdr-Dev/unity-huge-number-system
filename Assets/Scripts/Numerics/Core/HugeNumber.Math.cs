using System;

namespace HugeNumberSystem.Numerics
{
    public readonly partial struct HugeNumber
    {
        private const double MaxExactExponentInDouble = 9007199254740992d;

        public static HugeNumber Pow(HugeNumber value, long power)
        {
            if (power == 0L)
                return One;
            if (value.IsZero)
            {
                if (power < 0L)
                    throw new DivideByZeroException("Zero cannot be raised to a negative power.");

                return Zero;
            }

            bool reciprocal = power < 0L;
            ulong remaining = reciprocal
                ? (ulong)(-(power + 1L)) + 1UL
                : (ulong)power;
            HugeNumber factor = reciprocal ? One / value : value;
            HugeNumber result = One;
            while (remaining != 0UL)
            {
                if ((remaining & 1UL) != 0UL)
                    result *= factor;

                remaining >>= 1;
                if (remaining != 0UL)
                    factor *= factor;
            }

            return result;
        }

        public static HugeNumber Pow(HugeNumber value, double power)
        {
            if (!double.IsFinite(power))
                throw new ArgumentOutOfRangeException(nameof(power), "Power must be finite.");
            if (value.Sign < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Real powers require a nonnegative base.");
            if (power == 0d)
                return One;
            if (value.IsZero)
            {
                if (power < 0d)
                    throw new DivideByZeroException("Zero cannot be raised to a negative power.");

                return Zero;
            }
            if (power == 1d)
                return value;

            double sourceLog = value.Log10();
            if (Math.Abs(sourceLog) >= MaxExactExponentInDouble)
                throw new ArgumentOutOfRangeException(nameof(value), "Base logarithm exceeds exact double exponent resolution.");

            double resultLog = sourceLog * power;
            if (Math.Abs(resultLog) >= MaxExactExponentInDouble)
                throw new ArgumentOutOfRangeException(nameof(power), "Result logarithm exceeds exact double exponent resolution.");

            long resultExponent = (long)Math.Floor(resultLog);
            double resultMantissa = Math.Pow(10d, resultLog - resultExponent);
            return FromParts(resultMantissa, resultExponent);
        }

        public double Log10()
        {
            if (Sign <= 0)
                throw new ArgumentOutOfRangeException(nameof(HugeNumber), "Logarithm requires a positive value.");

            return Exponent + Math.Log10(Mantissa);
        }

        public bool ApproximatelyEquals(HugeNumber other, double relativeTolerance)
        {
            if (!double.IsFinite(relativeTolerance) || relativeTolerance < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(relativeTolerance),
                    "Relative tolerance must be finite and nonnegative.");
            }
            if (Equals(other))
                return true;
            if (IsZero || other.IsZero)
                return false;

            HugeNumber difference = Abs(this - other);
            HugeNumber scale = Max(Abs(this), Abs(other));
            return difference <= scale * FromDouble(relativeTolerance);
        }
    }
}
