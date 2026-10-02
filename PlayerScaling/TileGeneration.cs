using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json;
using UnityEngine;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using static UnityEngine.Rendering.VolumeComponent;

namespace PlayerScaling
{

    //Larger levels as we get more players
    [HarmonyPatch(typeof(LevelGenerator))]
    [HarmonyPatch("TileGeneration")]
    public static class TileGenerationPatch
    {
        static void Prefix(LevelGenerator __instance, ref int ___ModuleAmount, ref int ___ExtractionAmount, ref float ___DebugLevelSize)
        {
            Plugin.Logger.LogInfo("Entered LevelGenerator.TileGeneration map scaling prefix");
            int requestedModuleAmount = ___ModuleAmount;
            if (requestedModuleAmount <= 4)
            {
                // Vanilla preserves small/custom level module counts and skips its
                // normal progression logic for them. Keep that behavior intact.
                Plugin.curModuleAmount = requestedModuleAmount;
                Plugin.Logger.LogInfo(
                    $"Preserving small/custom map: requestedModules={requestedModuleAmount}; " +
                    "skipping map scaling to match vanilla TileGeneration");
                return;
            }

            float playerScalingAmount = Plugin.PlayerScaling(ScalingType.Map);
            int vanillaMapSize = Plugin.VanillaMapSize(RunManager.instance.levelsCompleted);
            int maxMapSize = Mathf.Min(Mathf.CeilToInt(Plugin.defaultMaxMapSize.Value * playerScalingAmount), 2000); //Capped at 2000 for sanity sake

            ___ExtractionAmount = 0;
            if (maxMapSize > 200) // ignored in beta
            {
                __instance.LevelHeight = 50;
                __instance.LevelWidth = 50;
            }

            ___ModuleAmount = Mathf.Min(Mathf.CeilToInt(playerScalingAmount * vanillaMapSize), maxMapSize);
            Plugin.curModuleAmount = ___ModuleAmount;
            ___DebugLevelSize = playerScalingAmount; // in beta level height and level width get overwritten by a constant, multiplied by this value
            ___ExtractionAmount = Mathf.Max(0, (___ModuleAmount - 4) / 2);
            Plugin.Logger.LogInfo(
                $"Map scaling applied: requestedModules={requestedModuleAmount}, scale={playerScalingAmount:F3}, " +
                $"vanillaModules={vanillaMapSize}, " +
                $"maxModules={maxMapSize}, modules={___ModuleAmount}, extractions={___ExtractionAmount}");
        }
#if DEBUG
    static void Postfix(LevelGenerator __instance, ref int ___ModuleAmount, ref int ___ExtractionAmount, ref int ___DeadEndAmount, ref GameObject ___DebugModule)
    {
        Plugin.Logger.LogInfo(string.Format("playerScalingAmount: {0,10}", Plugin.PlayerScaling(ScalingType.Map)));
        Plugin.Logger.LogInfo(string.Format("ModuleAmount: {0,10}, LevelHeight: {1,10}, LevelWidth: {2,10}", ___ModuleAmount, __instance.LevelHeight, __instance.LevelWidth));
        Plugin.Logger.LogInfo(string.Format("ExtractionAmount: {0,10}, DeadEndAmount: {1,10}, DebugModule: " + (___DebugModule ? "True" : "False"), ___ExtractionAmount, ___DeadEndAmount));
    }
#endif
    }

    //The transpiler method is different because reasons ig
    [HarmonyPatch(typeof(LevelGenerator), "TileGeneration", MethodType.Enumerator)]
    public static class TileGenerationPatchTrans
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var startIndex = -1;
            var endIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            var originalCodes = new List<CodeInstruction>(codes);
            var moduleAmountField = typeof(LevelGenerator).GetField("ModuleAmount", BindingFlags.NonPublic | BindingFlags.Instance);
            var extractionAmountField = typeof(LevelGenerator).GetField("ExtractionAmount", BindingFlags.NonPublic | BindingFlags.Instance);

            if (moduleAmountField == null || extractionAmountField == null)
            {
                Plugin.Logger.LogError("Cannot find LevelGenerator module/extraction fields; preserving vanilla iterator");
                return originalCodes;
            }

            //Remove the following snippet:
            /* ModuleAmount = Mathf.Min(5 + RunManager.instance.levelsCompleted, 10);
            ModuleAmount = Mathf.CeilToInt((float)ModuleAmount * DebugLevelSize);*/

            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].StoresField(moduleAmountField))
                {
                    if (!found)
                    {
                        found = true;
                        for (int j = i; j >= 0; j--)
                        {
                            if (codes[j].opcode == OpCodes.Ble || codes[j].opcode == OpCodes.Ble_S)
                            {
                                startIndex = j + 1;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (i > 0 && codes[i - 1].opcode == OpCodes.Add) continue;
                        endIndex = i + 1;
                        break;
                    }
                }
            }

            if (startIndex >= 0 && endIndex > startIndex)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex);
            }
            else
            {
                Plugin.Logger.LogError("Cannot find <Stdfld ModuleAmount> in LevelGenerator.TileGeneration; preserving vanilla iterator");
                return originalCodes;
            }
            startIndex = -1;
            endIndex = -1;

            //Remove line: ExtractionAmount = 0;

            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].StoresField(extractionAmountField))
                {
                    if (i > 0 && codes[i - 1].opcode == OpCodes.Ldc_I4_0)
                    {
                        startIndex = i - 2;
                        endIndex = i + 1;
                        break;
                    }
                    continue;
                }
            }

            if (startIndex >= 0 && endIndex > startIndex)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex);
            }
            else
            {
                Plugin.Logger.LogError("Cannot find <Stdfld ExtractionAmount> in LevelGenerator.TileGeneration; preserving vanilla iterator");
                return originalCodes;
            }

            found = false;
            startIndex = -1;
            endIndex = -1;

            //Remove the following snippet:
            /* if (ModuleAmount >= 10)
                {
                    ExtractionAmount = 3;
                }
                else if (ModuleAmount >= 8)
                {
                    ExtractionAmount = 2;
                }
                else if (ModuleAmount >= 6)
                {
                    ExtractionAmount = 1;
                }
                else
                {
                    ExtractionAmount = 0;
                }*/

            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(moduleAmountField) && !found)
                {
                    if (i > 0 && i + 2 < codes.Count && codes[i + 1].opcode == OpCodes.Ldc_I4_S && (Convert.ToInt32(codes[i + 1].operand) == 10 || Convert.ToInt32(codes[i + 1].operand) == 15) && (codes[i + 2].opcode == OpCodes.Blt || codes[i + 2].opcode == OpCodes.Blt_S))
                    {
                        found = true;
                        startIndex = i - 1;
                    }
                    continue;
                }

                if (found && codes[i].StoresField(extractionAmountField))
                {
                    if (i > 0 && codes[i - 1].opcode == OpCodes.Ldc_I4_0)
                    {
                        endIndex = i + 1;
                        break;
                    }
                    continue;
                }
            }

            if (startIndex >= 0 && endIndex > startIndex)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex);
            }
            else
            {
                Plugin.Logger.LogError("Cannot find vanilla extraction thresholds in LevelGenerator.TileGeneration; preserving vanilla iterator");
                return originalCodes;
            }

            Plugin.Logger.LogInfo("Installed LevelGenerator.TileGeneration iterator rewrite");
            return codes.AsEnumerable();
        }
    }
}
