# Huge Number System Design

Status: Public source preview
Updated: 2026-10-14

## Context

Incremental games, persistent progression, and similar systems can produce resource totals, costs, and production rates beyond the finite range of ordinary numeric types. A normal `double` stops near `1.8e308`, and an arbitrary-precision integer preserves exact digits at a computational and storage cost that an approximate incremental economy may not need.

The library provides a numeric model that represents extremely large magnitudes, preserves a useful and predictable amount of relative precision, remains cheap enough for frequent calculations, stores culture-independent save data, and produces compact player-facing notation.

The system is a standalone numeric foundation. Consumers adopt `HugeNumber` when their required range justifies it and retain ordinary numeric types elsewhere. Gameplay, authored content, and save ownership remain with the consuming project.

## Goals

- Represent positive and negative values far beyond `double.MaxValue`.
- Preserve approximately 15 significant decimal digits at every supported magnitude for direct arithmetic, with explicit accuracy limits for logarithmic operations.
- Keep normal arithmetic allocation-free and constant-time except for documented integer exponentiation.
- Provide explicit, deterministic semantics for normalization, comparison, arithmetic, powers, zero, and invalid input.
- Keep the numeric core as plain C# with no `UnityEngine` dependency.
- Give designers a safe Unity authoring bridge with validation and a usable inspector.
- Separate numeric value, domain rules, persistence, and player-facing formatting.
- Support compact suffix, scientific, and engineering notation without storing presentation strings in game state.
- Make save data culture-independent, versioned, and safe from JSON numeric-range limitations.
- Enable efficient exponential costs, production formulas, offline progress, and bulk-purchase calculations.

## Non-goals

The first version does not implement:

- Arbitrary-precision integers or decimals
- Exact accounting of individual units at astronomical magnitudes
- Power towers, tetration, or layered exponential values
- Currency ownership, inventory mutation, transactions, or purchase commands
- A universal economy service
- Cost curves, bulk-purchase policies, or offline-progress orchestration
- Replacement of ordinary numeric types for values that fit their existing range
- Network lockstep or cross-machine deterministic simulation
- Player input that parses compact suffixes such as `1.2aa`
- General-purpose numeric floor, ceiling, truncation, or decimal-rounding helpers
- UI animation, label components, or a specific visual style

These systems may consume `HugeNumber`, but they remain separate owners.

## Guiding model

The system has four boundaries:

1. **Numeric core:** an immutable `HugeNumber` value and pure operations.
2. **Domain consumers:** resources, costs, production, affordability, and transaction rules.
3. **Infrastructure:** versioned save data and migrations.
4. **Presentation and authoring:** formatters, serialized inspector data, property drawers, and validation.

The dependency direction is one-way:

```text
Unity authoring data -> domain consumer -> HugeNumber core
save DTO <-> domain consumer -> HugeNumber core
domain value -> formatter -> player-facing text
```

The core never knows which value is a currency, whether negative balances are legal, how a purchase commits, or how a value is displayed.

## Numeric representation

`HugeNumber` is a readonly value type containing:

- A finite `double` mantissa
- A signed 64-bit base-10 exponent

Its mathematical value is:

```text
mantissa * 10^exponent
```

Every value is canonical. The invariants are:

- Zero is represented only as mantissa `0` and exponent `0`.
- `default(HugeNumber)` is therefore valid zero.
- Positive and negative floating-point zero both normalize to canonical zero, and negating zero returns canonical zero.
- A nonzero value has `1 <= abs(mantissa) < 10`.
- The mantissa is never NaN or positive or negative infinity.
- Negative values use a negative mantissa, not a separate sign field.
- Exponent arithmetic is checked and never wraps.
- Construction from every finite nonzero `double`, including subnormal values, normalizes without an intermediate overflow or underflow. Implementations scale in safe chunks or use an equivalent decomposition rather than relying on an unrepresentable power-of-ten intermediate.

Examples:

```text
0                 -> 0, 0
1,250,000         -> 1.25, 6
-0.0042           -> -4.2, -3
7.4e12000         -> 7.4, 12000
```

All constructors and operations normalize their result. Canonical representation gives exact field-based equality, stable hashing, and one save representation for any value.

