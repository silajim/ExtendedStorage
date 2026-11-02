using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExtendedStorage;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Storage16Checks;

public class RuntimeChecks : GameComponent
{
    private int stage;
    private bool running;
    private readonly List<string> passed = new();
    public RuntimeChecks(Game game) { }
    public override void ExposeData() { Scribe_Values.Look(ref stage, "stage"); }
    private void Check(bool result, string name)
    {
        if (!result) throw new Exception(name);
        passed.Add(name);
        Log.Message("STORAGE16 PASS: " + name);
    }
    public override void GameComponentUpdate()
    {
        if (running || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        running = true;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        try
        {
            if (stage == 0) InitialChecks();
            else ReloadChecks();
        }
        catch (Exception e)
        {
            Log.Error("STORAGE16 FAIL: " + e);
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "result.txt"), string.Join("\n", passed) + "\nFAIL: " + e);
            Application.Quit(1);
        }
    }
    private Building_ExtendedStorage Build(string name, IntVec3 cell)
    {
        var def = ThingDef.Named(name);
        var storage = (Building_ExtendedStorage)ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
        storage.SetFaction(Faction.OfPlayer);
        storage.label = "CHECK_" + name;
        GenSpawn.Spawn(storage, cell, Find.CurrentMap);
        return storage;
    }
    private void Allow(Building_ExtendedStorage storage, ThingDef def)
    {
        storage.userSettings.filter.SetDisallowAll();
        storage.userSettings.filter.SetAllow(def, true);
    }
    private void Move(Building_ExtendedStorage storage) => AccessTools.Method(typeof(Building_ExtendedStorage), "TryMoveItem").Invoke(storage, null);
    private void InitialChecks()
    {
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.thingClass == typeof(Building_ExtendedStorage)).ToList();
        Check(defs.Count == 16, "all 16 storage buildings loaded");
        Check(ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageId == "mlie.extendedstorageextended"), "extension enabled");
        Check(ThingDef.Named("Hay").thingCategories.Contains(ThingCategoryDef.Named("AnimalFeed")) && ThingDef.Named("Kibble").thingCategories.Contains(ThingCategoryDef.Named("AnimalFeed")), "feed category patches");
        Check(ThingDef.Named("Storage_HazMatContainer").GetStatValueAbstract(StatDef.Named("ES_StorageFactor")) == 13f, "uranium storage capacity patch");
        foreach (var weapon in ThingCategoryDefOf.Weapons.DescendantThingDefs.Where(w => !w.thingCategories.Contains(ThingCategoryDef.Named("Grenades")) && !w.thingCategories.Contains(ThingCategoryDef.Named("MortarShells"))))
        {
            var mass = weapon.BaseMass * (weapon.weaponTags?.Contains("UltratechMelee") == true ? 2 : 1);
            var rack = mass >= 7 ? "Storage_MassiveWeaponsRack" : mass > 3 ? "Storage_LargeWeaponsRack" : "Storage_SmallWeaponsRack";
            Check(ThingDef.Named(rack).building.fixedStorageSettings.filter.Allows(weapon), "weapon filter " + weapon.defName);
        }
        foreach (var method in Harmony.GetAllPatchedMethods().Where(m => Harmony.GetPatchInfo(m).Owners.Contains("com.extendedstorage.patches")))
            passed.Add("patched " + method.DeclaringType.FullName + "." + method.Name);
        var map = Find.CurrentMap;
        var center = map.Center;
        var buildings = new List<Building_ExtendedStorage>();
        for (int i = 0; i < defs.Count; i++)
        {
            var c = center + new IntVec3((i % 4) * 5, 0, (i / 4) * 5);
            foreach (var cell in CellRect.CenteredOn(c, 2))
            {
                foreach (var thing in cell.GetThingList(map).ToList()) thing.Destroy();
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }
            buildings.Add(Build(defs[i].defName, c));
        }
        Check(buildings.All(b => b.Spawned && b.InputSlot != b.OutputSlot), "all buildings spawn with two storage cells");
        var pallet = buildings.Single(b => b.def.defName == "Storage_WoodenPallet");
        Allow(pallet, ThingDefOf.Steel);
        for (int i = 0; i < 9; i++)
        {
            var steel = ThingMaker.MakeThing(ThingDefOf.Steel);
            steel.stackCount = 75;
            GenSpawn.Spawn(steel, pallet.InputSlot, map);
            Move(pallet);
        }
        Check(pallet.StoredThingTotal == 600 && pallet.AtCapacity, "pallet capacity 600");
        Check(pallet.InputSlot.GetThingList(map).Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount) == 75, "overflow remains in input cell");
        Check(pallet.StoredThings.Count() == 8, "multiple output stacks survive spawning");
        var locker = buildings.Single(b => b.def.defName == "Storage_Locker");
        var shirtDef = ThingDef.Named("Apparel_BasicShirt");
        Allow(locker, shirtDef);
        for (int i = 0; i < 2; i++)
        {
            var shirt = ThingMaker.MakeThing(shirtDef, ThingDefOf.Cloth);
            shirt.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
            GenSpawn.Spawn(shirt, locker.InputSlot, map);
            Move(locker);
        }
        Check(locker.StoredThingTotal == 2, "locker stores individual apparel");
        var pawn = map.mapPawns.FreeColonists.FirstOrDefault();
        if (pawn == null)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
            GenSpawn.Spawn(pawn, center + new IntVec3(30, 0, 5), map);
        }
        var context = new FloatMenuContext(new List<Pawn> { pawn }, locker.InputSlot.ToVector3Shifted(), map);
        Check(locker.StoredThings.All(t => context.ClickedThings.Contains(t)), "1.6 menu exposes all stored apparel");
        var releaseLocker = Build("Storage_Locker", center + new IntVec3(25, 0, 0));
        Allow(releaseLocker, shirtDef);
        var original = ThingMaker.MakeThing(shirtDef, ThingDefOf.Cloth);
        original.TryGetComp<CompQuality>().SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
        GenSpawn.Spawn(original, releaseLocker.InputSlot, map);
        Move(releaseLocker);
        releaseLocker.MakeMinified();
        Check(original.Spawned && original.TryGetQuality(out var quality) && quality == QualityCategory.Legendary && original.Position != releaseLocker.OutputSlot, "minifying releases apparel and preserves quality");
        var hooks = buildings.Single(b => b.def.defName == "Storage_MeatHooks");
        var deer = PawnGenerator.GeneratePawn(PawnKindDef.Named("Deer"));
        GenSpawn.Spawn(deer, center + new IntVec3(30, 0, 0), map);
        deer.Kill(null);
        var corpse = deer.Corpse;
        Allow(hooks, corpse.def);
        corpse.DeSpawn();
        GenSpawn.Spawn(corpse, hooks.InputSlot, map);
        Move(hooks);
        Check(hooks.StoredThingTotal == 1, "meat hooks store corpse");
        hooks.userSettings.filter.SetDisallowAll();
        var release = (Action)AccessTools.Field(typeof(Building_ExtendedStorage), "queuedTickAction").GetValue(hooks);
        release?.Invoke();
        AccessTools.Field(typeof(Building_ExtendedStorage), "queuedTickAction").SetValue(hooks, null);
        Check(corpse.Spawned && corpse.InnerPawn == deer && !corpse.Destroyed && corpse.Position != hooks.OutputSlot, "filter change releases original corpse with inner pawn intact");
        DeepStorageChecks.Run(map, Check);
        stage = 1;
        GameDataSaveLoader.SaveGame("Storage16RuntimeChecks");
        Check(File.Exists(GenFilePaths.FilePathForSavedGame("Storage16RuntimeChecks")), "save written");
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "stage1.txt"), string.Join("\n", passed));
        GameDataSaveLoader.LoadGame("Storage16RuntimeChecks");
    }
    private void ReloadChecks()
    {
        var buildings = Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_ExtendedStorage>().ToList();
        var pallet = buildings.Single(b => b.label == "CHECK_Storage_WoodenPallet");
        var locker = buildings.Single(b => b.label == "CHECK_Storage_Locker");
        Check(pallet.StoredThingTotal == 600 && pallet.StoredThings.Count() == 8, "save/reload preserves eight output stacks and 600 steel");
        Check(pallet.InputSlot.GetThingList(pallet.Map).Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount) == 75, "save/reload preserves input overflow");
        Check(locker.StoredThingTotal == 2 && locker.StoredThings.All(t => t.TryGetQuality(out var q) && q == QualityCategory.Excellent), "save/reload preserves individual apparel and quality");
        Check(pallet.userSettings.filter.Allows(ThingDefOf.Steel), "save/reload preserves user filter");
        DeepStorageChecks.AfterReload(Find.CurrentMap, Check);
        var all = File.ReadAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "stage1.txt")) + "\n" + string.Join("\n", passed) + "\nPASS";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "result.txt"), all);
        Log.Message("STORAGE16 ALL CHECKS PASSED");
        Application.Quit(0);
    }
}
