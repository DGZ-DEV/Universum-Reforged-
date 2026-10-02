using System.Collections.Generic;
using Verse;

namespace Universum.Defs {
    [StaticConstructorOnStartup]
    public static class Loader {
        public static Dictionary<string, CelestialObject> celestialObjects = new Dictionary<string, CelestialObject>();
        public static Dictionary<string, ObjectGeneration> celestialObjectGenerationStartUpSteps = new Dictionary<string, ObjectGeneration>();
        public static Dictionary<string, ObjectGeneration> celestialObjectGenerationRandomSteps = new Dictionary<string, ObjectGeneration>();
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Dictionary<string, NamePack> namePacks = new Dictionary<string, NamePack>();
        private static int _totalDefs;

        public static void Init() {
            foreach (CelestialObject celestialObject in DefDatabase<CelestialObject>.AllDefs) {
                celestialObjects[celestialObject.defName] = celestialObject;
                _totalDefs++;
            }
            foreach (ObjectGeneration celestialObjectGenerationStep in DefDatabase<ObjectGeneration>.AllDefs) {
                if (celestialObjectGenerationStep.initializationType == InitializationType.START_UP) {
                    celestialObjectGenerationStartUpSteps[celestialObjectGenerationStep.defName] = celestialObjectGenerationStep;
                } else if (celestialObjectGenerationStep.initializationType == InitializationType.RANDOM) {
                    celestialObjectGenerationRandomSteps[celestialObjectGenerationStep.defName] = celestialObjectGenerationStep;
                    // PORT 1.6: si estas dos listas salen vacias, el generador no coloca nada y el mundo
                    // se ve normal. Es el sintoma de que no aparezcan asteroides ni cuerpos celestes.
                    Verse.Log.Message("[Universum PORT 1.6] Defs de generacion: " + celestialObjects.Count + " CelestialObject, " + celestialObjectGenerationStartUpSteps.Count + " ObjectGeneration de arranque, " + celestialObjectGenerationRandomSteps.Count + " aleatorios.");
                    // PORT 1.6: comprueba si las definiciones de contenido de RimNauts 2 estan dentro.
                    Verse.Log.Message("[Universum PORT 1.6] Contenido de RimNauts 2 -> ThingDef SatelliteOperationsCenter: "
                        + (DefDatabase<ThingDef>.GetNamedSilentFail("RimNauts2_SatelliteOperationsCenter") != null)
                        + " | BiomeDef Satellite: " + (DefDatabase<RimWorld.BiomeDef>.GetNamedSilentFail("RimNauts2_Satellite_Biome") != null)
                        + " | WorldObjectDef CannonShell: " + (DefDatabase<RimWorld.WorldObjectDef>.GetNamedSilentFail("RimNauts2_TravellingDeliveryCannonShell") != null)
                        + " | CelestialObject: " + celestialObjects.Count + " | ObjectGeneration: " + (celestialObjectGenerationStartUpSteps.Count + celestialObjectGenerationRandomSteps.Count));
                }
                _totalDefs++;
            }
            foreach (Material material in DefDatabase<Material>.AllDefs) {
                materials[material.defName] = material;
                _totalDefs++;
            }
            foreach (NamePack namePack in DefDatabase<NamePack>.AllDefs) {
                namePacks[namePack.defName] = namePack;
                _totalDefs++;
            }
            // print mod info
            Logger.print(
                Logger.Importance.Info,
                key: "RimNauts.Info.def_loader_done",
                prefix: Style.tab,
                args: new NamedArgument[] { _totalDefs }
            );
        }
    }
}
