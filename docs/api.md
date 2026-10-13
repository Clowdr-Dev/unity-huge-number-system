# API reference

The public API uses `HugeNumberSystem.Numerics` and its subnamespaces. All operations use approximate floating-point precision. See the [design contract](huge-number-system-design.md) for invariants and the [usage guide](usage.md) for examples.

## Core: HugeNumberSystem.Numerics

`HugeNumber` is a readonly struct implementing `IEquatable<HugeNumber>` and `IComparable<HugeNumber>`.

| Member | Contract |
| --- | --- |
| `Zero`, `One`, `Ten` | Canonical constants; `default(HugeNumber)` is zero |
| `Mantissa`, `Exponent`, `Sign`, `IsZero` | Read-only normalized components and sign information |
| `FromDouble(double)`, `FromInt64(long)` | Normalize finite input; retain the type's approximate precision |
| `FromParts(double, long)` | Normalize mantissa/exponent; rejects non-finite mantissas and exponent overflow |
| `TryFromParts(double, long, out HugeNumber)` | Returns false and zero output for invalid input |
| `ParseInvariant(string)` | Parses ordinary/scientific text; rejects grouping, localized decimals, suffixes, and non-finite values |
| `TryParseInvariant(string, out HugeNumber)` | Returns false and zero output for invalid text or exponent overflow |
| `ToInvariantString()` | Invariant `G17` mantissa and signed exponent; round-trips canonical fields throughout the supported exponent range |
| `+`, binary `-`, unary `-`, `*`, `/` | Normalize results; checked exponent arithmetic; division by zero throws |
| `==`, `!=`, `<`, `>`, `<=`, `>=`, `CompareTo`, `Equals`, `GetHashCode` | Field-based exact equality and numeric ordering without primitive conversion |
| `Abs`, `Min`, `Max` | Static magnitude/order helpers |
| `Pow(HugeNumber, long)` | Integer exponentiation by squaring, including negative powers and negative bases |
| `Pow(HugeNumber, double)` | Nonnegative bases; reduced accuracy for large exponents; source/result logarithms must be below `2^53` in absolute value except power identities zero and one |
| `Log10()` | Positive values only; returns a finite double approximation with reduced resolution at large exponents |
| `ApproximatelyEquals(HugeNumber, double)` | Explicit nonnegative finite relative tolerance; domain comparisons should use ordering |
| `AddWithDiagnostics(HugeNumber, HugeNumber)` | Returns value and `Combined`, `LeftDominated`, or `RightDominated` disposition |
| `TryToDouble(out double)` | False and zero for overflow or nonzero underflow to zero; otherwise ordinary primitive rounding |
| `ToDoubleChecked()` | Throws `OverflowException` when `TryToDouble` would fail |
| `TryToInt64Exact(out long)` | Candidate must normalize back to the identical stored value; false and zero if it cannot |
| `TryToInt64Truncating(out long)` | Preserves represented integers, otherwise truncates toward zero; magnitude below one becomes zero; false and zero outside conversion range |

`AdditionAlignmentDigits` is 16. At exponent differences of 17 or more, addition/subtraction returns the larger-magnitude operand unchanged. Integer conversion cannot recover digits already lost during construction, including adjacent integers that share a representation at large magnitudes.

`ParseInvariant` throws `ArgumentNullException` for null, `FormatException` for malformed text, and `OverflowException` for an unsupported exponent. Construction rejects non-finite mantissas with `ArgumentException`. Invalid power/log domains and tolerances throw `ArgumentOutOfRangeException`; zero denominators and negative powers of zero throw `DivideByZeroException`. Arithmetic exponent overflow throws rather than clamping.

## Presentation: HugeNumberSystem.Numerics.Presentation

| Type/member | Contract |
| --- | --- |
| `HugeNumberFormatter(IHugeNumberSuffixProvider = null)` | Stateless calculations; uses the default suffix provider when omitted |
| `Format(HugeNumber, HugeNumberFormatOptions)` | Formats display text without mutating the value |
| `HugeNumberFormatOptions` | Immutable notation/culture, significant digits (1–15), fractional bounds (0–15), and ordinary trailing-zero policy |
| `HugeNumberNotation` | `Compact`, `Scientific`, `Engineering` |
| `IHugeNumberSuffixProvider.TryGetSuffix(long, out string)` | Maps a magnitude group to suffix text; false requests scientific fallback |
| `DefaultHugeNumberSuffixProvider.Instance` | `K`, `M`, `B`, `T`, generated alphabetic groups |

Construct options explicitly: `default(HugeNumberFormatOptions)` is invalid. Culture is required. Fractional bounds must be ordered and take precedence over the significant digit request. Rounding uses decimal coefficients and `MidpointRounding.AwayFromZero`, within the approximate 15-digit precision budget. A carry beyond the signed exponent ceiling throws `OverflowException` in exponent notation.

## Persistence: HugeNumberSystem.Numerics.Persistence

| Type/member | Contract |
| --- | --- |
| `HugeNumberSaveData` | Serializable DTO: `int version`, `string mantissa`, `string exponent` |
| `HugeNumberPersistence.CurrentVersion` | Version 1 |
| `Write(HugeNumber)` | Produces invariant `G17` mantissa and invariant exponent strings |
| `TryRead(HugeNumberSaveData, out HugeNumber, out string)` | Validates version and fields, normalizes, and returns an error on failure |
| `MigrateLegacyDouble(double)` | Converts a finite legacy primitive to a version-one DTO |

Successful `TryRead` returns a null error. Failure returns zero and a diagnostic. The caller owns recovery and whole-save migration.

## Authoring and Editor

`HugeNumberSystem.Numerics.Authoring.SerializedHugeNumber` holds raw Unity-serialized mantissa/exponent fields. Its constructor preserves raw authoring input; conversion performs validation and normalization.

| Member | Contract |
| --- | --- |
| `Mantissa`, `Exponent` | Raw authored components |
| `FromHugeNumber(HugeNumber)` | Copies canonical components into the bridge |
| `ToHugeNumber()` | Validates/normalizes or throws `InvalidOperationException` |
| `TryToHugeNumber(out HugeNumber, out string)` | Normalizes or returns zero plus an error |
| `Validate(string owner, string propertyPath)` | Returns contextual `HugeNumberAuthoringIssue` warnings/errors |

`HugeNumberSystem.Numerics.Editor.HugeNumberPropertyDrawer` draws bridge fields in the Inspector. `HugeNumberEditorClipboard.Copy` emits invariant text; `TryPaste` parses/normalizes ordinary or scientific invariant input and supplies an error on failure. Editor types must not be referenced from runtime consumer assemblies.
