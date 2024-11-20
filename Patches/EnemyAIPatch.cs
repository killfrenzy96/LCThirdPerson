using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace LCThirdPerson.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("EnableEnemyMesh")]
        private static void EnableEnemyMeshPostfix(ref EnemyAI __instance, bool enable, bool overrideDoNotSet)
        {
            if (__instance is not DressGirlAI) return;

            for (int i = 0; i < __instance.skinnedMeshRenderers.Length; i++)
            {
                if (!__instance.skinnedMeshRenderers[i].CompareTag("DoNotSet") || overrideDoNotSet)
                {
                    __instance.skinnedMeshRenderers[i].forceRenderingOff = !enable;
                }
            }

            for (int j = 0; j < __instance.meshRenderers.Length; j++)
            {
                if (!__instance.meshRenderers[j].CompareTag("DoNotSet") || overrideDoNotSet)
                {
                    __instance.meshRenderers[j].forceRenderingOff = !enable;
                }
            }
        }
    }
}
