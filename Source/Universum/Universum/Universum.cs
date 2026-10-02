using System.Reflection;
using Verse;
using UnityEngine;

namespace Universum {
    [StaticConstructorOnStartup]
    public static class Universum {
        static Universum() {
            // apply patch on internal class
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("sindre0830.universum");
            // Diagnostico ANTES de parchear: si el estado cambia entre Prepare() y el parcheo
            // real, esto lo delata. Es lo unico que me queda para nombrar al culpable.
            PortVerification.Log(harmony);
            // PORT 1.6: todo el parcheo va en un try/catch. Si un solo parche falla (por ejemplo un
            // transpiler escrito para una firma antigua), Harmony lanza y ANTES se llevaba por delante
            // el resto del constructor: ni ajustes, ni definiciones, ni assets. Con esto el mod carga
            // igual y el fallo queda escrito en el registro.
            try {
                harmony.Patch(
                    original: HarmonyLib.AccessTools.Method(HarmonyLib.AccessTools.TypeByName("SectionLayer_Terrain"), "Regenerate"),
                    postfix: new HarmonyLib.HarmonyMethod(typeof(Utilities.SectionLayer_Terrain_Regenerate).GetMethod("Postfix"))
                );
                // PORT 1.6: parche del constructor de Section, a mano y con guarda. Antes se hacia
                // por atributo y reventaba si la firma del constructor habia cambiado.
                try {
                    System.Reflection.ConstructorInfo ctorSection = HarmonyLib.AccessTools.Constructor(typeof(Verse.Section), new[] { typeof(Verse.IntVec3), typeof(Verse.Map) });
                    if (ctorSection != null) {
                        harmony.Patch(original: ctorSection, postfix: new HarmonyLib.HarmonyMethod(typeof(Utilities.Section_Constructor).GetMethod("Postfix")));
                    } else {
                        Verse.Log.Warning("[Universum PORT 1.6] No existe Section(IntVec3, Map): el parche del constructor queda sin aplicar.");
                    }
                } catch (System.Exception e) { Verse.Log.Warning("[Universum PORT 1.6] Fallo parcheando el constructor de Section: " + e.Message); }
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            } catch (System.Exception e) {
                Verse.Log.Error("[Universum PORT 1.6] Fallo aplicando parches. El mod SIGUE cargando, pero puede quedar incompleto: " + e.ToString());
            }
            PortVerification.Log(harmony);
            // print mod info
            Logger.print(
                Logger.Importance.Info,
                key: "Universum.Info.mod_loaded",
                args: new NamedArgument[] { ModContent.instance.Content.ModMetaData.Name, ModContent.instance.Content.ModMetaData.ModVersion }
            );
            // load configuarations
            Defs.Loader.Init();
            Settings.init();
            Utilities.Biome.Handler.init();
            Utilities.Terrain.Handler.init();
            if (ModsConfig.BiotechActive) Utilities.Gene.Handler.init();
            Assets.Init();
            // branch if camera+ patch needs to be applied
            if (ModsConfig.IsActive("brrainz.cameraplus")) {
                Globals.planet_mat.mainTextureOffset = new Vector2(0.3f, 0.3f);
                Globals.planet_mat.mainTextureScale = new Vector2(0.4f, 0.4f);
                Globals.planet_mat_glass.mainTextureOffset = new Vector2(0.3f, 0.3f);
                Globals.planet_mat_glass.mainTextureScale = new Vector2(0.4f, 0.4f);
                Globals.planet_render_altitude *= 1.6f;
                Logger.print(
                    Logger.Importance.Info,
                    key: "Universum.Info.camera_patch_applied",
                    prefix: Style.tab
                );
            }
        }

        public class ModContent : Mod {
            public static ModContent instance { get; private set; }

            public ModContent(ModContentPack content) : base(content) {
                instance = this;
            }
        }
    }
}
