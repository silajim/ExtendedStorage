using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExtendedStorage.Patches;

// 1.6 providers generate the normal wear/equip options, including all vanilla restrictions.
[HarmonyPatch(typeof(FloatMenuContext), MethodType.Constructor,
    typeof(List<Pawn>), typeof(Vector3), typeof(Map))]
internal static class FloatMenuContext_Constructor
{
    public static void Postfix(FloatMenuContext __instance)
    {
        var cell = __instance.ClickedCell;
        var map = __instance.map;
        if (map == null || !cell.InBounds(map))
        {
            return;
        }

        var storage = map.thingGrid.ThingsListAt(cell).OfType<Building_ExtendedStorage>().FirstOrDefault();
        if (storage == null)
        {
            return;
        }

        foreach (var thing in storage.StoredThings)
        {
            if ((thing.def.IsApparel || thing.def.IsWeapon) && !__instance.ClickedThings.Contains(thing))
            {
                __instance.ClickedThings.Add(thing);
            }
        }
    }
}