The signed 64-bit exponent permits magnitudes up to approximately `10^(9e18)`, far beyond expected progression needs. If a future design approaches exponent overflow, it requires a separately specified layered-exponent type rather than silent saturation.

## Precision contract

`HugeNumber` provides floating-point relative precision, not exact decimal accounting. Direct normalized arithmetic retains approximately 15 significant decimal digits regardless of exponent. Operations that convert a large decimal exponent into `double`, specifically real powers and logarithms, follow the weaker accuracy contract in the powers and logarithms section.

Consequences include:

- Integers above the exact range of `double` are not guaranteed to retain every unit.
- Addition aligns operands whose normalized exponents differ by at most 16. At a difference of 17 or more, the smaller-magnitude operand has no observable effect.
- At `1e100`, spending `1` is below the stored precision and leaves the represented balance unchanged.
- Values that require exact cents, exact item counts, audit-grade accounting, or exact unit conservation must use an integer, fixed-point type, or arbitrary-precision representation instead.

This loss of insignificant low digits is intentional for incremental progression. Domain designs must not present those digits as exact to the player.

## Core type contract

The core exposes a compact value-oriented API with these capabilities:

- Canonical constants for zero, one, and ten
- Construction from finite integral and floating-point values
- Construction from a mantissa and exponent with normalization
- Culture-invariant `Parse` and `TryParse` for ordinary and scientific notation
- Comparison, equality, hashing, absolute value, sign, minimum, and maximum
- Addition, subtraction, multiplication, division, and negation
- Integer and real powers within their documented ranges
- Base-10 logarithm for positive values
- Explicit conversion or `TryConvert` back to finite primitive types
- Culture-independent round-trip text for diagnostics and persistence

Conversions that may lose range or meaning are never implicit. In particular, there is no implicit conversion from `HugeNumber` to `double`, and compact display strings are never accepted by the core parser. `TryToDouble` permits ordinary floating-point rounding but fails for overflow and for a nonzero value that would underflow to zero.

Integral conversion uses an explicitly named exact or truncating policy. `TryToInt64Exact` rounds the primitive approximation to a candidate integer, checks the signed range, and succeeds only when normalizing that integer reproduces the stored value exactly. This is exactness within the HugeNumber representation, not arbitrary-precision equality: normalization/rescaling noise does not reject represented integers, but distinct nearby fractions are not snapped by a tolerance. Digits lost during construction cannot be recovered, and large adjacent integers can share a representation. `TryToInt64Truncating` preserves represented integers under the same policy and otherwise truncates the primitive approximation toward zero. Values with magnitude below one truncate to zero even outside double range. Out-of-range conversions fail and return zero.

Parsing uses invariant culture and the equivalent of `NumberStyles.Float` without `AllowThousands`. It accepts leading and trailing whitespace, a leading sign, a decimal point, and an exponent, including values such as `1234`, `-0.025`, `+4.2`, and `1.234e567`. It rejects grouping separators, localized separators, suffix notation, NaN, infinity, malformed exponents, and values whose normalized exponent cannot fit in the supported range. User-facing input, if ever needed, belongs to a separate localized parser.

## Arithmetic semantics

### Addition and subtraction

Addition aligns the operand with the smaller exponent to the larger exponent before adding mantissas. The alignment cutoff is 16 decimal exponent places. If the absolute exponent difference is 17 or greater, the operand with the larger magnitude is returned unchanged because the smaller-magnitude operand is below the available precision. This rule applies to both same-sign and opposite-sign operands.

Operands with different signs use the same path, so subtraction and cancellation normalize correctly. Exact cancellation produces canonical zero. The implementation must handle near-cancellation without leaving a denormalized mantissa.

Subtraction supports negative results. A resource domain that forbids negative balances must validate affordability before mutation or expose an explicitly named clamped operation. The ordinary subtraction operator never clamps silently.

### Multiplication and division

Multiplication multiplies mantissas and adds exponents. Division divides mantissas and subtracts exponents. Results are normalized after the operation.

Division by zero fails. Exponent overflow fails. Neither case produces infinity, NaN, or a saturated value.

### Comparison and equality

Numeric comparison accounts for sign, zero, exponent, and mantissa without converting to a primitive type. Values with greater positive exponents compare larger, while ordering reverses appropriately for negative values.

