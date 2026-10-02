using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace PlayerScaling
{
    [HarmonyPatch(typeof(ValuableDirector))]
    [HarmonyPatch(nameof(ValuableDirector.SetupHost))]
    [HarmonyWrapSafe]
    public static class ValuablePatch
    {
        private sealed class CurveBaseline
        {
            public AnimationCurve Curve { get; }

            public CurveBaseline(AnimationCurve curve) => Curve = curve;
        }

        private static readonly ConditionalWeakTable<AnimationCurve, CurveBaseline> CurveBaselines = new();

        private static AnimationCurve[] TotalMaxAmountCurves { get; set; }
        private static AnimationCurve[] TotalMaxValueCurves { get; set; }
        private static AnimationCurve[] TinyCurves { get; set; }
        private static AnimationCurve[] SmallCurves { get; set; }
        private static AnimationCurve[] MediumCurves { get;  set; }
        private static AnimationCurve[] BigCurves { get; set; }
        private static AnimationCurve[] WideCurves { get; set; }
        private static AnimationCurve[] TallCurves { get; set; }
        private static AnimationCurve[] VeryTallCurves { get; set; }
        
        static void Prefix(ValuableDirector __instance)
        {
            if(__instance == null) return;
            var totalMaxAmountRef = FieldRefAccess<ValuableDirector, int>("totalMaxAmount");
            Plugin.Logger.LogInfo("Player scaling runs HERE");
            var difficultyDelegates = GetStaticNumberedMethodDelegates<Plugin.DifficultyDelegate>(typeof(SemiFunc), "RunGetDifficultyMultiplier", [], 10);
            
            //Similar to enemies, we try to maintain density, but I'm not fucking around with trying to replicate these curves.
            float mapScalingFactor = GetMapDensityScalingFactor();

            var maxAmountFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "totalMaxAmountCurve", 10);
            if(maxAmountFieldRefs.Count != 0) {
                TotalMaxAmountCurves ??= new AnimationCurve[maxAmountFieldRefs.Count];
                foreach (var curve in maxAmountFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                        .Where(tuple => tuple.Item1 != null && tuple.Item2 != null)
                                                        .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))) 
                    curve.Item2.Invoke(__instance) = ReplaceCurve(ref TotalMaxAmountCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            }
            else // in beta it's a hard coded value that never changes so just replace it -- this is also what breaks the transpiler as it looks for an instruction setting it
            {
                totalMaxAmountRef.Invoke(__instance) = (int)Math.Ceiling(totalMaxAmountRef.Invoke(__instance) * mapScalingFactor);
            }

            var maxValueFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "totalMaxValueCurve", 10);
            if(maxValueFieldRefs.Count != 0) // this only exists in beta
            {
                TotalMaxValueCurves ??= new AnimationCurve[maxValueFieldRefs.Count];
                foreach (var curve in maxValueFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                       .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                       .Where(value => value.Item2 != null && value.Item3 != null)) 
                    curve.Item2.Invoke(__instance) = ReplaceCurve(ref TotalMaxValueCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            }
            
            var tinyMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "tinyMaxAmountCurve", 10);
            TinyCurves ??= new AnimationCurve[tinyMaxFieldRefs.Count];
            foreach (var curve in tinyMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                   .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref TinyCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var smallMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "smallMaxAmountCurve", 10);
            SmallCurves ??= new AnimationCurve[smallMaxFieldRefs.Count];
            foreach (var curve in smallMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref SmallCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var mediumMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "mediumMaxAmountCurve", 10);
            MediumCurves ??= new AnimationCurve[mediumMaxFieldRefs.Count];
            foreach (var curve in mediumMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref MediumCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var bigMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "bigMaxAmountCurve", 10);
            BigCurves ??= new AnimationCurve[bigMaxFieldRefs.Count];
            foreach (var curve in bigMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref BigCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var wideMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "wideMaxAmountCurve", 10);
            WideCurves ??= new AnimationCurve[wideMaxFieldRefs.Count];
            foreach (var curve in wideMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref WideCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var tallMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "tallMaxAmountCurve", 10);
            TallCurves ??= new AnimationCurve[tallMaxFieldRefs.Count];
            foreach (var curve in tallMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref TallCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
            var veryTallMaxFieldRefs = GetNumberedFieldRefs<ValuableDirector, AnimationCurve>(__instance, "veryTallMaxAmountCurve", 10);
            VeryTallCurves ??= new AnimationCurve[veryTallMaxFieldRefs.Count];
            foreach (var curve in veryTallMaxFieldRefs.Zip(difficultyDelegates, Tuple.Create)
                                                  .Select((value, index) => Tuple.Create(index, value.Item1, value.Item2))
                                                  .Where(value => value.Item2 != null && value.Item3 != null)) 
                curve.Item2.Invoke(__instance) = ReplaceCurve(ref VeryTallCurves[curve.Item1], curve.Item2.Invoke(__instance), value => value * mapScalingFactor);
        }
        
#if DEBUG
        static void Postfix(ref int ___totalMaxAmount, ref int ___tinyMaxAmount, ref int ___smallMaxAmount, ref int ___mediumMaxAmount, ref int ___bigMaxAmount, ref int ___wideMaxAmount, ref int ___tallMaxAmount, ref int ___veryTallMaxAmount, ValuableDirector __instance)
        {
            Plugin.Logger.LogInfo(string.Format("totalmax: {0,10}, tinymax: {1,10}, smallmax: {2,10}", ___totalMaxAmount, ___tinyMaxAmount, ___smallMaxAmount));
            Plugin.Logger.LogInfo(string.Format("mediummax: {0,10}, bigmax: {1,10}, widemax: {2,10}", ___mediumMaxAmount, ___bigMaxAmount, ___wideMaxAmount));
            Plugin.Logger.LogInfo(string.Format("tallmax: {0,10}, veryTallmax: {1,10}", ___tallMaxAmount, ___veryTallMaxAmount));
        }
