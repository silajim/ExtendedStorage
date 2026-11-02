> Work in progress: this RimWorld 1.6 port has a reported unresolved bug. It is published for development and is not ready for an upstream pull request or release.

# RimWorld 1.6 local ports

Updated local copies of emipa606/ExtendedStorage and emipa606/ExtendedStorageExtended.
Upstream snapshots: ExtendedStorage `344412d`; ExtendedStorageExtended `05b9101`.

## Install

Extract `artifacts/ExtendedStorage-1.6.zip` and `artifacts/ExtendedStorageExtended-1.6.zip` into your game's `Mods` folder. Each archive contains one top-level mod folder.
Enable in this order: Harmony, Extended Storage, Extended Storage Extended. Core and any enabled DLC also load before Extended Storage.
Disable other copies of the same mods, since the original package IDs are retained. These are local ports of the archived projects, not official upstream releases.

The working mod folders in this workspace are also installable directly. No game install, active player configuration, or existing player save was modified during validation; tests used a separate profile in `.validation`.

## Changes

- Added complete `1.6` content folders and rebuilt both DLLs for RimWorld 1.6.4871 / .NET Framework 4.8.
- Updated supported versions and mod-version metadata to 1.6.0. Original 1.5 and earlier content and binaries remain unchanged.
- Changed the building tick override to the protected 1.6 API.
- Replaced the removed `FloatMenuMakerMap.AddHumanlikeOrders` hook with a `FloatMenuContext` hook. Stored apparel and weapons are passed to the normal 1.6 menu providers, preserving their wear/equip restrictions.
- Replaced the fragile spawn IL branch insertion with a targeted item-limit call replacement for storage output cells. Ordinary cells and input cells retain their normal limits.
- Updated save-loading and legacy oversized-stack patches to validate their anchors. Storage buildings are restored before stored stacks.
- Fixed stored-definition change notification and set input/output positions before spawn registration callbacks. Unspawned storage returns an empty contents sequence.
- Item release now splits/moves the original object instead of constructing a replacement. This preserves quality, other component data, and corpse inner pawns. Filter changes and minifying also release individual apparel correctly.
- Omitted the upstream abandonment update popup from 1.6 content. Original author credits and licenses are retained.

## Verified

Both projects build in Release with zero warnings and zero errors.
Automated in-game checks passed on the installed RimWorld 1.6.4871 (assembly reports rev591; Version.txt says rev590), using the installed Harmony mod:

- Core + Harmony + both ports.
- Core + Royalty + Ideology + Biotech + Anomaly + Odyssey + Harmony + both ports.

For each configuration, checks verified all 16 building definitions and two-cell footprints, weapon classification by mass, Hay/Kibble category patches, the 13x hazardous-material capacity adjustment, a pallet holding 600 steel plus 75 overflow, eight separate output stacks, two individual apparel items, 1.6 menu target visibility, apparel release with quality intact, and corpse release after a filter change with its original inner pawn intact. Saving and reloading retained all steel stacks, input overflow, apparel qualities, and user filters. All 11 Harmony patches installed successfully.

The final two game logs contained no exceptions, failed XML references, failed mod patches, or item-release exhaustion warnings. Results are in `.validation/ProfileCore/result.txt` and `.validation/ProfileAll/result.txt`, with `.validation/PlayerCore.log` and `.validation/PlayerAll.log`.

Validation covers the checks above, not every gameplay interaction. Visual layout and manual menu clicks, gravship transport, other third-party mods (including Combat Extended and VGP), and migration of an existing 1.5 colony were not tested. The extension's pre-existing unconditional `AnimalFeed` category may conflict with another mod defining that category.

## Rebuild and rerun checks

Use a .NET SDK with .NET Framework 4.8 targeting support:

```powershell
.\Build-Mods.ps1
# Package already-built DLLs without recompiling:
.\Package-Mods.ps1
# Launch isolated runtime checks (returns a process ID and result/log paths):
.\Validate-Mods.ps1 -GamePath 'C:\Games\RimWorld.v1.6.4871\RimWorld' -Mode Core
.\Validate-Mods.ps1 -GamePath 'C:\Games\RimWorld.v1.6.4871\RimWorld' -Mode All
```

The validation script defaults to `Mods/HarmonyRimWorld` within that game folder. Supply `-HarmonyPath` for another Harmony mod location. A fresh profile is created for each run. The test mod remains under `.validation` and is excluded from release archives.

Reference packages are pinned: `Krafs.Rimworld.Ref` 1.6.4871 and `Lib.Harmony` 2.3.6. The 1.6 runtime Harmony DLL is supplied by the user's Harmony mod. The base mod's unchanged legacy 1.0 folder retains its original embedded Harmony assembly for 1.0 compatibility.

Binary inspection provenance: REA managed artifact evidence `ev_d761f23cf75491721aa7bafc5a772b11a539d5c34fdba634166975718f650765`; installed Assembly-CSharp.dll SHA-256 `5cf1b5be399d5b1c9c56ca72c9d35b4ecf307feacf5859d04ac5a1aa5926356a`, MVID `61e41735-6189-4da4-9d21-0260257b5097`. Focused ILSpy inspection established patched method bodies; actual game execution established the runtime results above. Game binaries and decompiled game code are excluded from the archives.
