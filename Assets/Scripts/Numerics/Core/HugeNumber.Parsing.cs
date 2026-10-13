using System;
using System.Globalization;
using System.Text;

namespace HugeNumberSystem.Numerics
{
    internal enum InvariantParseFailure
    {
        None,
        Format,
        Overflow,
    }

    public readonly partial struct HugeNumber
    {
        public static HugeNumber ParseInvariant(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            InvariantParseFailure failure = TryParseInvariantCore(text, out HugeNumber value);
            if (failure == InvariantParseFailure.None)
                return value;
            if (failure == InvariantParseFailure.Overflow)
                throw new OverflowException("The invariant number exponent is outside the supported range.");

            throw new FormatException("Text must be a finite invariant ordinary or scientific number.");
        }

        public static bool TryParseInvariant(string text, out HugeNumber value)
        {
            return TryParseInvariantCore(text, out value) == InvariantParseFailure.None;
        }

        public string ToInvariantString()
        {
            if (IsZero)
                return "0";

            return Mantissa.ToString("G17", CultureInfo.InvariantCulture) + "e" +
                Exponent.ToString(CultureInfo.InvariantCulture);
        }

        private static InvariantParseFailure TryParseInvariantCore(string text, out HugeNumber value)
        {
            value = Zero;
            if (text == null)
                return InvariantParseFailure.Format;

            string input = text.Trim();
            if (input.Length == 0)
                return InvariantParseFailure.Format;

            int index = 0;
            bool negative = false;
            if (input[index] == '+' || input[index] == '-')
            {
                negative = input[index] == '-';
                index++;
            }

            var digits = new StringBuilder(input.Length);
            int digitsBeforeDecimal = 0;
            bool decimalSeen = false;
            while (index < input.Length && input[index] != 'e' && input[index] != 'E')
            {
                char character = input[index];
                if (character >= '0' && character <= '9')
                {
                    digits.Append(character);
                    if (!decimalSeen)
                        digitsBeforeDecimal++;
                }
                else if (character == '.' && !decimalSeen)
                {
                    decimalSeen = true;
                }
                else
                {
                    return InvariantParseFailure.Format;
                }

                index++;
            }

            if (digits.Length == 0)
                return InvariantParseFailure.Format;

            long explicitExponent = 0L;
            if (index < input.Length)
            {
                index++;
                bool exponentNegative = false;
                if (index < input.Length && (input[index] == '+' || input[index] == '-'))
                {
                    exponentNegative = input[index] == '-';
                    index++;
                }

                int exponentStart = index;
                try
                {
                    while (index < input.Length)
                    {
                        char character = input[index];
                        if (character < '0' || character > '9')
                            return InvariantParseFailure.Format;

                        int digit = character - '0';
                        explicitExponent = exponentNegative
                            ? checked(explicitExponent * 10L - digit)
                            : checked(explicitExponent * 10L + digit);
                        index++;
                    }
                }
                catch (OverflowException)
                {
                    return InvariantParseFailure.Overflow;
                }

                if (index == exponentStart)
                    return InvariantParseFailure.Format;
            }

            int firstNonZero = 0;
            while (firstNonZero < digits.Length && digits[firstNonZero] == '0')
                firstNonZero++;
            if (firstNonZero == digits.Length)
                return InvariantParseFailure.None;

            long normalizedExponent;
            try
            {
                long decimalOffset = (long)digitsBeforeDecimal - firstNonZero - 1L;
                normalizedExponent = checked(explicitExponent + decimalOffset);
            }
            catch (OverflowException)
            {
                return InvariantParseFailure.Overflow;
            }

            var coefficient = new StringBuilder(digits.Length - firstNonZero + 2);
            if (negative)
                coefficient.Append('-');
            coefficient.Append(digits[firstNonZero]);
            if (firstNonZero + 1 < digits.Length)
            {
                coefficient.Append('.');
                for (int digitIndex = firstNonZero + 1; digitIndex < digits.Length; digitIndex++)
                    coefficient.Append(digits[digitIndex]);
            }

            if (!double.TryParse(coefficient.ToString(),
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out double normalizedMantissa) ||
                !double.IsFinite(normalizedMantissa))
            {
                return InvariantParseFailure.Format;
            }

            try
            {
                value = FromParts(normalizedMantissa, normalizedExponent);
                return InvariantParseFailure.None;
            }
            catch (OverflowException)
            {
                value = Zero;
                return InvariantParseFailure.Overflow;
            }
        }
    }
}