Equality compares canonical mantissa and exponent exactly. Approximate equality is an explicitly named utility that takes a relative tolerance. Domain rules such as affordability use exact numeric ordering, not approximate equality.

### Powers and logarithms

The integer-power overload accepts a signed 64-bit power and supports negative and positive bases, including reciprocal results for negative powers. It uses exponentiation by squaring over the normalized representation, with checked exponent arithmetic at every multiplication. It handles the magnitude of `long.MinValue` without applying an overflowing signed absolute-value operation. Integer powers therefore preserve exponent information without converting the exponent to `double` and run in `O(log(abs(power)))` time.

The real-power overload accepts nonnegative bases. Zero follows the explicit edge cases below without entering the logarithmic calculation. A caller with a negative base and an integral power uses the integer-power overload; passing any negative base to the real-power overload fails.

Real powers of positive bases may use the base-10 logarithm of the magnitude, split the resulting exponent into integer and fractional portions, and reconstruct a normalized value. Except for power arguments zero and one, the real-power operation fails when either the source or result decimal logarithm has an absolute value at or above `2^53`, because `double` can no longer distinguish adjacent integer exponents. Within that range, its mantissa precision degrades as the result exponent grows. A useful conservative estimate is approximately `16 - log10(max(1, abs(resultExponent)))` reliable decimal digits. Consumers that require the normal approximately 15-digit contract use integer powers or direct arithmetic instead.

`Log10` accepts positive values only. Zero and negative values fail instead of returning infinity or NaN. It returns the closest available `double` approximation to `exponent + log10(mantissa)`. Above an absolute exponent of `2^53`, the result remains finite but cannot preserve unit differences between adjacent decimal exponents and must not be used for equality, affordability, or identity decisions.

The usual explicit edge cases apply:

- Any nonzero value raised to zero is one.
- Zero raised to a positive power is zero.
- Zero raised to zero is one for formula convenience and must be documented by consumers that expose this case.
- Zero raised to a negative power fails.

### Rounding

General-purpose floor, ceiling, truncation, and decimal-rounding helpers are out of scope for version 1 because no current consumer requires them. A future consumer must specify how the operation behaves when fractional or unit digits are below available precision before those helpers are added.

Rounding for display remains a formatter concern. Rounding a displayed label never mutates domain state.

## Domain usage rules

`HugeNumber` is a numeric mechanism, not a resource model. Each consuming domain owns:

- Whether values may be negative
- Minimum and maximum policies
- Affordability and transaction preconditions
- Atomic mutation and canonical events
- Rounding or quantization rules
- Save timing
- Formula ownership

Resource changes occur through domain commands. Presentation code may read a balance and submit a purchase command, but it must not subtract a cost directly.

A purchase command compares the full `HugeNumber` balance against the full cost, validates every related precondition, applies the complete transaction atomically, and emits its canonical event. It must not compare formatted strings or values converted to `double`.

Consumers may use `HugeNumber` for large cumulative balances and progression costs while retaining their existing numeric types for combat values, rates, cooldowns, and other bounded quantities.

## Formula usage

Economy consumers calculate growth analytically rather than iterating once per upgrade index, purchase, or elapsed second.

An exponential cost curve follows the conceptual form:

```text
cost(index) = baseCost * growth^index
```

The total cost of a consecutive range uses the geometric-series formula when applicable. A maximum-affordable purchase uses a closed-form estimate followed by bounded correction, or monotonic binary search when a closed form is unsafe. It never loops once per potential purchase when the count can become large.

Production over ordinary elapsed time follows:

```text
produced = ratePerSecond * elapsedSeconds
```

Offline progress uses an analytic formula or a bounded simulation chosen by the owning feature. It never simulates every missed frame or second. Time remains an injected dependency, and negative clock changes award no progress.

Curve definitions, growth factors, offline caps, and balance values are authored content. They are not constants embedded in `HugeNumber`.

## Unity authoring model

The plain C# core is not serialized directly by Unity. Unity serialization can bypass constructors and therefore cannot be trusted to preserve runtime invariants.

The authoring layer provides a serializable bridge containing mantissa and exponent fields. It:

- Converts to a normalized `HugeNumber` through the validated core factory
- Rejects a non-finite mantissa and exponent overflow
- Shows the normalized scientific value in the inspector
- Warns when authored low-order digits exceed meaningful precision
- Supports copying and pasting invariant scientific notation
- Never stores a player-facing compact suffix as source data

