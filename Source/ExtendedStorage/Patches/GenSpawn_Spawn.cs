using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace ExtendedStorage.Patches;

[HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4),
    typeof(WipeMode), typeof(bool), typeof(bool))]
public static class GenSpawn_Spawn
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetMaxItemsAllowedInCell));
        var replacement = AccessTools.Method(typeof(GenSpawn_Spawn), nameof(MaxItemsAllowed));
        var code = instructions.ToList();
        var matches = code.Where(instruction => instruction.Calls(original)).ToList();
        if (matches.Count != 1)
        {
            throw new InvalidOperationException("Extended Storage: could not find the GenSpawn item-limit check.");
        }

        matches[0].operand = replacement;
        return code;
    }

    public static int MaxItemsAllowed(IntVec3 cell, Map map)
    {
        return map.thingGrid.ThingsListAtFast(cell).OfType<Building_ExtendedStorage>()
            .Any(storage => storage.OutputSlot == cell)
            ? int.MaxValue
            : cell.GetMaxItemsAllowedInCell(map);
    }
}
