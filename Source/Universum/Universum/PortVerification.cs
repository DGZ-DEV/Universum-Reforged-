using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Universum {
    /// <summary>
    /// Diagnostico del port a 1.6. Escribe en el registro cuantos metodos ha parcheado Harmony y,
    /// sobre todo, QUE PARCHES SE HAN AUTODESACTIVADO porque su Prepare() no encontro el objetivo.
    /// Eso es imprescindible aqui: varios parches localizan su metodo POR CADENA DE TEXTO, asi que
    /// si el nombre cambio en 1.6 no fallan al compilar: simplemente no se aplican y nadie se entera.
    /// </summary>
    internal static class PortVerification {
        public static void Log(Harmony harmony) {
            try {
                int parcheados = 0;
                try { parcheados = harmony.GetPatchedMethods().Count(); } catch (Exception) { }

                List<string> desactivados = new List<string>();
                Type[] tipos;
                try { tipos = Assembly.GetExecutingAssembly().GetTypes(); }
                catch (ReflectionTypeLoadException e) { tipos = e.Types.Where(t => t != null).ToArray(); }

                for (int i = 0; i < tipos.Length; i++) {
                    Type t = tipos[i];
                    if (t == null) continue;
                    if (t.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                    MethodInfo prep = t.GetMethod("Prepare", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (prep == null) continue;
                    bool listo = false;
                    try { listo = (bool)prep.Invoke(null, null); } catch (Exception) { listo = false; }
                    if (!listo) desactivados.Add(t.Name);
                }

                Verse.Log.Message("[Universum PORT 1.6] Harmony ha parcheado " + parcheados + " metodos. Parches autodesactivados por no encontrar su objetivo: " + (desactivados.Count == 0 ? "NINGUNO" : string.Join(", ", desactivados.ToArray())));
            } catch (Exception e) {
                Verse.Log.Error("[Universum PORT 1.6] Fallo el diagnostico de parches: " + e.Message);
            }
        }
    }
}