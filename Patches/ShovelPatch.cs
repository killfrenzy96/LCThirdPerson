using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCThirdPerson.Patches
{
    [HarmonyPatch(typeof(Shovel))]
    internal class ShovelPatch
    {
        private static Vector3 originalPosition;
        private static Quaternion originalRotation;

        [HarmonyPrefix]
        [HarmonyPatch("HitShovel")]
        private static void HitShovelPrepatch(Shovel __instance, ref PlayerControllerB ___previousPlayerHeldBy)
        {
            if (ThirdPersonPlugin.Camera == null || ___previousPlayerHeldBy == null || ___previousPlayerHeldBy.gameplayCamera == null)
            {
                return;
            }

            originalPosition = ___previousPlayerHeldBy.gameplayCamera.transform.position;
            originalRotation = ___previousPlayerHeldBy.gameplayCamera.transform.rotation;
            ___previousPlayerHeldBy.gameplayCamera.transform.position = ThirdPersonPlugin.OriginalTransform.transform.position;
            ___previousPlayerHeldBy.gameplayCamera.transform.rotation = ThirdPersonPlugin.OriginalTransform.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch("HitShovel")]
        private static void HitShovelPostpatch(Shovel __instance, ref PlayerControllerB ___previousPlayerHeldBy)
        {
            if (ThirdPersonPlugin.OriginalTransform == null || ___previousPlayerHeldBy == null || ___previousPlayerHeldBy.gameplayCamera == null)
            {
                return;
            }

            ___previousPlayerHeldBy.gameplayCamera.transform.position = originalPosition;
            ___previousPlayerHeldBy.gameplayCamera.transform.rotation = originalRotation;
        }
    }
}
