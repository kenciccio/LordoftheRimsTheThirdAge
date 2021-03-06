﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;

namespace TheThirdAge
{
    /// <summary>
    /// Original code by Xen https://github.com/XenEmpireAdmin
    /// Adjustments by Jecrell https://github.com/Jecrell
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MainMenuTex
    {
        static readonly bool debug = false;

        public static Texture2D BGMain;

        static MainMenuTex()
        {
            if (!ModStuff.Settings.LimitTechnology)
            {
                return;
            }

            LoadTextures();
        }

        private static void LoadTextures()
        {
            BGMain = ContentFinder<Texture2D>.Get("UI/HeroArt/TTABGPlanet", true);

            try
            {
                Traverse.CreateWithType("UI_BackgroundMain").Field("BGPlanet").SetValue(BGMain);
            }
            catch
            {
                if (debug)
                {
                    Log.Message("Failed to Traverse BGPlanet");
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    internal static class SwapMainMenuGraphics
    {
        static readonly bool debug = false;

        static SwapMainMenuGraphics()
        {
            if (!ModStuff.Settings.LimitTechnology)
            {
                return;
            }

            var UI_BackgroundMainPatch = new Harmony("TTA.MainMenu.UI_BackgroundMainPatch");
            MethodInfo methInfBackgroundOnGUI = AccessTools.Method(typeof(UI_BackgroundMain), "BackgroundOnGUI", null, null);
            var harmonyMethodPreFBackgroundOnGUI = new HarmonyMethod(typeof(SwapMainMenuGraphics).GetMethod("PreFBackgroundOnGUI"));
            UI_BackgroundMainPatch.Patch(methInfBackgroundOnGUI, harmonyMethodPreFBackgroundOnGUI, null, null);
            if (debug)
            {
                Log.Message("UI_BackgroundMainPatch initialized");
            }
        }
        public static bool PreFBackgroundOnGUI()
        {
            if (!ModStuff.Settings.LimitTechnology)
            {
                return true;
            }
            // Shape the BG
            var floRatio = UI.screenWidth / 2048f;
            var floHeight = 1280f * floRatio;
            var floYPos = (UI.screenHeight - floHeight) / 2f;
            var rectBG = new Rect(0f, floYPos, UI.screenWidth, floHeight);

            // Draw the BG
            GUI.DrawTexture(rectBG, Traverse.Create(typeof(UI_BackgroundMain)).Field("BGPlanet").GetValue<Texture2D>(), ScaleMode.ScaleToFit);

            return false;
        }
    }
}
