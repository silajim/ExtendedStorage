# Runtime checks

These checks use a separate game installation made from junctions and an isolated save profile. They generate a test colony and do not open or modify normal player saves.

Requirements: Windows, a .NET SDK with .NET Framework 4.8 targeting support, RimWorld 1.6.4871, Harmony, and the matching 1.6 ExtendedStorageExtended port. Build the mod before running the checks.

```powershell
.\Validate-Mods.ps1 -GamePath 'D:\Games\RimWorld\RimWorld' -ExtensionPath '..\ExtendedStorageExtended' -Mode All -DeepStorage
```

Supply `-DeepStoragePath` and `-StockpileLimitPath` when those mods are installed under different folder names. Omit `-DeepStorage` for the original storage checks without either compatibility mod. `-Mode Core` disables DLC for the generated test colony.

The compatibility regression puts 75 steel on a shelf with a five-item Stockpile Limit. It first temporarily removes the cache guard inside this isolated process and detects the original recursion at depth three, before a stack overflow. It then restores the guard and checks cache construction, the five-item limit, the Deep Storage mass limit, item preservation, cleanup after an exception, and save/reload.

The runner prints its process ID, result file, and game log. A successful result ends with `PASS`; the test game exits automatically. The original storage checks also cover capacity, item quality, menu targeting, corpse release, and save/reload.