A custom property drawer presents an approachable mantissa and base-10 exponent editor while preserving the serialized fields. Content validation reports the owning asset, property path, invalid input, and expected constraint.

Consumers choose how to organize authored assets and definitions. The bridge works in serialized MonoBehaviour and ScriptableObject fields without a required content directory or database. Consumers own formula behavior, bases, growth rates, caps, and thresholds.

## Formatting

Formatting is implemented by a pure, stateless `HugeNumberFormatter` configured through immutable options. It does not mutate or round the source value.

Supported notation modes are:

- **Compact suffix:** ordinary values, followed by magnitude suffixes
- **Scientific:** normalized mantissa followed by `e` and the exponent
- **Engineering:** exponent constrained to a multiple of three

The default compact policy is:

- Zero displays as `0`.
- Nonzero absolute values below `1e-3` fall back to scientific notation so a representable value is never displayed as zero.
- Absolute values from `1e-3` through values below `1,000` use ordinary localized number formatting.
- For values of at least `1,000`, the magnitude group is `g = floor(exponent / 3)` using the normalized absolute value. Groups 1 through 4 use `K`, `M`, `B`, and `T`.
- A group `g >= 5` uses a generated lowercase alphabetic suffix. Let `n = g - 5` and begin with width `w = 2`. While `n >= 26^w`, subtract `26^w` from `n` and increment `w`. Encode the remaining `n` as exactly `w` base-26 digits with `a = 0` through `z = 25`, padding on the left with `a`. Power comparisons use overflow-safe integer arithmetic. This produces `aa` through `zz`, followed by `aaa`, `aab`, and onward.
- If product or localization requirements later choose a different suffix vocabulary, that mapping is supplied as formatter configuration rather than embedded in the numeric core.
- Players may select scientific notation if the settings design exposes notation preferences.

Engineering notation also uses mathematical floor division for negative exponents. For example, `5e-4` is represented as `500e-6`, not `0.5e-3`.

If the floored engineering exponent would be below the signed 64-bit exponent range, which can occur only at the two lowest supported source exponents, engineering mode falls back to scientific notation.

Formatting options specify significant digits, minimum and maximum fractional digits, notation mode, culture, and optional trailing-zero trimming. The selected culture supplies its negative sign. The default player-facing format uses three significant digits, zero to three fractional digits, and `MidpointRounding.AwayFromZero`. The third fractional digit ensures the ordinary lower boundary `1e-3` cannot round to display zero. Ordinary notation trims insignificant trailing zeros; suffixed, scientific, and engineering notation retain zeros needed to communicate the requested significant digits. The formatter calculates the fractional digits implied by the significant-digit setting, then clamps that count to the configured fractional-digit range. Fractional-digit bounds therefore take precedence when they conflict with the requested significant digits.

The formatter converts the coefficient to decimal before explicit rounding with the midpoint rule, within the approximate 15-digit precision budget. This prevents binary normalization/rescaling noise from shifting a decimal midpoint to the wrong side of a display boundary. It does not delegate boundary decisions to runtime general-format behavior. Cross-backend behavior still requires platform validation.

If configured ordinary fractional bounds would round a nonzero value to display zero, the formatter falls back to scientific notation for that value.

Rounding can cross a magnitude boundary. The formatter rounds first, then re-evaluates the group so that a value near the top of `K` becomes `1.00M` instead of `1000.00K`. A scientific or engineering carry beyond the signed exponent ceiling throws `OverflowException`; invariant diagnostic text does not perform display rounding.

Examples under the default compact policy include:

```text
0               -> 0
5e-4            -> 5.00e-4
12.345          -> 12.3
999             -> 999
1,250           -> 1.25K
9,999,000       -> 10.0M
1e12            -> 1.00T
1e15            -> 1.00aa
-4.2e18         -> -4.20ab
```

Suffix text is presentation only. Formatting output is not parsed back into domain state and is never persisted as the authoritative value.

If a UI later animates changes between huge values, it uses an exponent-aware presentation model, such as interpolation in logarithmic space for positive values. It never converts both endpoints to `double` or writes intermediate display values back to the domain.

## Persistence

