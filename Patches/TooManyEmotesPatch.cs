using HarmonyLib;
using TooManyEmotes.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace LCThirdPerson.Patches
{
    [HarmonyPatch(typeof(ThirdPersonEmoteController))]
    internal class TooManyEmotesPatch
    {
        public static bool isPerformingEmote = false;
        public static bool firstPersonEmotesEnabled = false;

        [HarmonyPrefix]
        [HarmonyPatch("OnStartCustomEmoteLocal")]
        private static void OnStartCustomEmoteLocalPrepatch() // Called before starting emote
        {
            if (ThirdPersonEmoteController.firstPersonEmotesEnabled) ThirdPersonPlugin.Instance.ForceEnabled(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStartCustomEmoteLocal")]
        private static void OnStartCustomEmoteLocalPostpatch() // Called after starting emote
        {
            isPerformingEmote = true;
            firstPersonEmotesEnabled = ThirdPersonEmoteController.firstPersonEmotesEnabled;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStopCustomEmoteLocal")]
        private static void OnStopCustomEmoteLocalPostpatch() // Called after ending emote and resetting
        {
            isPerformingEmote = false;
            firstPersonEmotesEnabled = ThirdPersonEmoteController.firstPersonEmotesEnabled;
            ThirdPersonPlugin.Instance.ForceEnabled(ThirdPersonPlugin.Instance.Enabled);
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateFirstPersonEmoteMode")]
        private static void UpdateFirstPersonEmoteModePostpatch(bool value) // Called after starting emote
        {
            firstPersonEmotesEnabled = ThirdPersonEmoteController.firstPersonEmotesEnabled;
            FixCamera();
        }

        public static void FixCamera()
        {
            if (isPerformingEmote)
            {
                if (ThirdPersonPlugin.Instance.Enabled)
                {
                    ThirdPersonPlugin.Instance.ForceEnabled(true);
                }
                else
                {
                    ThirdPersonPlugin.Instance.ForceEnabled(!firstPersonEmotesEnabled);
                }
            }
        }
    }
}
