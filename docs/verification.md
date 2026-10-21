# Verification

## Run the complete suite

Install Unity `6000.5.4f1` through Unity Hub. The project pins Test Framework `1.7.0` in its package manifest. Open the project once if the installation requires activation, then close this project in the Editor before starting batch verification.

From the repository root on Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
```

With PowerShell 7, including on macOS or Linux:

```powershell
pwsh -NoProfile -File scripts/verify.ps1 -UnityPath '/path/to/the/Unity/executable'
```

The script discovers the project's pinned Editor in the default Unity Hub location on Windows and macOS. Set `UNITY_EDITOR_PATH` or supply `-UnityPath` for a custom location; the explicit parameter takes precedence. Use the pinned version when checking this release.

The default run executes EditMode filtered to all `HugeNumberSystem.Numerics` tests, then PlayMode filtered to `HugeNumberSystem.Numerics.Player` persistence tests. Unity is launched with `-batchmode -nographics -runTests`. The script waits for each process before starting the next. It omits `-quit` because the Test Framework controls completion.

## Options

```powershell
# Run only one suite during development.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 -TestPlatform EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 -TestPlatform PlayMode

# Override the Editor, per-suite timeout, or artifact destination.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 `
  -UnityPath 'D:\Unity Editors\6000.5.4f1\Editor\Unity.exe' `
  -TimeoutMinutes 30 -OutputDirectory 'artifacts/verification'
```

`TimeoutMinutes` defaults to 15 per suite. Relative output directories are resolved against the repository root, even when the script is called from another working directory. Script paths themselves can also be absolute.

## Results and failure handling

Each invocation creates a new timestamped directory under `artifacts/verification/`, containing a log and NUnit XML file per executed suite plus `summary.json`. Fresh directories prevent an old passing result from satisfying a new run. Artifact directories are ignored by Git.

Success requires a zero Unity exit code, a `Passed` test-run result, at least one test, and all reported tests passed. A failed, skipped, inconclusive, empty, missing, or malformed run returns exit code 1. For a timeout, the script stops the Unity process it launched and reports the log path. A successful run returns exit code 0.

When running this in another script or CI, invoke a separate PowerShell process and check its exit code. Unity installation and license activation remain prerequisites; this repository does not configure hosted CI credentials.

If verification fails:

- Check the current run's XML for failed assertions and its log for compile or import errors.
- Close this project in any interactive Editor before launching batch tests. Other projects can stay open.
- Check that the chosen executable exists, matches the pinned version, and has a valid Unity license.
- Allow time and network access for package restoration on a fresh clone.

You can also use **Window > General > Test Runner** in the Editor to run EditMode and PlayMode tests. Use the script for a reproducible complete check and saved results.

## Scope

The suite covers normalization, exponent limits, comparisons, arithmetic, randomized double-reference checks, allocation-free hot paths, powers, parsing, primitive conversion, culture handling, formatting boundaries, serialized authoring, and version-one JSON persistence.

The PlayMode tests run inside the Editor and are compatible with player test assemblies. They do not constitute an IL2CPP or device build test. This release has not established mobile, WebGL, other Unity versions, or comprehensive visual coverage of Inspector multi-object and prefab workflows. Add those checks when supporting those targets or preparing the later Unity distribution. The verification script has been exercised on Windows PowerShell 5.1; its macOS/Linux paths require validation on those hosts.

## Public publishing check — 2026-10-21

The publishing preparation passed **108 EditMode tests and 2 PlayMode tests**, with zero failures, skips, or inconclusive results, on Unity **6000.5.4f1** and Windows. The suites ran through `scripts/verify.ps1` in an isolated source copy; its showcase source and assembly definition were checked against the publishing branch. The temporary Pipeline capture package was removed before these tests and is not part of the public dependency manifest.

The included **Huge Number System > Showcase** window was inspected live. Its screenshot shows `1.25e12000`, an affordable cost of `5e11999`, a remaining value of `7.5e11999`, notation formatting, a matching version-one JSON save round trip, and the actual serialized property drawer. The Editor console reported no errors during capture. The screenshot is included in the [README](../README.md#explore-in-unity).