Infrastructure owns a versioned save DTO. The canonical version 1 representation stores both numeric components as culture-invariant strings:

```json
{
  "version": 1,
  "mantissa": "1.2345678901234567",
  "exponent": "12000"
}
```

The version 1 wire contract formats the mantissa with `G17` and `CultureInfo.InvariantCulture`; it never relies on a runtime-selected round-trip format. The exponent uses invariant signed 64-bit parsing and is stored as a string so JavaScript, WebGL tooling, and third-party JSON readers cannot truncate it.

Loading performs these steps:

1. Validate the schema version and required fields.
2. Parse both fields with invariant culture.
3. Reject NaN, infinity, malformed data, and exponent overflow.
4. Normalize through the core factory.
5. Apply the owning save system's recovery policy if validation fails.

The save never stores compact suffix text. A migration from an older finite integer, float, or double field converts that value through the same validated factory and preserves no more precision than the old representation contained.

The `HugeNumber` core does not own save files, backup recovery, or migrations. `HugeNumberSystem.Numerics.Persistence` supplies the versioned value DTO and culture-independent conversion; the owning save infrastructure handles whole-snapshot migration and recovery.

## Error handling

The core fails fast for programmer errors and invalid arithmetic:

- NaN or infinite inputs
- Divide by zero
- Exponent overflow or underflow beyond the signed 64-bit range
- Logarithm of zero or a negative value
- Unsupported real powers of negative values
- A real power outside its documented logarithm range
- Invalid conversion to a finite primitive type

Throwing methods have matching `Try` forms where invalid external data is expected, especially parsing, save loading, and Unity authoring conversion. Normal arithmetic never silently clamps to zero, saturates to a maximum, or creates a non-finite sentinel.

Validation errors from authored content and saves include the original input and the violated constraint. Player-facing systems translate domain failures into their own presentation and do not display raw exceptions.

## Assembly boundaries

- `HugeNumberSystem.Numerics.Core`: `HugeNumber`, parsing, arithmetic, comparisons, primitive conversions, and round-trip representation; no `UnityEngine` reference.
- `HugeNumberSystem.Numerics.Authoring`: serialized Unity bridge, conversion, content validation, and property drawer runtime data.
- `HugeNumberSystem.Numerics.Editor`: custom inspector, normalized preview, copy and paste support, and authoring diagnostics.
- `HugeNumberSystem.Numerics.Persistence`: versioned save DTOs and culture-independent conversion between DTOs and `HugeNumber`; plain C# with no save-file or platform-storage orchestration.
- `HugeNumberSystem.Numerics.Presentation`: formatter policies and suffix providers; pure C# where practical so formatting can be tested without Play Mode.
- Matching core, authoring, persistence, and presentation test assemblies.

Economy curves, resources, purchases, offline progress, save-file ownership, backup recovery, migrations between whole snapshot versions, and save timing live in their owning feature or infrastructure assemblies rather than `HugeNumberSystem.Numerics`.

## Performance contract

`HugeNumber` is a small readonly struct. Normal arithmetic:

- Runs in constant time for construction, normalization, comparison, addition, subtraction, multiplication, division, and logarithm
- Runs in `O(log(abs(power)))` time for integer exponentiation, with at most 64 squaring steps for a signed 64-bit power
- Allocates no managed memory
- Does not construct strings
- Does not use reflection, `BigInteger`, or Unity APIs
- Is safe to pass by value at its expected size

Parsing, formatting, diagnostics, and inspector tooling may allocate by design. UI consumers update text only when the formatted result changes, avoiding a new string every frame.

The immutable core contains no global state and is safe for concurrent reads and independent calculations. Domain owners remain responsible for synchronization and main-thread Unity access.

## Diagnostics

Diagnostic output can expose:

- Canonical mantissa and exponent
- Invariant scientific notation
- Selected display notation and suffix group
- Whether a primitive conversion would overflow or underflow
- Whether an arithmetic result discarded an operand below available precision, when using an opt-in diagnostic operation

Hot-path operators do not log. Editor and development tooling may use explicitly requested diagnostic results to explain surprising economy calculations.

## Verification strategy

### Core EditMode tests

