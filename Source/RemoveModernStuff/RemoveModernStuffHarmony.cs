﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheThirdAge
{
    [StaticConstructorOnStartup]
    public static class RemoveModernStuffHarmony
    {
        private const int START_DATE = 3001;

        static RemoveModernStuffHarmony()
        {
            if (!ModStuff.Settings.LimitTechnology)
            {
                return;
            }

            var harmony = new Harmony(id: "rimworld.removemodernstuff");
            //Log.Message("IsTravelingInTransportPodWorldObject");
            harmony.Patch(original: AccessTools.Method(type: typeof(PawnUtility), name: "IsTravelingInTransportPodWorldObject"),
                prefix: new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(IsTravelingInTransportPodWorldObject)), postfix: null);


            //Changes the starting date of RimWorld.
            //Log.Message("StartingYear");
            harmony.Patch(AccessTools.Property(typeof(TickManager), "StartingYear").GetGetMethod(), null,
                new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(StartingYear_PostFix)), null);
            //Log.Message("Year");
            harmony.Patch(AccessTools.Method(typeof(GenDate), "Year"), null,
                new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(Year_PostFix)), null);

            //Replaces date string to include 'The Third Age'
            //Log.Message("DateFullStringAt");
            harmony.Patch(AccessTools.Method(typeof(GenDate), "DateFullStringAt"), null,
                new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(DateFullStringAt_PostFix)), null);
            //Log.Message("DateReadoutStringAt");
            harmony.Patch(AccessTools.Method(typeof(GenDate), "DateReadoutStringAt"), null,
                new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(DateReadoutStringAt_PostFix)), null);

            //Log.Message("RandomViolenceDamageType");
            harmony.Patch(AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.RandomViolenceDamageType)), null, new HarmonyMethod(typeof(RemoveModernStuffHarmony),
                nameof(RandomPermanentInjuryDamageTypePostfix)));

            //foreach (Type type in typeof(ThingSetMaker).AllSubclassesNonAbstract())
            //{
            //    harmony.Patch(original: AccessTools.Method(type: type, name: "Generate", parameters: new[] { typeof(ThingSetMakerParams) }), prefix: new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(ItemCollectionGeneratorGeneratePrefix)), postfix: null);
            //}
            //Log.Message("ThingSetMaker");
            harmony.Patch(original: AccessTools.Method(type: typeof(ThingSetMaker), name: "Generate", parameters: new[] { typeof(ThingSetMakerParams) }), prefix: new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(ItemCollectionGeneratorGeneratePrefix)), postfix: null);

            harmony.Patch(original: AccessTools.Method(type: typeof(FactionManager), name: "FirstFactionOfDef", parameters: new[] { typeof(FactionDef) }), prefix: new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(FactionManagerFirstFactionOfDefPrefix)), postfix: null);

            harmony.Patch(original: AccessTools.Method(type: typeof(BackCompatibility), name: "FactionManagerPostLoadInit", parameters: new Type[] { } ), prefix: new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(BackCompatibilityFactionManagerPostLoadInitPrefix)), postfix: null);

            IEnumerable<MethodInfo> mis = AgeInjuryUtilityNamesHandler();
            if (mis.Any())
            {
                //Log.Message("..." + mis.Count() + " AgeInjuryUtility types found. Attempting address to harmony...");
                foreach (MethodInfo mi in mis)
                {
                    harmony.Patch(original: mi,
                                  prefix: null,
                                  postfix: new HarmonyMethod(typeof(RemoveModernStuffHarmony),
                                                             nameof(RandomPermanentInjuryDamageTypePostfix)));
                }
            }
            else
            {
                //Log.Message("No AgeInjuryUtility found.");
            }

            //Log.Message("GeneratePawn");
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }), null, new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(PostGenerateCleanup)));
            //Log.Message("AddToTradeables");
            harmony.Patch(AccessTools.Method(typeof(TradeDeal), "AddToTradeables"), new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(PostCacheTradeables)), null);
            //Log.Message("TryGiveSolidBioTo");
            harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"), new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(TryGiveSolidBioTo_PreFix)), null);
            //Log.Message("CanGenerate");
            harmony.Patch(AccessTools.Method(typeof(ThingSetMakerUtility), nameof(ThingSetMakerUtility.CanGenerate)), null, new HarmonyMethod(typeof(RemoveModernStuffHarmony), nameof(ThingSetCleaner)));
            
        }

        public static void ThingSetCleaner(ThingDef thingDef, ref bool __result)
        {
            __result &= !RemoveModernStuff.things.Contains(thingDef);
        }

        public static bool PostCacheTradeables(Thing t)
        {
            return !RemoveModernStuff.things.Contains(t.def);
        }

        public static void PostGenerateCleanup(Pawn __result)
        {
            __result.health.hediffSet.hediffs.RemoveAll(hed => RemoveModernStuff.hediffs.Contains(hed.def));
        }

        //TickManager
        public static void StartingYear_PostFix(ref int __result)
        {
            __result = START_DATE; //The year Bilbo left the shire.
        }


        //GenDate
        public static void Year_PostFix(long absTicks, float longitude, ref int __result)
        {
            var num = absTicks + (GenDate.TimeZoneAt(longitude) * 2500L);
            __result = START_DATE + Mathf.FloorToInt(num / 3600000f);
        }


        //GenDate
        public static void DateFullStringAt_PostFix(long absTicks, Vector2 location, ref string __result)
        {
            var num = GenDate.DayOfSeason(absTicks, location.x) + 1;
            var value = Find.ActiveLanguageWorker.OrdinalNumber(num, Gender.None);
            __result = "TTA_FullDate".Translate(value, GenDate.Quadrum(absTicks, location.x).Label(), GenDate.Year(absTicks, location.x), num);
        }

        //GenDate
        public static void DateReadoutStringAt_PostFix(long absTicks, Vector2 location, ref string __result)
        {
            var num = GenDate.DayOfSeason(absTicks, location.x) + 1;
            var value = Find.ActiveLanguageWorker.OrdinalNumber(num, Gender.None);
            __result = "TTA_DateReadout".Translate(value, GenDate.Quadrum(absTicks, location.x).Label(), GenDate.Year(absTicks, location.x), num);
        }

        public static IEnumerable<MethodInfo> AgeInjuryUtilityNamesHandler()
        {
            //Log.Message("Looking for AgeInjuryUtility...");
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    where method.Name == "RandomPermanentInjuryDamageType"
                    select method;
        }

        public static void RandomPermanentInjuryDamageTypePostfix(ref DamageDef __result)
        {
            if (__result == DamageDefOf.Bullet)
            {
                __result = DamageDefOf.Scratch;
                //Log.Message("Hello from RandomOldInjuryDamageTypePostfix.\nI heard you don't like Gunshot, so I fixed it.");
            }
        }

        public static void ItemCollectionGeneratorGeneratePrefix(ref ThingSetMakerParams parms)
        {
            if (ModStuff.Settings.LimitTechnology && parms.techLevel.HasValue && parms.techLevel > RemoveModernStuff.MAX_TECHLEVEL)
            {
                parms.techLevel = RemoveModernStuff.MAX_TECHLEVEL;
            }
        }

        public static bool FactionManagerFirstFactionOfDefPrefix(ref FactionDef facDef)
        {
            if (ModStuff.Settings.LimitTechnology && facDef != null && facDef.techLevel > RemoveModernStuff.MAX_TECHLEVEL)
            {
                return false;
            }
            return true;
        }

        public static bool BackCompatibilityFactionManagerPostLoadInitPrefix()
        {
            return !ModStuff.Settings.LimitTechnology;
        }

        //No one travels in transport pods in the medieval times
        // ReSharper disable once RedundantAssignment
        public static bool IsTravelingInTransportPodWorldObject(Pawn pawn, ref bool __result)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (RemoveModernStuff.MAX_TECHLEVEL <= TechLevel.Industrial)
            {
                __result = false;
                return false;
            }
            // ReSharper disable once HeuristicUnreachableCode
#pragma warning disable 162
            return true;
#pragma warning restore 162
        }

        // No solid bios, to avoid conflicts.
        public static bool TryGiveSolidBioTo_PreFix(Pawn pawn, string requiredLastName, List<string> backstoryCategories,
            ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}