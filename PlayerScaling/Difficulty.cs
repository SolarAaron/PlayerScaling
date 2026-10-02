using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PlayerScaling
{
    [HarmonyPatch(typeof(SemiFunc))]
    public static class DifficultyPatch
    {
        static IEnumerable<MethodBase> TargetMethods() // in beta this function is split into 3 different versions -- this scales all three in that case or the default one in release
        {
            return Plugin.GetNumberedMethodInfos(typeof(SemiFunc), "RunGetDifficultyMultiplier", [], 10);
        }
        
        static void Postfix(ref float __result)
        {
            float playerScalingAmount = Plugin.PlayerScaling(ScalingType.Difficulty);
            __result += Plugin.difficultyScalingOffset.Value; //Slight bump to make playerScaling actually kick in on first round
            __result *= Mathf.Sqrt(playerScalingAmount);
        }
    }
}
