using RimWorld;
using Verse;

namespace ExtendedStorage;

internal static class StorageUtility
{
    public static Building_ExtendedStorage GetStoringBuilding(Thing t)
    {
        Building_ExtendedStorage building_ExtendedStorage;
        if (t is not Building_ExtendedStorage)
        {
            var slotGroup = t.GetSlotGroup();
            building_ExtendedStorage = slotGroup?.parent as Building_ExtendedStorage;
        }
        else
        {
            building_ExtendedStorage = null;
        }

        var building_ExtendedStorage2 = building_ExtendedStorage;
        return building_ExtendedStorage2?.StoredThingDef != t.def ? null : building_ExtendedStorage2;
    }

    public static void SplitOfStackInto(Thing existingThing, IntVec3 targetLocation)
    {
        var map = existingThing.Map;
        // Split the original object to retain corpse contents, quality, and other comp data.
        var thing = existingThing.SplitOff(System.Math.Min(existingThing.stackCount, existingThing.def.stackLimit));
        GenSpawn.Spawn(thing, targetLocation, map);
    }

    public static bool SingleStorableDef(this ThingDef def)
    {
        return def.IsApparel || def.IsCorpse || def.IsWeapon;
    }
}
