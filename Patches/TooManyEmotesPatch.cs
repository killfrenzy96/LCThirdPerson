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

        [HarmonyPostfix]
        [HarmonyPatch("OnStartCustomEmoteLocal")]
        private static void OnStartCustomEmoteLocalPrepatch() // Called when starting emote
        {
            isPerformingEmote = true;
            firstPersonEmotesEnabled = ThirdPersonEmoteController.firstPersonEmotesEnabled;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStopCustomEmoteLocal")]
        private static void OnStopCustomEmoteLocalPrepatch() // Called when ending emote and resetting
        {
            isPerformingEmote = false;
            firstPersonEmotesEnabled = ThirdPersonEmoteController.firstPersonEmotesEnabled;
            FixCamera();
        }

        private static void FixCamera()
        {
            if (ThirdPersonPlugin.Instance == null) return;
            ThirdPersonPlugin.Instance.SetEnabled(!ThirdPersonPlugin.Instance.Enabled);
            ThirdPersonPlugin.Instance.SetEnabled(!ThirdPersonPlugin.Instance.Enabled);
        }
    }
}
