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
}
