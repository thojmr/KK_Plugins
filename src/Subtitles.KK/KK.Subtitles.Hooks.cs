﻿using ActionGame.Communication;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            internal static void PlayVoice(LoadAudioBase __instance)
            {
                if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;

                if (HSceneProcInstance != null)
                    Caption.DisplayHSubtitle(__instance);
                else if (ActionGameInfoInstance != null && GameObject.Find("ActionScene/ADVScene") == null)
                    Caption.DisplayDialogueSubtitle(__instance);
                else if (SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                    Caption.DisplaySubtitle(__instance.gameObject, text);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
            internal static void InfoInit(Info __instance) => ActionGameInfoInstance = __instance;

            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
            internal static void HVoiceCtrlInit() => HSceneProcInstance = FindObjectOfType<HSceneProc>();
        }
    }
}