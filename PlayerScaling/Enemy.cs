using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PlayerScaling
{
    [HarmonyPatch(typeof(EnemyDirector))]
    [HarmonyPatch(nameof(EnemyDirector.AmountSetup))]
    //Enemy related patches
    //The curves cap the difficulty so the solution is to add the multiplier post-curve
    public static class EnemyAmountPatch
    {
        static void Prefix(ref int ___amountCurve1Value, ref int ___amountCurve2Value, ref int ___amountCurve3Value, EnemyDirector __instance)
        {
            float effectiveLevelsCompleted = RunManager.instance.levelsCompleted * Plugin.enemyScalingMultiplier.Value;

            //I've decided I want to prioritize on expanding the "feel" of vanilla, so my equations prioritize maintaining and if enabled, extrapolating enemy density (i.e. how many enemies divided by map size)

            float tier1VanillaCount = Mathf.FloorToInt(Mathf.Max(1, Mathf.Min(2, (effectiveLevelsCompleted + 4) / 5f)));

            float tier2VanillaCount = Mathf.FloorToInt(Mathf.Max(0, Mathf.Min(2, effectiveLevelsCompleted / 3f)));

            float tier3VanillaCount = Mathf.FloorToInt(Mathf.Max(1, Mathf.Min(2, (effectiveLevelsCompleted + 4) / 5f)));

            float vanillaMapSize = Plugin.VanillaMapSize(RunManager.instance.levelsCompleted);

            float tier1Density = tier1VanillaCount / vanillaMapSize;
            float tier2Density = tier2VanillaCount / vanillaMapSize;
            float tier3Density = tier3VanillaCount / vanillaMapSize;
            
            float totalDensity = tier1Density + tier2Density + tier3Density;

            float ratio = 1.0f;
            //Configured to be less than default, scale down if necessary
            if(totalDensity > Plugin.maxEnemyDensity.Value)
            {
                ratio = Plugin.maxEnemyDensity.Value / totalDensity;
            }

            //Configured to be more than default, continue scaling past vanilla
            if(Plugin.maxEnemyDensity.Value > 0.8 && effectiveLevelsCompleted > 11)
            {
                float targetDensity = 0.25f + 0.05f * effectiveLevelsCompleted;
                ratio = targetDensity / totalDensity;
            }

            tier1Density *= ratio;
            tier2Density *= ratio;
            tier3Density *= ratio;

            //To get actual monster amounts, multiply the density we got by the number of map modules currently.
            ___amountCurve1Value = Mathf.Max(1,Mathf.RoundToInt(tier1Density * Plugin.curModuleAmount));
            ___amountCurve2Value = Mathf.Max(0, Mathf.RoundToInt(tier2Density * Plugin.curModuleAmount));
            ___amountCurve3Value = Mathf.Max(1, Mathf.RoundToInt(tier3Density * Plugin.curModuleAmount));
        }
#if DEBUG
    static void Postfix(ref int ___amountCurve1Value, ref int ___amountCurve2Value, ref int ___amountCurve3Value)
    {
        Plugin.Logger.LogInfo(string.Format("Tier 1 enemies: {0,10}, Tier 2 enemies: {1,10}, Tier 3 enemies: {2,10}", ___amountCurve1Value, ___amountCurve2Value, ___amountCurve3Value));
    }
#endif
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var endIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].StoresField(typeof(EnemyDirector).GetField("amountCurve1Value", BindingFlags.NonPublic | BindingFlags.Instance)))
                {
                    endIndex = i + 1;
                }
            }

            if (endIndex > -1)
            {
                codes.RemoveRange(0, endIndex);
            }
            else
            {
                Plugin.Logger.LogError("Cannot find <Stdfld amountCurve1Value> in EnemyDirector.AmountSetup");
            }
            return codes.AsEnumerable();
        }
    }
}
