using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ExtendedStorage.Patches;

// Deep Storage calls Notify_Spawned from the CellCache constructor before publishing
// the new cache. Stockpile Limit can then query that same cell from a hauling check.
[HarmonyPatch]
internal static class DeepStorage_UpdateCache
{
    [ThreadStatic]
    private static Dictionary<object, HashSet<IntVec3>> activeUpdates;

    private static MethodBase updateCache;

    private static bool Prepare()
    {
        var componentType = AccessTools.TypeByName("LWM.DeepStorage.MapComponentDS");
        var storageType = AccessTools.TypeByName("LWM.DeepStorage.CompDeepStorage");
        if (componentType == null || storageType == null)
        {
            return false;
        }

        updateCache = AccessTools.Method(componentType, "UpdateCache", new[] { typeof(IntVec3), storageType });
        return updateCache != null;
    }

    private static MethodBase TargetMethod() => updateCache;

    [HarmonyPriority(Priority.First)]
    internal static bool Prefix(object __instance, IntVec3 cell, out bool __state)
    {
        activeUpdates ??= new Dictionary<object, HashSet<IntVec3>>();
        if (!activeUpdates.TryGetValue(__instance, out var cells))
        {
            cells = new HashSet<IntVec3>();
            activeUpdates.Add(__instance, cells);
        }

        __state = cells.Add(cell);
        // A nested capacity query falls back to CapacityToStoreThingAtDirect when
        // no cache is present. It must not recursively construct another cache.
        return __state;
    }

    internal static void Finalizer(object __instance, IntVec3 cell, bool __state)
    {
        if (!__state || activeUpdates == null || !activeUpdates.TryGetValue(__instance, out var cells))
        {
            return;
        }

        cells.Remove(cell);
        if (cells.Count == 0)
        {
            activeUpdates.Remove(__instance);
        }
    }
}