- Default construction produces canonical zero.
- Positive zero, negative zero, negated zero, and the smallest, largest, and representative finite subnormal `double` values normalize without invalid intermediates.
- Every constructor and operation preserves normalization invariants.
- Inputs that share the same source representation, plus values whose normalized mantissas are exactly representable, normalize to exact equality and equal hashes.
- Positive, negative, and zero comparisons are numerically ordered.
- Addition and subtraction cover equal exponents, the alignment boundary at exponent differences 16 and 17, opposite signs, exact cancellation, and near-cancellation.
- Multiplication and division cover signs, normalization boundaries, reciprocal values, divide by zero, and checked exponent limits.
- Integer powers cover documented zero and negative-base behavior, `long.MinValue`, large exponents, exact powers of ten, and composition identities within documented relative tolerances.
- Real powers cover zero and one identities, negative-base rejection, the `2^53` logarithm boundary, and precision degradation at representative large exponents.
- Parsing enforces the specified invariant `NumberStyles` contract, and `G17` round-trip formatting survives all supported exponent ranges and system cultures.
- NaN, infinity, malformed values, and overflowing exponents are rejected.
- Randomized operations agree with ordinary `double` within a documented relative tolerance whenever the expected result fits safely in `double`.
- Normal arithmetic allocates no managed memory.

### Presentation EditMode tests

- Compact, scientific, and engineering notation produce stable results.
- Default suffix boundaries map to `K`, `M`, `B`, `T`, `aa`, `ab`, `zz`, `aaa`, and later generated groups correctly.
- Away-from-zero midpoint rounding carries into the next suffix group identically across supported runtimes.
- Negative values, zero, the `1e-3` scientific fallback, negative-exponent engineering notation, trailing-zero policy, and significant-digit versus fractional-digit precedence are correct.
- Localized decimal and grouping separators never change magnitude or suffix selection.
- Formatting does not mutate the source value.

### Authoring and persistence tests

- Serialized authoring values convert through validation and normalization.
- Invalid mantissas and exponents produce actionable asset diagnostics.
- Version 1 save DTOs round-trip exactly across supported cultures.
- Future platform validation: representative IL2CPP player checks round-trip the version 1 `G17` DTO on mobile and WebGL release paths. The current verification runs Editor EditMode and PlayMode only.
- Old finite numeric fields migrate to the equivalent canonical value.
- Invalid save fields follow the owning save system's recovery path rather than entering domain state.

### Consumer tests

Any feature adopting `HugeNumber` tests its externally visible behavior, including:

- Affordability at exact, insufficient, and widely separated magnitudes
- Atomic resource deductions and awards
- Exponential cost monotonicity
- Geometric bulk cost against repeated calculation at small reference counts
- Maximum-affordable results at formula boundaries
- Offline production caps and negative clock changes

The numeric core and formatter require no Play Mode tests. Any integration that changes visible UI must add the required end-to-end journey coverage, capture screenshots of every affected screen or section, and visually verify formatting, text fit, safe areas, touch targets, and notation changes.

## Acceptance criteria

The design is successfully implemented when:

- `HugeNumber` represents normalized values with a finite mantissa and signed 64-bit decimal exponent.
- Zero, signs, comparison, equality, arithmetic, powers, logarithms, conversion, and invalid cases follow this specification.
- Core operations remain allocation-free and compile without a `UnityEngine` reference.
- A value far beyond `double.MaxValue` can be calculated, compared, saved, loaded, and formatted without conversion through infinity.
- Unity-authored values convert through validated bridge data without bypassing core invariants.
- Versioned save data round-trips independently of culture and does not rely on JSON's numeric precision.
- Compact formatting handles suffix generation and rounding across magnitude boundaries, while scientific and engineering alternatives remain available.
- Domain consumers retain ownership of negativity, affordability, transactions, formulas, events, and save timing.
- Consumers retain ordinary numeric types for values whose range does not require HugeNumber.
- Core, presentation, authoring, and persistence tests are green. When a feature first adopts `HugeNumber`, that feature's consumer tests are green before the adoption merges.
- Any player-facing integration includes a visually inspected end-to-end screenshot check.

## Future extension point

If progression ever approaches the signed 64-bit exponent limit or intentionally introduces values such as `10^(10^x)`, a new layered magnitude type may be added above `HugeNumber`. That design must define its own arithmetic and formatting semantics. The first version does not reserve hidden tier fields or complicate the common representation in anticipation of that possibility.
