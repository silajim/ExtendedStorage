using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace ExtendedStorage.Patches;

[HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
internal class Map_FinalizeLoading
{
    private static readonly MethodInfo mi =
        typeof(BackCompatibility).GetMethod("PreCheckSpawnBackCompatibleThingAfterLoading");

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
    {
        var list = new List<CodeInstruction>(instr);
        var num = list.FindIndex(ci => ci.opcode == OpCodes.Call && (MethodInfo)ci.operand == mi);
        if (num == -1 || num + 1 >= list.Count || !list[num + 1].IsLdloc())
        {
            throw new InvalidOperationException("Extended Storage: could not find the map-loading thing list.");
        }

        list.InsertRange(num + 1, new[]
        {
            new CodeInstruction(list[num + 1].opcode, list[num + 1].operand),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call,
                typeof(Map_FinalizeLoading).GetMethod("PrioritySpawnStorageBuildings"))
        });
        return list;
    }

    public static void PrioritySpawnStorageBuildings(List<Thing> things, Map map)
    {
        foreach (var building_ExtendedStorage in things.OfType<Building_ExtendedStorage>().ToArray())
        {
            try
            {
                GenSpawn.SpawnBuildingAsPossible(building_ExtendedStorage, map, true);
                if (building_ExtendedStorage.Spawned)
                {
                    things.Remove(building_ExtendedStorage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Exception spawning loaded thing {building_ExtendedStorage.ToStringSafe<Building>()}: {ex}");
            }

        }
    }
}
