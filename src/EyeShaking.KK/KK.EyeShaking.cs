﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System;
using UniRx;

namespace KK_Plugins
{
    /// <summary>
    /// Adds shaking to a character's eye highlights when she is a virgin in an H scene
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class EyeShaking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.eyeshaking";
        public const string PluginName = "Eye Shaking";
        public const string PluginNameInternal = Constants.Prefix + "_EyeShaking";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            CharacterApi.RegisterExtraBehaviour<EyeShakingController>(GUID);

            Enabled = Config.Bind("Config", "Enabled", true, "When enabled, virgins in H scenes will appear to have shaking eye highlights");

            //Patch the VR version of these methods via reflection
            Type VRHSceneType = Type.GetType("VRHScene, Assembly-CSharp");
            if (VRHSceneType != null)
            {
                harmony.Patch(VRHSceneType.GetMethod("MapSameObjectDisable", AccessTools.all), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.MapSameObjectDisableVR), AccessTools.all)), null);
                harmony.Patch(VRHSceneType.GetMethod("EndProc", AccessTools.all), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.EndProcVR), AccessTools.all)), null);
            }

            if (StudioAPI.InsideStudio)
                RegisterStudioControls();
        }

        private static EyeShakingController GetController(ChaControl character) => character == null ? null : character.gameObject.GetComponent<EyeShakingController>();
        private static EyeShakingController GetSelectedStudioController()
        {
            var mpCharCtrl = FindObjectOfType<MPCharCtrl>();
            if (mpCharCtrl == null || mpCharCtrl.ociChar == null || mpCharCtrl.ociChar.charInfo == null)
                return null;
            return mpCharCtrl.ociChar.charInfo.GetComponent<EyeShakingController>();
        }

        private static void RegisterStudioControls()
        {
            var invisibleSwitch = new CurrentStateCategorySwitch("Shaking Eye Highlights", controller => controller.charInfo.GetComponent<EyeShakingController>().EyeShaking);
            invisibleSwitch.Value.Subscribe(Observer.Create((bool value) =>
            {
                var controller = GetSelectedStudioController();
                if (controller != null)
                    controller.EyeShaking = value;
            }));

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(invisibleSwitch);
        }
    }
}