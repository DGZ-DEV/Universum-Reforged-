using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Universum.World.Patch {
    public class TravellingTransporters {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(TravellingTransporters_Start)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(TravellingTransporters_End)).Patch();
        }
    }

    [HarmonyPatch]
    static class TravellingTransporters_Start {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.TravellingTransporters:get_Start");

        public static bool Prefix(RimWorld.Planet.TravellingTransporters __instance, ref Vector3 __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(__instance.initialTile);
            if (objectHolder == null) return true;

            __result = objectHolder.DrawPos;

            return false;
        }
    }

    [HarmonyPatch]
    static class TravellingTransporters_End {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.TravellingTransporters:get_End");

        public static bool Prefix(RimWorld.Planet.TravellingTransporters __instance, ref Vector3 __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(__instance.destinationTile);
            if (objectHolder == null) return true;

            __result = objectHolder.DrawPos;

            return false;
        }
    }
}
