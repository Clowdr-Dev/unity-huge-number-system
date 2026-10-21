# Usage guide

## Integrate with an existing Unity project

Use Unity `6000.5.4f1` for the currently verified configuration. Open this repository as its own Unity project to explore or verify the source.

For another project, copy `Assets/Scripts/Numerics` and its accompanying `.meta` file into your `Assets/Scripts` folder, preserving the contained `.meta` files. The five production folders can also be copied without `Tests` and `Tests.meta`. Include tests only when you want them in the consuming project, and install Test Framework `1.7.0` if you do.

When your consumer has an `.asmdef`, reference the assemblies it uses:

| Assembly | Use |
| --- | --- |
| `HugeNumberSystem.Numerics.Core` | HugeNumber values and math |
| `HugeNumberSystem.Numerics.Presentation` | Player-facing formatting |
| `HugeNumberSystem.Numerics.Persistence` | Save DTO conversion |
| `HugeNumberSystem.Numerics.Authoring` | Serialized Inspector fields |
| `HugeNumberSystem.Numerics.Editor` | Editor tools; reference only from Editor assemblies |

Core, Presentation, and Persistence do not reference Unity. Authoring uses Unity serialization; Editor is restricted to the Editor platform. This repository is not yet an installable UPM package.

## Construct and calculate

```csharp
using HugeNumberSystem.Numerics;

HugeNumber baseCost = HugeNumber.FromInt64(100);
HugeNumber growth = HugeNumber.FromParts(1.15d, 0L);
HugeNumber upgradeCost = baseCost * HugeNumber.Pow(growth, 25L);
HugeNumber balance = HugeNumber.ParseInvariant("1.25e12000");

bool affordable = balance >= upgradeCost;
HugeNumber doubled = balance * HugeNumber.FromInt64(2);
string diagnostic = doubled.ToInvariantString(); // "2.5e12000"
```

The represented value is `Mantissa * 10^Exponent`. Zero is always `(0, 0)`; nonzero mantissas satisfy `1 <= abs(mantissa) < 10`. `FromParts` accepts unnormalized input and normalizes it. Non-finite mantissas and out-of-range exponents fail.

Use `25L` for the integer power overload. It supports negative bases and reciprocal powers. The `double` power overload uses logarithms, accepts nonnegative bases, and loses precision with large exponents. Both overloads define `0^0` as one and reject zero raised to a negative power.

Compare full values for affordability. Consumers own validation and atomic resource changes; the library does not implement purchase commands or negative-balance policy.

## Precision and conversions

```csharp
using HugeNumberSystem.Numerics;

HugeNumber number = HugeNumber.FromInt64(116);
bool isInteger = number.TryToInt64Exact(out long exact);        // true, 116
bool canTruncate = number.TryToInt64Truncating(out long units); // true, 116

HugeNumber fraction = HugeNumber.ParseInvariant("-42.9");
bool fractionalExact = fraction.TryToInt64Exact(out _);         // false
fraction.TryToInt64Truncating(out long truncated);              // -42

HugeNumber huge = HugeNumber.FromParts(1d, 12000L);
bool fitsDouble = huge.TryToDouble(out _);                      // false
```

Exact integer conversion means the returned integer normalizes back to precisely the same stored mantissa and exponent. It compensates for primitive rescaling noise through that equality check, without a relative tolerance that would round nearby distinct fractions into integers. Truncating conversion preserves such represented integers; otherwise it truncates the primitive approximation toward zero. Any value with magnitude below one truncates to zero, including values outside `double` range.

Approximately 15 significant digits are retained, not arbitrary integer precision. At large magnitudes, many adjacent integers can share one representation. Digits lost at construction cannot be recovered, and values near the signed 64-bit boundaries may round out of conversion range. Use a primitive integer or an arbitrary-precision type when exact unit conservation matters.

`TryToDouble` allows ordinary rounding but rejects infinity and underflow of a nonzero value to zero. `ToDoubleChecked` throws in those cases. Use `ToInvariantString` explicitly for diagnostics; there is no custom display `ToString` override.

