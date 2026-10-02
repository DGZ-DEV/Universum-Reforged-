using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace Universum {
    /// <summary>
    /// Espejo de miembros de vanilla que en 1.6 pasaron de publicos a privados.
    /// Siguen existiendo con el mismo nombre, asi que se accede por reflexion.
    /// Los campos de instancia se exponen devolviendo REFERENCIA: la misma llamada
    /// sirve para leer, escribir y pasar el valor a un parametro ref.
    /// </summary>
    internal static class MirrorRoomTemp {
        private static readonly System.Reflection.MethodInfo getMap = AccessTools.PropertyGetter(typeof(Verse.RoomTempTracker), "Map");
        private static readonly System.Reflection.MethodInfo getRoom = AccessTools.PropertyGetter(typeof(Verse.RoomTempTracker), "room");
        public static Map Map(Verse.RoomTempTracker d) { return (Map)getMap.Invoke(d, null); }
        public static Room room(Verse.RoomTempTracker d) { return (Room)getRoom.Invoke(d, null); }
    }

    internal static class MirrorWeather {
        public static readonly AccessTools.FieldRef<RimWorld.WeatherDecider, Map> map = AccessTools.FieldRefAccess<RimWorld.WeatherDecider, Map>("map");
    }

    internal static class MirrorExitMap {
        public static readonly AccessTools.FieldRef<Verse.ExitMapGrid, Map> map = AccessTools.FieldRefAccess<Verse.ExitMapGrid, Map>("map");
    }

    internal static class MirrorTick {
        public static readonly AccessTools.FieldRef<Verse.TickManager, Verse.TimeSpeed> curTimeSpeed = AccessTools.FieldRefAccess<Verse.TickManager, Verse.TimeSpeed>("curTimeSpeed");
    }

    internal static class MirrorWorldRenderer {
        private static readonly System.Reflection.FieldInfo worldRenderedNow = AccessTools.Field(typeof(RimWorld.Planet.WorldRendererUtility), "WorldRenderedNow");
        public static bool WorldRenderedNow { get { return (bool)worldRenderedNow.GetValue(null); } }
    }

    internal static class MirrorWorldCamera {
        private static readonly System.Reflection.FieldInfo worldSkyboxCameraInt = AccessTools.Field(typeof(RimWorld.Planet.WorldCameraManager), "worldSkyboxCameraInt");
        public static Camera worldSkyboxCameraInt_ { get { return (Camera)worldSkyboxCameraInt.GetValue(null); } }
    }
    internal static class MirrorWorldObject {
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldObject, System.Collections.Generic.List<RimWorld.Planet.WorldObjectComp>> comps = AccessTools.FieldRefAccess<RimWorld.Planet.WorldObject, System.Collections.Generic.List<RimWorld.Planet.WorldObjectComp>>("comps");
    }

    internal static class MirrorTransporters {
        public static readonly AccessTools.FieldRef<RimWorld.Planet.TravellingTransporters, RimWorld.Planet.PlanetTile> initialTile = AccessTools.FieldRefAccess<RimWorld.Planet.TravellingTransporters, RimWorld.Planet.PlanetTile>("initialTile");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.TravellingTransporters, RimWorld.Planet.PlanetTile> destinationTile = AccessTools.FieldRefAccess<RimWorld.Planet.TravellingTransporters, RimWorld.Planet.PlanetTile>("destinationTile");
    }
    internal static class WorldLayersHelper {
        private static readonly System.Reflection.FieldInfo layersField = AccessTools.Field(typeof(RimWorld.Planet.WorldRenderer), "layers");

        /// <summary>
        /// Lee el campo privado 'layers' del renderizador del mundo sin depender del tipo de capa,
        /// porque en 1.6 las clases WorldLayer_* desaparecieron del juego.
        /// Devuelve TRUE (hay que regenerar) ante cualquier duda: es el camino prudente, que es
        /// el que tomaba el codigo original cuando la comprobacion no daba false.
        /// </summary>
        public static bool ShouldRegenerate(object renderer) {
            try {
                if (layersField == null || renderer == null) return true;
                System.Collections.IList layers = layersField.GetValue(renderer) as System.Collections.IList;
                if (layers == null || layers.Count == 0) return true;
                object first = layers[0];
                if (first == null) return true;
                System.Reflection.PropertyInfo prop = first.GetType().GetProperty("ShouldRegenerate");
                if (prop == null) return true;
                return (bool)prop.GetValue(first);
            } catch (Exception) { return true; }
        }
    }
    /// <summary>
    /// Acceso a la casilla del mundo por indice. En 1.6 WorldGrid.tiles dejo de ser publico y el
    /// indice de casilla paso a ser la estructura PlanetTile, asi que se lee por reflexion y se
    /// toca el miembro "biome" del objeto que haya dentro, sea campo o propiedad.
    /// Si no encuentra el miembro, lo DICE UNA VEZ en el registro: eso convierte la duda en un dato.
    /// </summary>
    internal static class WorldGridHelper {
        private static readonly System.Reflection.FieldInfo tilesField = AccessTools.Field(typeof(RimWorld.Planet.WorldGrid), "tiles");
        private static bool avisoDado;
        private static readonly System.Reflection.BindingFlags Banderas = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        private static System.Collections.IList Tiles() {
            if (Find.World == null || tilesField == null) return null;
            return tilesField.GetValue(Find.World.grid) as System.Collections.IList;
        }

        public static object TileAt(int index) {
            System.Collections.IList t = Tiles();
            if (t == null || index < 0 || index >= t.Count) return null;
            return t[index];
        }

        private static System.Reflection.MemberInfo MiembroBioma(Type tipo) {
            System.Reflection.FieldInfo f = tipo.GetField("biome", Banderas);
            if (f != null) return f;
            return tipo.GetProperty("biome", Banderas);
        }

        private static void Avisar(Type tipo) {
            if (avisoDado) return;
            avisoDado = true;
            Log.Warning("[Universum] No se encontro el miembro 'biome' en " + (tipo == null ? "la casilla" : tipo.FullName) + ". La mascara de oceano y el bioma del generador no funcionaran: hay que revisar la API de 1.6.");
        }

        public static BiomeDef GetBiome(int index) {
            try {
                object casilla = TileAt(index);
                if (casilla == null) return null;
                System.Reflection.MemberInfo m = MiembroBioma(casilla.GetType());
                if (m == null) { Avisar(casilla.GetType()); return null; }
                System.Reflection.FieldInfo f = m as System.Reflection.FieldInfo;
                if (f != null) return (BiomeDef)f.GetValue(casilla);
                return (BiomeDef)((System.Reflection.PropertyInfo)m).GetValue(casilla);
            } catch (Exception e) { Log.Warning("[Universum] Fallo leyendo el bioma de la casilla " + index + ": " + e.Message); return null; }
        }

        public static void SetBiome(int index, BiomeDef biome) {
            try {
                System.Collections.IList lista = Tiles();
                if (lista == null || index < 0 || index >= lista.Count) return;
                object casilla = lista[index];
                if (casilla == null) return;
                System.Reflection.MemberInfo m = MiembroBioma(casilla.GetType());
                if (m == null) { Avisar(casilla.GetType()); return; }
                System.Reflection.FieldInfo f = m as System.Reflection.FieldInfo;
                if (f != null) { f.SetValue(casilla, biome); } else { ((System.Reflection.PropertyInfo)m).SetValue(casilla, biome); }
                lista[index] = casilla;
            } catch (Exception e) { Log.Warning("[Universum] Fallo fijando el bioma de la casilla " + index + ": " + e.Message); }
        }

        public static BiomeDef GetBiomeDePlanetTile(object planetTile) {
            try {
                if (planetTile == null) return null;
                Type tipo = planetTile.GetType();
                string[] nombres = new string[] { "tileId", "TileId", "Id", "id", "index", "Index" };
                for (int i = 0; i < nombres.Length; i++) {
                    System.Reflection.PropertyInfo p = tipo.GetProperty(nombres[i], Banderas);
                    if (p != null && p.PropertyType == typeof(int)) return GetBiome((int)p.GetValue(planetTile));
                    System.Reflection.FieldInfo f = tipo.GetField(nombres[i], Banderas);
                    if (f != null && f.FieldType == typeof(int)) return GetBiome((int)f.GetValue(planetTile));
                }
                Avisar(tipo);
                return null;
            } catch (Exception) { return null; }
        }
    }
}
