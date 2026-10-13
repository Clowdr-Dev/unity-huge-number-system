using System;
using System.Collections.Generic;
using UnityEngine;

using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Authoring
{
    [Serializable]
    public struct SerializedHugeNumber
    {
        [SerializeField] private double _mantissa;
        [SerializeField] private long _exponent;

        public SerializedHugeNumber(double mantissa, long exponent)
        {
            _mantissa = mantissa;
            _exponent = exponent;
        }

        public double Mantissa => _mantissa;
        public long Exponent => _exponent;

        public static SerializedHugeNumber FromHugeNumber(HugeNumber value)
            => new SerializedHugeNumber(value.Mantissa, value.Exponent);

        public HugeNumber ToHugeNumber()
        {
            if (TryToHugeNumber(out HugeNumber value, out string error))
                return value;

            throw new InvalidOperationException(error);
        }

        public bool TryToHugeNumber(out HugeNumber value, out string error)
        {
            if (HugeNumber.TryFromParts(_mantissa, _exponent, out value))
            {
                error = null;
                return true;
            }

            value = HugeNumber.Zero;
            error = FormattableString.Invariant(
                $"Mantissa '{_mantissa:G17}' and exponent '{_exponent}' must be finite and normalize within the signed 64-bit exponent range.");
            return false;
        }

        public IReadOnlyList<HugeNumberAuthoringIssue> Validate(string owner, string propertyPath)
        {
            string context = $"{owner ?? "<unknown owner>"}.{propertyPath ?? "<unknown property>"}";
            var issues = new List<HugeNumberAuthoringIssue>();
            if (!TryToHugeNumber(out HugeNumber normalized, out string error))
            {
                issues.Add(new HugeNumberAuthoringIssue(
                    HugeNumberAuthoringIssueSeverity.Error,
                    FormattableString.Invariant(
                        $"{context}: raw mantissa '{_mantissa:G17}' and exponent '{_exponent}' are invalid. {error}")));
                return issues;
            }

            double magnitude = Math.Abs(_mantissa);
            if (_mantissa != 0d && (magnitude < 1d || magnitude >= 10d))
            {
                issues.Add(new HugeNumberAuthoringIssue(
                    HugeNumberAuthoringIssueSeverity.Warning,
                    FormattableString.Invariant(
                        $"{context}: raw mantissa '{_mantissa:G17}' is not normalized to [1, 10). Normalized preview is '{normalized.ToInvariantString()}'; the runtime will normalize this value.")));
            }

            if (magnitude >= 1e15d)
            {
                issues.Add(new HugeNumberAuthoringIssue(
                    HugeNumberAuthoringIssueSeverity.Warning,
                    FormattableString.Invariant(
                        $"{context}: raw mantissa '{_mantissa:G17}' uses at least 15 significant digits; low-order authored digits exceed the reliable precision budget. Use normalized scientific notation.")));
            }

            return issues;
        }
    }
}