## Format for display

```csharp
using System.Globalization;
using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Presentation;

var formatter = new HugeNumberFormatter();
var compact = new HugeNumberFormatOptions(
    HugeNumberNotation.Compact, CultureInfo.InvariantCulture);
var engineering = new HugeNumberFormatOptions(
    HugeNumberNotation.Engineering, CultureInfo.InvariantCulture);
var localized = new HugeNumberFormatOptions(
    HugeNumberNotation.Compact, CultureInfo.GetCultureInfo("de-DE"));

string compactLabel = formatter.Format(HugeNumber.FromInt64(1250), compact); // "1.25K"
string smallLabel = formatter.Format(HugeNumber.FromParts(5d, -4L), engineering); // "500e-6"
string localizedLabel = formatter.Format(HugeNumber.FromInt64(1250), localized); // "1,25K"
```

Defaults are three significant digits, zero to three fractional digits, and decimal midpoint rounding away from zero. Fractional digit bounds take precedence over requested significant digits. Ordinary notation trims trailing zeros by default; suffixed and exponent notation retain significant zeros. Compact suffixes progress through `K`, `M`, `B`, `T`, `aa` to `zz`, then `aaa` and onward. Supply an `IHugeNumberSuffixProvider` to the formatter constructor to customize suffixes.

Small nonzero values use scientific fallback when ordinary formatting would show zero. Display rounding re-evaluates the magnitude group, so `999.5` becomes `1.00K`. Formatting does not change the source. At the absolute exponent ceiling, a scientific or engineering rounding carry can throw `OverflowException`; use invariant text when exact diagnostic output is required.

## Save and restore

```csharp
using System;
using UnityEngine;
using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Persistence;

HugeNumber original = HugeNumber.ParseInvariant("1.25e12000");
HugeNumberSaveData data = HugeNumberPersistence.Write(original);
string json = JsonUtility.ToJson(data);
HugeNumberSaveData loadedData = JsonUtility.FromJson<HugeNumberSaveData>(json);

if (!HugeNumberPersistence.TryRead(loadedData, out HugeNumber restored, out string error))
    throw new FormatException(error); // Choose your save owner's recovery policy here.
```

Version-one JSON stores both components as strings:

```json
{"version":1,"mantissa":"1.25","exponent":"12000"}
```

The mantissa uses invariant `G17`; the exponent is an invariant signed 64-bit integer string. Keeping both as strings avoids JSON tooling's numeric range and precision limits. Preserve all three fields. `TryRead` rejects missing data, unsupported versions, invalid fields, and normalization overflow, returning an actionable error and zero output. `MigrateLegacyDouble` converts an old finite double through the same factory; it does not migrate an entire save snapshot.

Save files, backups, recovery, and save timing belong to the consuming project. Never persist compact display strings as authoritative values.

## Author in the Inspector

```csharp
using UnityEngine;
using HugeNumberSystem.Numerics;
using HugeNumberSystem.Numerics.Authoring;

public sealed class HugeNumberExample : MonoBehaviour
{
    [SerializeField] private SerializedHugeNumber startingBalance =
        new SerializedHugeNumber(1.25d, 12000L);

    public HugeNumber StartingBalance => startingBalance.ToHugeNumber();
}
```

Put the class in `HugeNumberExample.cs` and attach it to a GameObject. The drawer shows mantissa and exponent fields, a normalized preview, validation messages, and Copy/Paste buttons. Paste invariant ordinary or scientific text, such as `1.25e12000`; compact suffixes are rejected.

Use `SerializedHugeNumber` for Unity fields because Unity serialization bypasses constructors and does not serialize the immutable runtime value directly. `TryToHugeNumber` returns validation errors without throwing; `ToHugeNumber` throws for invalid authored data. `Validate(owner, propertyPath)` supplies contextual warnings and errors for consumer validation tools. Consumer assets and their organization are your project's choice.
