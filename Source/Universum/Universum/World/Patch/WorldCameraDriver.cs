using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse.Steam;
using Verse;

namespace Universum.World.Patch {
    public class WorldCameraDriver {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_AltitudePercent)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_MinAltitude)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_CurrentZoom)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_WorldCameraDriverOnGUI)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_Update)).Patch();
        }

        [HarmonyPatch]
        static class WorldCameraDriver_AltitudePercent {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_AltitudePercent");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance, ref float __result) {
                __result = Mathf.InverseLerp(RimWorld.Planet.WorldCameraDriver.MinAltitude, CameraInfo.maxAltitude, __instance.altitude);
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_MinAltitude {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_MinAltitude");

            public static bool Prefix(ref float __result) {
                __result = (float) (CameraInfo.minAltitude + (SteamDeck.IsSteamDeck ? 17.0 : 25.0));
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_CurrentZoom {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_CurrentZoom");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance, ref RimWorld.Planet.WorldCameraZoomRange __result) {
                float altitudePercent = __instance.AltitudePercent;
                if ((double) altitudePercent < 0.025 * CameraInfo.zoomEnumMultiplier) {
                    __result = RimWorld.Planet.WorldCameraZoomRange.VeryClose;
                    return false;
                }
                if ((double) altitudePercent < 0.042 * CameraInfo.zoomEnumMultiplier) {
                    __result = RimWorld.Planet.WorldCameraZoomRange.Close;
                    return false;
                }
                __result = (double) altitudePercent < (0.125 * CameraInfo.zoomEnumMultiplier) ? RimWorld.Planet.WorldCameraZoomRange.Far : RimWorld.Planet.WorldCameraZoomRange.VeryFar;
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_WorldCameraDriverOnGUI {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:WorldCameraDriverOnGUI");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance) {
                _UpdateReleasedLeftWhileHoldingMiddle(ref __instance);
                _UpdateMouseCoveredByUI(ref __instance);

                if (WorldCameraMirror.AnythingPreventsCameraMotion(__instance)) {
                    return false;
                }

                _HandleMouseDrag(ref __instance);
                _HandleScrollWheelAndZoom(ref __instance);
                _HandleKeyMovements(ref __instance);

                __instance.config.ConfigOnGUI();

                return false;
            }

            private static void _UpdateReleasedLeftWhileHoldingMiddle(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (Input.GetMouseButtonUp(0) && Input.GetMouseButton(2)) {
                    WorldCameraMirror.releasedLeftWhileHoldingMiddle(__instance) = true;
                } else if (Event.current.rawType == EventType.MouseDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) {
                    WorldCameraMirror.releasedLeftWhileHoldingMiddle(__instance) = false;
                }
            }

            private static void _UpdateMouseCoveredByUI(ref RimWorld.Planet.WorldCameraDriver __instance) {
                WorldCameraMirror.mouseCoveredByUI(__instance) = Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) != null;
            }

            private static void _HandleMouseDrag(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (!UnityGUIBugsFixer.IsSteamDeckOrLinuxBuild && Event.current.type == EventType.MouseDrag && Event.current.button == 2 ||
                    UnityGUIBugsFixer.IsSteamDeckOrLinuxBuild && Input.GetMouseButton(2) &&
                    (!SteamDeck.IsSteamDeck || !Find.WorldSelector.AnyCaravanSelected)) {
                    Vector2 currentEventDelta = UnityGUIBugsFixer.CurrentEventDelta;

                    if (Event.current.type == EventType.MouseDrag) {
                        Event.current.Use();
                    }

                    if (currentEventDelta != Vector2.zero) {
                        RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.FrameInteraction);

                        currentEventDelta.x *= -1f;
                        WorldCameraMirror.desiredRotationRaw(__instance) += currentEventDelta / RimWorld.Planet.GenWorldUI.CurUITileSize() * 0.273f * (Prefs.MapDragSensitivity * CameraInfo.dragSensitivityMultiplier);
                    }
                }
            }

            private static void _HandleScrollWheelAndZoom(ref RimWorld.Planet.WorldCameraDriver __instance) {
                float num = 0.0f;

                if (Event.current.type == EventType.ScrollWheel) {
                    num -= Event.current.delta.y * 0.1f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapZoom_In.KeyDownEvent) {
                    num += 2f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapZoom_Out.KeyDownEvent) {
                    num -= 2f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                WorldCameraMirror.desiredAltitude(__instance) -= num * (__instance.config.zoomSpeed * CameraInfo.zoomSensitivityMultiplier) * __instance.altitude / 12.0f;
                WorldCameraMirror.desiredAltitude(__instance) = Mathf.Clamp(WorldCameraMirror.desiredAltitude(__instance), RimWorld.Planet.WorldCameraDriver.MinAltitude, CameraInfo.maxAltitude);
            }

            private static void _HandleKeyMovements(ref RimWorld.Planet.WorldCameraDriver __instance) {
                WorldCameraMirror.desiredRotation(__instance) = Vector2.zero;

                if (RimWorld.KeyBindingDefOf.MapDolly_Left.IsDown) {
                    WorldCameraMirror.desiredRotation(__instance).x = -__instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Right.IsDown) {
                    WorldCameraMirror.desiredRotation(__instance).x = __instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Up.IsDown) {
                    WorldCameraMirror.desiredRotation(__instance).y = __instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Down.IsDown) {
                    WorldCameraMirror.desiredRotation(__instance).y = -__instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_Update {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:Update");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (LongEventHandler.ShouldWaitForEvent)
                    return false;
                if (Find.World == null) {
                    WorldCameraMirror.MyCamera(__instance).gameObject.SetActive(false);
                } else {
                    if (!Find.WorldInterface.everReset)
                        Find.WorldInterface.Reset();
                    Vector2 curInputDollyVect = (Vector2)WorldCameraMirror.CalculateCurInputDollyVect(__instance);
                    if (curInputDollyVect != Vector2.zero) {
                        float num = (float) (((double) __instance.altitude - (double) RimWorld.Planet.WorldCameraDriver.MinAltitude) / (CameraInfo.maxAltitude - (double) RimWorld.Planet.WorldCameraDriver.MinAltitude) * 0.850000023841858 + 0.150000005960464);
                        WorldCameraMirror.rotationVelocity(__instance) = new Vector2(curInputDollyVect.x, curInputDollyVect.y) * num;
                    }
                    if ((!Input.GetMouseButton(2) || SteamDeck.IsSteamDeck && WorldCameraMirror.releasedLeftWhileHoldingMiddle(__instance)) && WorldCameraMirror.dragTimeStamps(__instance).Any()) {
                        WorldCameraMirror.rotationVelocity(__instance) += CameraDriver.GetExtraVelocityFromReleasingDragButton(WorldCameraMirror.dragTimeStamps(__instance), 5f * CameraInfo.dragVelocityMultiplier);
                        WorldCameraMirror.dragTimeStamps(__instance).Clear();
                    }
                    if (!WorldCameraMirror.AnythingPreventsCameraMotion(__instance)) {
                        float num = Time.deltaTime * CameraDriver.HitchReduceFactor;
                        __instance.sphereRotation *= Quaternion.AngleAxis(WorldCameraMirror.rotationVelocity(__instance).x * num * __instance.config.rotationSpeedScale, WorldCameraMirror.MyCamera(__instance).transform.up);
                        __instance.sphereRotation *= Quaternion.AngleAxis(-WorldCameraMirror.rotationVelocity(__instance).y * num * __instance.config.rotationSpeedScale, WorldCameraMirror.MyCamera(__instance).transform.right);
                        if (WorldCameraMirror.desiredRotationRaw(__instance) != Vector2.zero) {
                            __instance.sphereRotation *= Quaternion.AngleAxis(WorldCameraMirror.desiredRotationRaw(__instance).x, WorldCameraMirror.MyCamera(__instance).transform.up);
                            __instance.sphereRotation *= Quaternion.AngleAxis(-WorldCameraMirror.desiredRotationRaw(__instance).y, WorldCameraMirror.MyCamera(__instance).transform.right);
                        }
                        WorldCameraMirror.dragTimeStamps(__instance).Add(new CameraDriver.DragTimeStamp() {
                            posDelta = WorldCameraMirror.desiredRotationRaw(__instance),
                            time = Time.time
                        });
                    }
                    WorldCameraMirror.desiredRotationRaw(__instance) = Vector2.zero;
                    int num1 = Gen.FixedTimeStepUpdate(ref WorldCameraMirror.fixedTimeStepBuffer(__instance), 60f);
                    for (int index = 0; index < num1; ++index) {
                        if (WorldCameraMirror.rotationVelocity(__instance) != Vector2.zero) {
                            WorldCameraMirror.rotationVelocity(__instance) *= __instance.config.camRotationDecayFactor;
                            if ((double) WorldCameraMirror.rotationVelocity(__instance).magnitude < 0.0500000007450581)
                                WorldCameraMirror.rotationVelocity(__instance) = Vector2.zero;
                        }
                        if (__instance.config.smoothZoom) {
                            float num2 = Mathf.Lerp(__instance.altitude, WorldCameraMirror.desiredAltitude(__instance), 0.05f);
                            WorldCameraMirror.desiredAltitude(__instance) += (num2 - __instance.altitude) * __instance.config.zoomPreserveFactor;
                            __instance.altitude = num2;
                        } else {
                            float num2 = (float) (((double) WorldCameraMirror.desiredAltitude(__instance) - (double) __instance.altitude) * 0.400000005960464);
                            WorldCameraMirror.desiredAltitude(__instance) += __instance.config.zoomPreserveFactor * num2;
                            __instance.altitude += num2;
                        }
                    }
                    WorldCameraMirror.rotationAnimation_lerpFactor(__instance) += Time.deltaTime * 8f;
                    if (Find.PlaySettings.lockNorthUp) {
                        __instance.RotateSoNorthIsUp(false);
                        object[] clampArgs = new object[] { __instance.sphereRotation };
                WorldCameraMirror.ClampXRotation(__instance, clampArgs);
                __instance.sphereRotation = (Quaternion)clampArgs[0];
                    }
                    for (int index = 0; index < num1; ++index)
                        __instance.config.ConfigFixedUpdate_60(ref WorldCameraMirror.rotationVelocity(__instance));
                    WorldCameraMirror.ApplyPositionToGameObject(__instance);
                }
                return false;
            }
        }
    }
}
