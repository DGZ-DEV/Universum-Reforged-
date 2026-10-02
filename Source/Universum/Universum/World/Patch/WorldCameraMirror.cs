using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace Universum.World.Patch {
    /// <summary>
    /// Espejo de los miembros PRIVADOS de RimWorld.Planet.WorldCameraDriver.
    /// En 1.6 pasaron de publicos a privados; siguen existiendo con el mismo nombre,
    /// asi que se accede a ellos por reflexion en vez de reescribir la logica del mod.
    /// FieldRefAccess devuelve una referencia, de modo que la misma llamada sirve para
    /// leer, escribir y pasar el campo a un parametro ref.
    /// </summary>
    internal static class WorldCameraMirror {
        private static readonly Type T = typeof(RimWorld.Planet.WorldCameraDriver);

        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, Vector2> rotationVelocity = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, Vector2>("rotationVelocity");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, Vector2> desiredRotationRaw = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, Vector2>("desiredRotationRaw");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, Vector2> desiredRotation = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, Vector2>("desiredRotation");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, float> desiredAltitude = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, float>("desiredAltitude");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, float> fixedTimeStepBuffer = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, float>("fixedTimeStepBuffer");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, float> rotationAnimation_lerpFactor = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, float>("rotationAnimation_lerpFactor");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, bool> releasedLeftWhileHoldingMiddle = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, bool>("releasedLeftWhileHoldingMiddle");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, bool> mouseCoveredByUI = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, bool>("mouseCoveredByUI");
        public static readonly AccessTools.FieldRef<RimWorld.Planet.WorldCameraDriver, List<CameraDriver.DragTimeStamp>> dragTimeStamps = AccessTools.FieldRefAccess<RimWorld.Planet.WorldCameraDriver, List<CameraDriver.DragTimeStamp>>("dragTimeStamps");

        private static readonly System.Reflection.MethodInfo getMyCamera = AccessTools.PropertyGetter(T, "MyCamera");
        private static readonly System.Reflection.MethodInfo getAnythingPrevents = AccessTools.PropertyGetter(T, "AnythingPreventsCameraMotion");
        private static readonly System.Reflection.MethodInfo calculateCurInputDollyVect = AccessTools.Method(T, "CalculateCurInputDollyVect");
        private static readonly System.Reflection.MethodInfo clampXRotation = AccessTools.Method(T, "ClampXRotation");
        private static readonly System.Reflection.MethodInfo applyPositionToGameObject = AccessTools.Method(T, "ApplyPositionToGameObject");

        public static Camera MyCamera(RimWorld.Planet.WorldCameraDriver d) { return (Camera)getMyCamera.Invoke(d, null); }
        public static bool AnythingPreventsCameraMotion(RimWorld.Planet.WorldCameraDriver d) { return (bool)getAnythingPrevents.Invoke(d, null); }
        public static object CalculateCurInputDollyVect(RimWorld.Planet.WorldCameraDriver d) { return calculateCurInputDollyVect.Invoke(d, null); }
        public static void ClampXRotation(RimWorld.Planet.WorldCameraDriver d, object[] args) { clampXRotation.Invoke(d, args); }
        public static void ApplyPositionToGameObject(RimWorld.Planet.WorldCameraDriver d) { applyPositionToGameObject.Invoke(d, null); }
    }
}