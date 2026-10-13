# Contributing

Use Unity `6000.5.4f1` and the Test Framework version pinned in `Packages/manifest.json`. Open the repository root as the reference Unity project. Close the Editor before running `scripts/verify.ps1`; see [verification](docs/verification.md).

For a numeric fix, include a regression test that fails before the fix. Check the [design contract](docs/huge-number-system-design.md) when changing normalization, precision, parsing, conversion, or error behavior. Test the public behavior with literal expected values, including signs and relevant boundaries.

Keep core, presentation, and persistence independent of Unity APIs. Unity serialization belongs in Authoring, and UnityEditor APIs belong in the Editor-only assembly. Preserve asset `.meta` files and assembly boundaries. Avoid committing generated `Library`, logs, verification artifacts, or local editor settings.

Update the changelog and relevant user documentation when public behavior changes. Run both verification suites before submitting a pull request. Include the command used, the passed/failed counts, and any validation limits in the pull request description.

Bug reports should include the Unity version, runtime/backend, smallest reproducible input, actual and expected results, and relevant exceptions or test output. Exclude account tokens and other private information from logs.

## Contribution licensing

Contributions must be your own work or supplied with compatible permissions. Contributions are distributed under this repository's PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE) and [NOTICE](NOTICE). Do not assume third-party code can be copied here merely because it is publicly visible. Relicensing contributed code in a later release may require the contributor's permission.
