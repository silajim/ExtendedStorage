using System;
using System.Linq;
using System.Reflection;
using ExtendedStorage;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Storage16Checks;

internal static class DeepStorageChecks
{
    private const string Owner = "com.extendedstorage.patches";
    private const string ProbeOwner = "local.storage16checks.reentry";
    private const string Marker = "STORAGE16_EXPECTED_CACHE_REENTRY";
    [ThreadStatic] private static int probeDepth;
    private static int deepestProbe;

    public static void Run(Map map, Action<bool, string> check)
    {
        var componentType = AccessTools.TypeByName("LWM.DeepStorage.MapComponentDS");
        if (componentType == null) return;
        var storageType = AccessTools.TypeByName("LWM.DeepStorage.CompDeepStorage");
        var component = map.components.Single(c => c.GetType() == componentType);
        var update = AccessTools.Method(componentType, "UpdateCache");
        var dirty = AccessTools.Method(componentType, "DirtyCache");
        var canStore = AccessTools.Method(componentType, "CanStoreItemAt", new[] { storageType, typeof(Thing), typeof(IntVec3) });
        var limitsType = AccessTools.TypeByName("StockpileLimit.AdditionalStorageSettings");
        check(limitsType != null, "Deep Storage regression includes Stockpile Limit");
        var setLimit = AccessTools.Method(limitsType, "SetStacklimit");
        var cell = map.Center + new IntVec3(0, 0, 25);
        foreach (var thing in cell.GetThingList(map).ToList()) thing.Destroy();
        map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
        var shelf = (Building_Storage)ThingMaker.MakeThing(ThingDef.Named("ShelfSmall"), ThingDefOf.WoodLog);
        shelf.SetFaction(Faction.OfPlayer);
        shelf.label = "CHECK_DS_REENTRY";
        shelf.settings.filter.SetDisallowAll();
        shelf.settings.filter.SetAllow(ThingDefOf.Steel, true);
        shelf.settings.Priority = StoragePriority.Critical;
        GenSpawn.Spawn(shelf, cell, map);
        var comp = shelf.AllComps.Single(c => c.GetType() == storageType);
        AccessTools.Field(storageType, "limitingTotalFactorForCell").SetValue(comp, 0f);
        var steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 75;
        GenSpawn.Spawn(steel, cell, map);
        setLimit.Invoke(null, new object[] { shelf.settings, 5 });

        var guard = typeof(Building_ExtendedStorage).Assembly.GetType("ExtendedStorage.Patches.DeepStorage_UpdateCache", true);
        var prefix = AccessTools.Method(guard, "Prefix");
        var finalizer = AccessTools.Method(guard, "Finalizer");
        var harmony = new Harmony(Owner);
        var probe = new Harmony(ProbeOwner);
        var detected = false;
        try
        {
            harmony.Unpatch(update, prefix);
            harmony.Unpatch(update, finalizer);
            probe.Patch(update,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(DeepStorageChecks), nameof(ProbePrefix))) { priority = Priority.First },
                finalizer: new HarmonyMethod(AccessTools.Method(typeof(DeepStorageChecks), nameof(ProbeFinalizer))));
            dirty.Invoke(component, new object[] { cell });
            try { canStore.Invoke(component, new object[] { comp, steel, cell }); }
            catch (Exception error) { detected = error.ToString().Contains(Marker); }
        }
        finally
        {
            probe.Unpatch(update, HarmonyPatchType.All, ProbeOwner);
            harmony.Patch(update,
                prefix: new HarmonyMethod(prefix) { priority = Priority.First },
                finalizer: new HarmonyMethod(finalizer));
        }
        check(detected && deepestProbe >= 3 && probeDepth == 0,
            "unpatched Deep Storage and five-item shelf limit reproduce recursive cache construction (bounded at depth three)");

        dirty.Invoke(component, new object[] { cell });
        check((bool)canStore.Invoke(component, new object[] { comp, steel, cell }),
            "guarded cache construction completes with unlimited mass");
        check(!HasStorageSpace(cell, map, steel), "five-item Stockpile Limit remains enforced");
        check(steel.Spawned && steel.stackCount == 75, "recursion fix preserves all 75 items");
        AccessTools.Field(storageType, "limitingTotalFactorForCell").SetValue(comp, 1f);
        dirty.Invoke(component, new object[] { cell });
        check(!(bool)canStore.Invoke(component, new object[] { comp, steel, cell }),
            "Deep Storage mass capacity remains enforced");

        var failed = false;
        dirty.Invoke(component, new object[] { cell });
        try { update.Invoke(component, new object[] { cell, null }); }
        catch (TargetInvocationException) { failed = true; }
        AccessTools.Field(storageType, "limitingTotalFactorForCell").SetValue(comp, 0f);
        check(failed && (bool)canStore.Invoke(component, new object[] { comp, steel, cell }),
            "cache guard is released after an exception");
    }

    public static void AfterReload(Map map, Action<bool, string> check)
    {
        if (AccessTools.TypeByName("LWM.DeepStorage.MapComponentDS") == null) return;
        var shelf = map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>()
            .Single(b => b.label == "CHECK_DS_REENTRY");
        var steel = shelf.Position.GetThingList(map).Single(t => t.def == ThingDefOf.Steel);
        check(steel.stackCount == 75 && !HasStorageSpace(shelf.Position, map, steel),
            "save/reload preserves the over-limit shelf and completes without cache recursion");
    }

    private static void ProbePrefix(out bool __state)
    {
        __state = true;
        deepestProbe = Math.Max(deepestProbe, ++probeDepth);
        if (probeDepth >= 3) throw new InvalidOperationException(Marker);
    }

    private static bool HasStorageSpace(IntVec3 cell, Map map, Thing thing) =>
        (bool)AccessTools.Method(typeof(StoreUtility), "NoStorageBlockersIn")
            .Invoke(null, new object[] { cell, map, thing });

    private static void ProbeFinalizer(bool __state)
    {
        if (__state) probeDepth--;
    }
}
