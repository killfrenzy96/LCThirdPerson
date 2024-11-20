using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UniVRM10;
using System.Reflection;

namespace LCThirdPerson.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class CameraPatch
    {
        private static PlayerControllerB Instance;
        private static bool TriggerAwake;
        private static int OriginalCullingMask;
        private static UnityEngine.Rendering.ShadowCastingMode OriginalShadowCastingMode;

        private static bool VrmAssemblyExists = false;
        private static GameObject VrmRootObject = null;
        private static Transform VrmHeadTransform = null;
        private static List<Renderer> VrmHeadMeshes = new List<Renderer>();

        private static bool VrmTriggerAwake = false;
        private static bool VrmHeadVisible = true;

        private static readonly string[] IgnoreGameObjectPrefixes = new[]{
            "VolumeMain"
        };

        public static void OnEnable()
        {
            ThirdPersonPlugin.Log.LogInfo("Enabled");

            var visor = Instance.localVisor;
            var playerModel = Instance.thisPlayerModel;

            // Hide the visor
            // visor.gameObject.SetActive(false);
            var visorRenderers = visor.GetComponentInChildren<MeshRenderer>();
            if (visorRenderers) visorRenderers.enabled = false;

            if (ThirdPersonPlugin.Instance.FirstPersonVrm.Value) SetVrmHeadVisibility(true);

            // Show the player model
            playerModel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // Hide the player arms
            Instance.thisPlayerModelArms.enabled = false;

            // Set culling mask to see model's layer
            Instance.gameplayCamera.cullingMask = OriginalCullingMask | (1 << 23);

            // Increase the grab distance
            Instance.grabDistance = Math.Max(5f - ThirdPersonPlugin.Instance.Offset.Value.z, 5);
        }

        public static void OnDisable()
        {
            ThirdPersonPlugin.Log.LogInfo("Disabled");

            var visor = Instance.localVisor;
            var playerModel = Instance.thisPlayerModel;

            // Show the visor
            // visor.gameObject.SetActive(true);
            var visorRenderers = visor.GetComponentInChildren<MeshRenderer>();
            if (visorRenderers) visorRenderers.enabled = !ThirdPersonPlugin.Instance.AlwaysHideVisor.Value;

            if (ThirdPersonPlugin.Instance.FirstPersonVrm.Value && SetVrmHeadVisibility(false))
            {
                // Show the player model
                playerModel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                // Hide the player arms
                Instance.thisPlayerModelArms.enabled = false;

                // Set culling mask to see model's layer
                Instance.gameplayCamera.cullingMask = OriginalCullingMask | (1 << 23);
            }
            else
            {
                // Hide the player model
                playerModel.shadowCastingMode = OriginalShadowCastingMode;

                // Show the arms
                Instance.thisPlayerModelArms.enabled = true;

                // Hide the models' layer again
                Instance.gameplayCamera.cullingMask = OriginalCullingMask;
            }

            // Reset the grab distance
            Instance.grabDistance = 5f;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Awake()
        {
            TriggerAwake = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void PatchUpdate(ref PlayerControllerB __instance, ref bool ___isCameraDisabled, ref bool ___isPlayerControlled)
        {
            // First person VRM specific updates
            if (ThirdPersonPlugin.Instance.FirstPersonVrm.Value && Instance != null && VrmAssemblyExists)
            {
                // Initialize VRM
                VrmInit();

                // Hide head if it is too close to the camera
                if (ThirdPersonPlugin.Camera != null && VrmHeadTransform != null)
                {
                    bool showHead;
                    if (
                        ThirdPersonPlugin.Instance.Enabled == true || Instance.isPlayerDead ||
                        Vector3.Distance(VrmHeadTransform.position, ThirdPersonPlugin.Camera.position) >
                        ThirdPersonPlugin.Instance.FirstPersonVrmHeadHideDistance.Value
                    )
                    {
                        showHead = true;
                    }
                    else
                    {
                        showHead = false;
                    }

                    if (VrmHeadVisible != showHead || VrmTriggerAwake)
                    {
                        // Show/hide head
                        SetVrmHeadVisibility(showHead);

                        // Show the player model
                        Instance.thisPlayerModel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                        // Hide the player arms
                        Instance.thisPlayerModelArms.enabled = false;

                        // Set culling mask to see model's layer
                        Instance.gameplayCamera.cullingMask = OriginalCullingMask | (1 << 23);

                        VrmTriggerAwake = false;
                    }
                }
            }

            if (!___isPlayerControlled || ___isCameraDisabled)
            {
                return;
            }

            if (TriggerAwake)
            {
                Instance = __instance;
                OriginalCullingMask = Instance.gameplayCamera.cullingMask;
                OriginalShadowCastingMode = Instance.thisPlayerModel.shadowCastingMode;
                ThirdPersonPlugin.Instance.OnEnable.AddListener(OnEnable);
                ThirdPersonPlugin.Instance.OnDisable.AddListener(OnDisable);

                if (ThirdPersonPlugin.Instance.Enabled)
                {
                    OnEnable();
                }
                else
                { 
                    OnDisable();
                }

                // Check if VRM10 is loaded
                Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
                bool vrmFound = false;
                bool uniHumanoidFound = false;
                foreach (Assembly assem in assems)
                {
                    if (assem.FullName.StartsWith("VRM10,"))
                    {
                        vrmFound = true;
                    }
                    else if (assem.FullName.StartsWith("UniHumanoid,"))
                    {
                        uniHumanoidFound = true;
                    }
                }

                if (vrmFound && uniHumanoidFound)
                {
                    VrmAssemblyExists = true;
                }

                TriggerAwake = false;
                VrmTriggerAwake = true;
            }

            if (Instance == null)
            {
                return;
            }

            if (ThirdPersonPlugin.OriginalTransform == null)
            {
                ThirdPersonPlugin.OriginalTransform = CopyTransform(Instance.gameplayCamera.transform, "LCThirdPerson_Original Camera Position");
                ThirdPersonPlugin.Camera = CopyTransform(Instance.gameplayCamera.transform, "LCThirdPerson_Camera Position");
            }

            // Set this for any method that needs patching inbetween the start of Update and the end of LateUpdate
            ThirdPersonPlugin.Camera.position = Instance.gameplayCamera.transform.position;
            ThirdPersonPlugin.Camera.rotation = Instance.gameplayCamera.transform.rotation;

            // Reset the camera before the PlayerController update method, so nothing gets too messed up
            if (!Instance.isClimbingLadder && !Instance.inVehicleAnimation)
            {
                // Instance.gameplayCamera.transform.rotation = ThirdPersonPlugin.OriginalTransform.transform.rotation;
                Instance.gameplayCamera.transform.position = ThirdPersonPlugin.OriginalTransform.transform.position;
            }

            // Don't check for toggle key if in terminal menu or typing in chat
            if (Instance.inTerminalMenu || Instance.isTypingChat)
            {
                return;
            }

            ThirdPersonPlugin.Instance.CheckEnable();
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void PatchCamera(ref bool ___isCameraDisabled, ref bool ___isPlayerControlled)
        {
            var originalTransform = ThirdPersonPlugin.OriginalTransform;

            if (!___isPlayerControlled || ___isCameraDisabled || originalTransform == null)
            {
                return;
            }

            // Move camera forward/back to avoid head better
            var forwardOffset = originalTransform.up;
            forwardOffset.y = 0f;
            forwardOffset *= ThirdPersonPlugin.Instance.CameraLookDownOffset.Value;

            var gameplayCamera = Instance.gameplayCamera;

            // Set the placeholder rotation to match the updated gameplayCamera rotation
            // originalTransform.transform.rotation = gameplayCamera.transform.rotation;

            bool fixIntersectRay = false;

            if (!ThirdPersonPlugin.Instance.Enabled || Instance.inTerminalMenu)
            {
                // Set the camera look down offset for first person VRM
                if (ThirdPersonPlugin.Instance.FirstPersonVrm.Value)
                {
                    gameplayCamera.transform.position = originalTransform.transform.position + forwardOffset;
                    fixIntersectRay = true;
                }
            }
            else
            {
                var offset = originalTransform.transform.right * ThirdPersonPlugin.Instance.Offset.Value.x +
                    originalTransform.transform.up * ThirdPersonPlugin.Instance.Offset.Value.y;
                var lineStart = originalTransform.transform.position;
                var lineEnd = originalTransform.transform.position + forwardOffset + offset + gameplayCamera.transform.forward * ThirdPersonPlugin.Instance.Offset.Value.z;

                // Check for camera collisions
                if (Physics.Linecast(lineStart, lineEnd, out RaycastHit hit, StartOfRound.Instance.collidersAndRoomMask) && !IgnoreCollision(hit.transform.name))
                {
                    offset += gameplayCamera.transform.forward * -Mathf.Max(hit.distance, 0);
                }
                else
                {
                    offset += gameplayCamera.transform.forward * ThirdPersonPlugin.Instance.Offset.Value.z;
                }

                // Limit height movement by camera
                offset.y = Math.Min(offset.y, ThirdPersonPlugin.Instance.CameraMaxHeight.Value);

                // Set the camera offset
                gameplayCamera.transform.position = originalTransform.transform.position + forwardOffset + offset;

                fixIntersectRay = true;
            }

            // Don't fix interact ray if on a ladder
            if (Instance.isClimbingLadder)
            {
                fixIntersectRay = false;
            }

            // Fix the interact ray
            if (fixIntersectRay)
            {
                var methodInfo = typeof(PlayerControllerB).GetMethod(
                    "SetHoverTipAndCurrentInteractTrigger",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                methodInfo.Invoke(Instance, new object[] { });
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetFaceUnderwaterFilters")]
        private static void UnderwaterPrepatch()
        {
            if (Instance == null || ThirdPersonPlugin.Camera == null)
            {
                return;
            }

            Instance.gameplayCamera.transform.position = ThirdPersonPlugin.Camera.transform.position;
            // Instance.gameplayCamera.transform.rotation = ThirdPersonPlugin.Camera.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetFaceUnderwaterFilters")]
        private static void UnderwaterPostpatch()
        {
            if (Instance == null || ThirdPersonPlugin.OriginalTransform == null)
            {
                return;
            }

            Instance.gameplayCamera.transform.position = ThirdPersonPlugin.OriginalTransform.transform.position;
            // Instance.gameplayCamera.transform.rotation = ThirdPersonPlugin.OriginalTransform.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static void SpawnPlayerPostpatch(ref bool ___isPlayerControlled)
        {
            if (Instance == null || !___isPlayerControlled)
            {
                return;
            }

            if (!ThirdPersonPlugin.Instance.Enabled)
            {
                return;
            }

            OnEnable();
        }

        private static Transform CopyTransform(Transform copyTransform, string gameObjectName)
        {
            var newTransform = new GameObject(gameObjectName).transform;
            newTransform.position = copyTransform.position;
            newTransform.rotation = copyTransform.rotation;
            newTransform.parent = copyTransform.parent;

            return newTransform;
        }

        private static bool IgnoreCollision(string name)
        {
            return IgnoreGameObjectPrefixes.Any(prefix => name.StartsWith(prefix));
        }

        private static void VrmInit()
        {
            if (VrmRootObject != null) return;

            // Reset variables
            VrmHeadVisible = true;
            VrmTriggerAwake = true;

            // Generate VRM object name to search for
            var steamId = SteamClient.SteamId;
            var steamName = SteamClient.Name;

            if (steamName == null) return;
            var vrmObjectName = "LethalVRM Character Model " + steamName + " " + steamId;

            // Search root objects for the local VRM model
            GameObject[] rootObjects = Instance.localVisor.gameObject.scene.GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.name.EndsWith(vrmObjectName))
                {
                    VrmRootObject = obj;
                    break;
                }
            }

            if (VrmRootObject == null) return;

            // Find first person renderers and head bone
            var vrmInstance = VrmRootObject.GetComponent<Vrm10Instance>();

            // Get head bone
            if (vrmInstance == null || vrmInstance.Humanoid == null)
            {
                VrmHeadTransform = RecursiveFindChild(VrmRootObject.transform, "Head");
            }
            else
            {
                VrmHeadTransform = vrmInstance.Humanoid.Head;
            }

            // Get head renderers
            VrmHeadMeshes.Clear();
            if (vrmInstance != null && vrmInstance.Vrm != null && vrmInstance.Vrm.FirstPerson != null && vrmInstance.Vrm.FirstPerson.Renderers != null)
            {
                foreach (var rendererFlag in vrmInstance.Vrm.FirstPerson.Renderers)
                {
                    string rendererName = rendererFlag.Renderer;
                    bool rendererIsHead = rendererFlag.FirstPersonFlag == UniGLTF.Extensions.VRMC_vrm.FirstPersonType.thirdPersonOnly;
                    
                    ThirdPersonPlugin.Log.LogInfo($"{rendererFlag.Renderer} - {rendererFlag.FirstPersonFlag}");

                    if (rendererIsHead)
                    {
                        var renderer = rendererFlag.GetRenderer(VrmRootObject.transform);
                        if (renderer != null)
                        {
                            VrmHeadMeshes.Add(renderer);
                        }
                    }
                }
            }

            ThirdPersonPlugin.Log.LogInfo($"Local VRM Model Initialized");
        }

        private static bool SetVrmHeadVisibility(bool visible)
        {
            if (!VrmAssemblyExists) return false;
            VrmInit();
            if (VrmRootObject == null) return false;

            if (VrmHeadMeshes.Count > 0)
            {
                UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                foreach (var renderer in VrmHeadMeshes)
                {
                    renderer.shadowCastingMode = shadowCastingMode;
                }
            }
            else
            {
                Vector3 scale = visible ? new Vector3(1f, 1f, 1f) : new Vector3(0f, 0f, 0f);
                VrmHeadTransform.localScale = scale;
            }

            VrmHeadVisible = visible;
            return true;
        }

        private static Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower() == childName.ToLower())
                {
                    return child;
                }
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
