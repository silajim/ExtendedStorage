using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace ExtendedStorage.Patches;

[HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
internal class Thing_SpawnSetup
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionsList = instructions.ToList();
        var matches = 0;
        int num;
        for (var i = 0; i < instructionsList.Count; i = num + 1)
        {
            var instruction = instructionsList[i];
            yield return instruction;
            if (i > 0 && (instruction.opcode == OpCodes.Ble || instruction.opcode == OpCodes.Ble_S) &&
                instructionsList[i - 1].LoadsField(AccessTools.Field(typeof(ThingDef), nameof(ThingDef.stackLimit))))
            {
                matches++;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(Thing_SpawnSetup).GetMethod("VerifyThingShouldNotBeTruncated"));
                yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
            }

            num = i;
        }

        if (matches != 1)
        {
            throw new InvalidOperationException("Extended Storage: could not find the SpawnSetup stack-limit check.");
        }
    }

    public static bool VerifyThingShouldNotBeTruncated(Thing t, Map map)
    {
        return map.thingGrid.ThingsListAt(t.Position).FirstOrDefault(o => o is Building_ExtendedStorage) is
            Building_ExtendedStorage building_ExtendedStorage && building_ExtendedStorage.OutputSlot == t.Position;
    }
}