#endif
        
        private static AnimationCurve ReplaceCurve(ref AnimationCurve target, AnimationCurve source, Func<float, float> calculate) {
            Plugin.Logger.LogInfo("Player scaling will replace a curve with a factor of " + calculate(1f));

            if (source == null)
            {
                target = null;
                return null;
            }

            // If SetupHost sees our previous output again, start from its saved input.
            // A curve from another mod (including SLRUpgradePack) is treated as fresh
            // input, so its change is preserved and this mod's multiplier is applied once.
            AnimationCurve baselineSource = source;
            if (CurveBaselines.TryGetValue(source, out var priorBaseline))
                baselineSource = priorBaseline.Curve;

            var baseline = new AnimationCurve();
            baseline.CopyFrom(baselineSource);

            target = new AnimationCurve();
            target.CopyFrom(baseline); // preserve wrap modes and curve metadata
            target.ClearKeys(); // replace keyframes with the scaled values

            foreach (var key in baseline.keys)
            {
                var newKey = key with { value = calculate.Invoke(key.value)};
                target.AddKey(newKey);
            }

            CurveBaselines.Add(target, new CurveBaseline(baseline));

            return target;
        }

        internal static float GetMapDensityScalingFactor()
        {
            int vanillaMapSize = Plugin.VanillaMapSize(RunManager.instance.levelsCompleted);
            float mapDensityFactor = 1f;
            if (Plugin.mapScalingEnabled.Value && vanillaMapSize > 0)
                mapDensityFactor = Plugin.curModuleAmount / (float)vanillaMapSize;

            float valuableFactor = Plugin.valuableScalingEnabled.Value
                ? Plugin.valuableScalingMultiplier.Value
                : 1f;
            return mapDensityFactor * valuableFactor;
        }
        
        private static List<FieldRef<S, T>> GetNumberedFieldRefs<S, T>(S source, string expectedBaseName, int checkMax, int checkMin = 0) 
        {
            var fields = new List<FieldRef<S, T>>();

            if (Traverse.Create(source).Field(expectedBaseName).FieldExists()) {
                fields.Add(FieldRefAccess<S, T>(expectedBaseName));
            }

            for (var i = checkMin; i < checkMax; i++) {
                if (Traverse.Create(source).Field(expectedBaseName + i).FieldExists()) {
                    fields.Add(FieldRefAccess<S, T>(expectedBaseName + i));
                }
            }
        
            return fields;
        }
        
        private static List<T> GetStaticNumberedMethodDelegates<T>(Type source, string expectedBaseName, Type[] parameters, int checkMax, int checkMin = 0) where T : Delegate 
        {
            var methods = new List<T>();
        
            if (Traverse.Create(source).Method(expectedBaseName, parameters).MethodExists()) 
            {
                methods.Add(MethodDelegate<T>(Method(source, expectedBaseName, parameters), source, true));
            }
        
            for (var i = checkMin; i < checkMax; i++) 
            {
                if (Traverse.Create(source).Method(expectedBaseName + i, parameters).MethodExists()) 
                {
                    methods.Add(MethodDelegate<T>(Method(source, expectedBaseName + i, parameters), source, true));
                }
            }
        
            return methods;
        }
    }

    // The game uses an inclusive loop bound for cosmetic spawn checks, so scale the
    // number of checks here rather than changing either of its probability curves.
    [HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.SetupHost), MethodType.Enumerator)]
    public static class CosmeticWorldObjectRollScalingPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var originalCodes = new List<CodeInstruction>(codes);
            var clampedLoopsGetter = AccessTools.Method(
                typeof(ValuableDirector), nameof(ValuableDirector.CosmeticWorldObjectLevelLoopsClampedGet));
            var scaledLoopsGetter = AccessTools.Method(
                typeof(CosmeticWorldObjectRollScalingPatch), nameof(ScaleRollLoopBound));

            if (clampedLoopsGetter == null || scaledLoopsGetter == null)
            {
                Plugin.Logger.LogError("Cannot resolve the cosmetic roll loop bound methods; preserving vanilla SetupHost iterator");
                return originalCodes;
            }

            var matchingCalls = new List<int>();
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(clampedLoopsGetter))
                    matchingCalls.Add(i);
            }

            if (matchingCalls.Count != 1)
            {
                Plugin.Logger.LogError("Cannot uniquely locate the cosmetic roll loop bound; preserving vanilla SetupHost iterator");
                return originalCodes;
            }

            var call = codes[matchingCalls[0]];
            call.opcode = OpCodes.Call;
            call.operand = scaledLoopsGetter;
            Plugin.Logger.LogInfo("Installed cosmetic world-object roll-count scaling in ValuableDirector.SetupHost");
            return codes;
        }

        private static int ScaleRollLoopBound(ValuableDirector director)
        {
            // REPO loops from zero through this bound (inclusive), hence the +1/-1.
            int vanillaLoopBound = director.CosmeticWorldObjectLevelLoopsClampedGet();
            int vanillaRollCount = vanillaLoopBound + 1;
            float scalingFactor = ValuablePatch.GetMapDensityScalingFactor();
            int scaledRollCount = Mathf.Clamp(Mathf.RoundToInt(vanillaRollCount * scalingFactor), 0, 2000);
            Plugin.Logger.LogInfo(
                $"Cosmetic world-object rolls: vanillaBound={vanillaLoopBound}, vanillaRolls={vanillaRollCount}, " +
                $"densityFactor={scalingFactor:F3}, scaledRolls={scaledRollCount}, scaledBound={scaledRollCount - 1}");
            return scaledRollCount - 1;
        }
    }
}
