using System;

namespace HugeNumberSystem.Numerics
{
    public readonly partial struct HugeNumber
    {
        public static HugeNumber operator +(HugeNumber left, HugeNumber right)
        {
            return AddCore(left, right, out _);
        }

        public static HugeNumber operator -(HugeNumber left, HugeNumber right)
        {
            return AddCore(left, -right, out _);
        }

        public static HugeNumber operator -(HugeNumber value)
        {
            return value.IsZero ? Zero : FromParts(-value.Mantissa, value.Exponent);
        }

        public static HugeNumber operator *(HugeNumber left, HugeNumber right)
        {
            if (left.IsZero || right.IsZero)
                return Zero;

            long exponent = checked(left.Exponent + right.Exponent);
            return FromParts(left.Mantissa * right.Mantissa, exponent);
        }

        public static HugeNumber operator /(HugeNumber left, HugeNumber right)
        {
            if (right.IsZero)
                throw new DivideByZeroException();
            if (left.IsZero)
                return Zero;

            long exponent = checked(left.Exponent - right.Exponent);
            return FromParts(left.Mantissa / right.Mantissa, exponent);
        }

        public static HugeNumber Abs(HugeNumber value)
        {
            return value.Sign < 0 ? -value : value;
        }

        public static HugeNumber Min(HugeNumber left, HugeNumber right)
        {
            return left.CompareTo(right) <= 0 ? left : right;
        }

        public static HugeNumber Max(HugeNumber left, HugeNumber right)
        {
            return left.CompareTo(right) >= 0 ? left : right;
        }

        public static HugeNumberAdditionResult AddWithDiagnostics(HugeNumber left, HugeNumber right)
        {
            HugeNumber value = AddCore(left, right, out HugeNumberAdditionDisposition disposition);
            return new HugeNumberAdditionResult(value, disposition);
        }

        private static HugeNumber AddCore(HugeNumber left, HugeNumber right,
            out HugeNumberAdditionDisposition disposition)
        {
            if (left.IsZero)
            {
                disposition = HugeNumberAdditionDisposition.RightDominated;
                return right;
            }
            if (right.IsZero)
            {
                disposition = HugeNumberAdditionDisposition.LeftDominated;
                return left;
            }
            if (left.Exponent == right.Exponent)
            {
                disposition = HugeNumberAdditionDisposition.Combined;
                return FromParts(left.Mantissa + right.Mantissa, left.Exponent);
            }

            bool leftHasHigherExponent = left.Exponent > right.Exponent;
            HugeNumber high = leftHasHigherExponent ? left : right;
            HugeNumber low = leftHasHigherExponent ? right : left;
            if (high.Exponent >= long.MinValue + AdditionAlignmentDigits + 1L &&
                low.Exponent <= high.Exponent - AdditionAlignmentDigits - 1L)
            {
                disposition = leftHasHigherExponent
                    ? HugeNumberAdditionDisposition.LeftDominated
                    : HugeNumberAdditionDisposition.RightDominated;
                return high;
            }

            int difference = checked((int)(high.Exponent - low.Exponent));
            double aligned = low.Mantissa * Math.Pow(10d, -difference);
            disposition = HugeNumberAdditionDisposition.Combined;
            return FromParts(high.Mantissa + aligned, high.Exponent);
        }
    }
}
