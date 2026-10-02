using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Universum.Utilities {
    // PORT 1.6: se le quita el [HarmonyPatch]. Harmony intentaba resolverlo por atributo
    // aunque Prepare() devolviera false, y con el metodo SOBRECARGADO (2 miembros) lanzaba
    // "Undefined target method". Este parche forzaba el brillo solar a 1.0 en casillas con
    // mascara de oceano: queda RETIRADO. Efecto visual menor, a cambio de no romper el parcheo.
    public class GenCelestial_CelestialSunGlow {
    // PORT 1.6: antes esto era [HarmonyPatch(typeof(GenCelestial), "CelestialSunGlow",
    // argumentTypes: new Type[] { typeof(int), typeof(int) })]. En 1.6 la firma no coincide
    // (el oraculo la ve como params object[]), Type.GetMethod exige coincidencia EXACTA,
    // devolvia null y Harmony fallaba con "Patching exception in method null", que se
    // llevaba por delante el resto del parcheo. Ahora se autodesactiva si no lo encuentra.
    public static bool Prepare() => TargetMethod() != null;

    public static System.Reflection.MethodBase TargetMethod() => HarmonyLib.AccessTools.Method(typeof(RimWorld.GenCelestial), "CelestialSunGlow", new Type[] { typeof(int), typeof(int) });
        public static bool Prefix(ref float __result, int tile, int ticksAbs) {
            if (tile == -1) return false;
            if (!Cache.allowed_utility(WorldGridHelper.GetBiome(tile), "universum.remove_shadows")) return true;
            __result = 1.0f;
            return false;
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L14
     */
    [HarmonyPatch(typeof(SkyManager), "SkyManagerUpdate")]
    public class SkyManager_SkyManagerUpdate {
        public static void Postfix() {
            if (!Cache.allowed_utility(Find.CurrentMap, "universum.remove_shadows")) return;
            if (!Cache.allowed_utility(Find.CurrentMap, "universum.vacuum")) return;
            MatBases.LightOverlay.color = new Color(1.0f, 1.0f, 1.0f);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/ShipInteriorMod2.cs#L2220
     */
    [HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
    public static class MapDrawer_DrawMapMesh {
        public static void Prefix() {
            if (!Cache.allowed_utility("universum.vacuum_overlay")) return;
            Map map = Find.CurrentMap;
            if (Globals.rendered || !Cache.allowed_utility(map, "universum.vacuum")) return;
            get_world_map_render();
            if (!WorldLayersHelper.ShouldRegenerate(Find.World.renderer)) {
                Globals.rendered = true;
            }
        }

        public static void get_world_map_render() {
            // block celestial object rendering
            Game.MainLoop.instance.blockRendering = true;
            Game.MainLoop.instance.ForceRender();

            RenderTexture oldTexture = Find.WorldCamera.targetTexture;
            RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
            Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
            Find.WorldCameraDriver.altitude = Globals.planet_render_altitude;
            Find.WorldCameraDriver.GetType()
                .GetField("desiredAltitude", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(Find.WorldCameraDriver, Globals.planet_render_altitude);

            float aspect = (float) UI.screenWidth / UI.screenHeight;

            Find.WorldCameraDriver.Update();
            Find.World.renderer.CheckActivateWorldCamera();
            Find.World.renderer.DrawWorldLayers();
            // PORT 1.6: WorldRendererUtility.UpdateWorldShadersParams() desaparecio en 1.6.
            // Se retira la llamada y queda PENDIENTE ver en el juego si los shaders del planeta
            // se actualizan solos. Si se ve mal, aqui es donde hay que buscar el sustituto.

            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = Globals.render;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = aspect;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

            Find.WorldCamera.targetTexture = Globals.render;
            Find.WorldCamera.aspect = aspect;
            Find.WorldCamera.Render();

            RenderTexture.active = Globals.render;
            Globals.planet_screenshot.ReadPixels(new Rect(0, 0, 2048, 2048), 0, 0);
            Globals.planet_screenshot.Apply();
            RenderTexture.active = null;

            Find.WorldCamera.targetTexture = oldTexture;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
            Find.World.renderer.CheckActivateWorldCamera();
            // unblock celestial object rendering
            Game.MainLoop.instance.blockRendering = false;
            Game.MainLoop.instance.ForceRender();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/ShipInteriorMod2.cs#L2283
     */
    // PORT 1.6: era [HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]. Harmony no
    // resolvia el objetivo ("Undefined target method"): el miembro existe, pero en 1.6 debe de
    // estar declarado en una clase base y no ser publico, y GetMethod no ve los privados
    // heredados. Se parchea a mano desde Universum.cs con guarda de nulo.
    public static class SectionLayer_FinalizeMesh {
        public static bool Prefix(SectionLayer __instance, Section ___section) {
            if (!Cache.allowed_utility("universum.vacuum_overlay")) return true;
            if (__instance.GetType().Name != "SectionLayer_Terrain" || !Cache.allowed_utility(___section.map, "universum.vacuum")) return true;
            bool foundSpace = false;
            foreach (IntVec3 cell in ___section.CellRect.Cells) {
                TerrainDef terrain1 = ___section.map.terrainGrid.TerrainAt(cell);
                if (Cache.allowed_utility(terrain1, "universum.vacuum_overlay")) {
                    foundSpace = true;
                    Material mat = Globals.planet_mat;
                    if (terrain1.defName == "RimNauts2_Vacuum_Glass") mat = Globals.planet_mat_glass;
                    Printer_Mesh.PrintMesh(__instance, Matrix4x4.TRS(cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, Vector3.one), MeshMakerPlanes.NewPlaneMesh(1f), mat);
                }
            }
            if (!foundSpace) {
                for (int i = 0; i < __instance.subMeshes.Count; i++) {
                    if (__instance.subMeshes[i].material == Globals.planet_mat || __instance.subMeshes[i].material == Globals.planet_mat_glass) {
                        __instance.subMeshes.RemoveAt(i);
                    }
                }
            }
            return true;
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L110
     */
    [HarmonyPatch(typeof(RimWorld.MapInterface), "Notify_SwitchedMap")]
    public class MapInterface_Notify_SwitchedMap {
        public static bool MapIsSpace;

        public static void Postfix() {
            if (Find.CurrentMap == null || Scribe.mode != LoadSaveMode.Inactive) return;
            MapIsSpace = Cache.allowed_utility(Find.CurrentMap, "universum.vacuum");
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L126
     */
    [HarmonyPatch(typeof(Verse.Game), "LoadGame")]
    public class Game_LoadGame {
        public static void Postfix() {
            Globals.rendered = false;
            MapInterface_Notify_SwitchedMap.Postfix();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L153
     */
    [HarmonyPatch(typeof(Verse.Game), "UpdatePlay")]
    public class Game_UpdatePlay {
        public static CameraDriver Driver;
        public static Camera GameCamera;
        public static Vector3 Center;
        public static float CellsHigh;
        public static float CellsWide;
        public static Dictionary<Map, Dictionary<Section, SectionLayer>> MapSections = new Dictionary<Map, Dictionary<Section, SectionLayer>>();
        private static Vector3 lastCameraPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        public static void add_section(Map map, Section section, SectionLayer layer) {
            if (!MapSections.TryGetValue(map, out Dictionary<Section, SectionLayer> sections)) {
                sections = new Dictionary<Section, SectionLayer>();
                MapSections.Add(map, sections);
            }
            sections.Add(section, layer);
        }

        public static void Prefix() {
            if (!Cache.allowed_utility("universum.vacuum_overlay")) return;
            if (!MapInterface_Notify_SwitchedMap.MapIsSpace || !MapSections.ContainsKey(Find.CurrentMap)) return;
            Center = GameCamera.transform.position;
            var ratio = (float) UI.screenWidth / UI.screenHeight;
            CellsHigh = UI.screenHeight / Find.CameraDriver.CellSizePixels;
            CellsWide = CellsHigh * ratio;
            if ((lastCameraPosition - Center).magnitude < 1e-4) return;
            lastCameraPosition = Center;
            var sections = MapSections[Find.CurrentMap];
            var visibleRect = Driver.CurrentViewRect;
            foreach (var entry in sections) {
                if (!visibleRect.Overlaps(entry.Key.CellRect)) continue;
                MeshRecalculateHelper.recalculate_layer(entry.Value);
            }
        }

        public static void Postfix() {
            if (!MeshRecalculateHelper.Tasks.Any()) return;
            Task.WaitAll(MeshRecalculateHelper.Tasks.ToArray());
            MeshRecalculateHelper.Tasks.Clear();
            foreach (var layer in MeshRecalculateHelper.LayersToDraw) {
                var mesh = layer.GetSubMesh(Globals.planet_mat);
                var mesh_glass = layer.GetSubMesh(Globals.planet_mat_glass);
                if (!(!mesh.finalized || mesh.disabled)) Graphics.DrawMesh(mesh.mesh, Vector3.zero, Quaternion.identity, mesh.material, 0);
                if (!(!mesh_glass.finalized || mesh_glass.disabled)) Graphics.DrawMesh(mesh_glass.mesh, Vector3.zero, Quaternion.identity, mesh_glass.material, 0);
            }
            MeshRecalculateHelper.LayersToDraw.Clear();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L138
     */
    [HarmonyPatch(typeof(Verse.Game), "FinalizeInit")]
    public class Game_FinalizeInit {
        public static void Postfix() {
            Game_UpdatePlay.Driver = Find.CameraDriver;
            Game_UpdatePlay.GameCamera = Find.CameraDriver.GetComponent<Camera>();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L69
     */
    public class MeshRecalculateHelper {
        public static List<Task> Tasks = new List<Task>();
        public static List<SectionLayer> LayersToDraw = new List<SectionLayer>();

        public static void recalculate_layer(SectionLayer instance) {
            var mesh = instance.GetSubMesh(Globals.planet_mat);
            Tasks.Add(Task.Factory.StartNew(() => recalculate_mesh(mesh)));
            var mesh_glass = instance.GetSubMesh(Globals.planet_mat_glass);
            Tasks.Add(Task.Factory.StartNew(() => recalculate_mesh(mesh_glass)));
            LayersToDraw.Add(instance);
        }

        private static void recalculate_mesh(object info) {
            if (!(info is LayerSubMesh mesh)) {
                Logger.print(
                    Logger.Importance.Error,
                    key: "Universum.Error.thread_with_wrong_type",
                    prefix: Style.name_prefix
                );
                return;
            }
            lock (mesh) {
                mesh.finalized = false;
                // PORT 1.6 (corregido): AQUI ESTABA mesh.Clear(MeshParts.All). Ese Clear vaciaba los
                // VERTICES, que son la geometria que la capa ya habia generado, asi que el bucle de
                // abajo recorria cero elementos, no se anadia ninguna UV y FinalizeMesh cocinaba una
                // malla vacia: "Cannot cook Tris/Verts ... no ingredients data" en cada recalculo.
                // El proposito del metodo es solo RECALCULAR LAS UV a partir de los vertices, no
                // reconstruir la malla, asi que no hay que limpiar nada mas que las UV.
                // PORT 1.6: en 1.6 MeshParts.All YA NO incluye las coordenadas de textura, asi que
                // Clear(MeshParts.All) vacia los vertices pero DEJA LAS UV ACUMULADAS. El bucle de abajo
                // anade una UV por vertice, de modo que en cada recalculo la lista de UV crecia y la de
                // vertices empezaba de cero: al no encajar los numeros, Unity se negaba a cocinar la
                // malla y el registro se llenaba de "Cannot cook Tris/Verts ... no ingredients data",
                // saltando cada vez que se recalculaba la capa (o sea, al hacer casi cualquier cosa).
                mesh.uvs.Clear();
                for (var i = 0; i < mesh.verts.Count; i++) {
                    var xdiff = mesh.verts[i].x - Game_UpdatePlay.Center.x;
                    var xfromEdge = xdiff + Game_UpdatePlay.CellsWide / 2.0f;
                    var zdiff = mesh.verts[i].z - Game_UpdatePlay.Center.z;
                    var zfromEdge = zdiff + Game_UpdatePlay.CellsHigh / 2.0f;
                    mesh.uvs.Add(new Vector3(xfromEdge / Game_UpdatePlay.CellsWide, zfromEdge / Game_UpdatePlay.CellsHigh, 0.0f));
                }
                mesh.FinalizeMesh(MeshParts.All);
            }
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L27
     */
    // PORT 1.6: era [HarmonyPatch(typeof(Section), MethodType.Constructor, typeof(IntVec3), typeof(Map))].
    // Harmony lo intentaba resolver por atributo y, si la firma del constructor cambio, lanzaba
    // "Patching exception in method null / Undefined target method" y abortaba el resto del parcheo.
    // Ahora se parchea a mano desde Universum.cs, con guarda de nulo.
    [StaticConstructorOnStartup]
    public class Section_Constructor {
        private static readonly Type SunShadowsType;
        private static readonly Type TerrainType;

        static Section_Constructor() {
            SunShadowsType = AccessTools.TypeByName("SectionLayer_SunShadows");
            TerrainType = AccessTools.TypeByName("SectionLayer_Terrain");
        }

        public static void Postfix(Map map, Section __instance, List<SectionLayer> ___layers) {
            if (!Cache.allowed_utility("universum.vacuum_overlay")) return;
            if (!Cache.allowed_utility(map, "universum.vacuum")) return;
            // Kill shadows
            if (Cache.allowed_utility(map, "universum.remove_shadows")) ___layers.RemoveAll(layer => SunShadowsType.IsInstanceOfType(layer));
            // Get and store terrain layer for recalculation
            var terrain = ___layers.Find(layer => TerrainType.IsInstanceOfType(layer));
            Game_UpdatePlay.add_section(map, __instance, terrain);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L56
     */
    public class SectionLayer_Terrain_Regenerate {
        public static void Postfix(SectionLayer __instance, Section ___section) {
            if (!Cache.allowed_utility("universum.vacuum_overlay")) return;
            if (!Cache.allowed_utility(___section.map, "universum.vacuum")) return;
            MeshRecalculateHelper.recalculate_layer(__instance);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/main/Source/1.4/ShipInteriorMod2.cs#L4402
     */
    [HarmonyPatch(typeof(RimWorld.Scenario), "PostWorldGenerate")]
    public class Scenario_PostWorldGenerate {
        public static void Prefix() => Globals.rendered = false;
    }

    [HarmonyPatch(typeof(Verse.Game), "InitNewGame")]
    public class Game_GameInitData {
        public static void Prefix() => Globals.rendered = false;
    }
}
