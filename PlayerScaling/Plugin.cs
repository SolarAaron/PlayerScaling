using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace PlayerScaling;

[BepInPlugin(modGUID, modName, modVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string modGUID = "dev.redfops.repo.playerscaling"; // keeping original guid
    public const string modName = "Player Scaling Updated";
    public const string modVersion = "2.0.0";

    public static int curModuleAmount = 5;

    public static ConfigEntry<bool> mapScalingEnabled;
    public static ConfigEntry<bool> mapIteratorRewriteEnabled;
    public static ConfigEntry<bool> downScalingEnabled;
    public static ConfigEntry<bool> difficultyScalingEnabled;
    public static ConfigEntry<bool> enemyScalingEnabled;
    public static ConfigEntry<bool> valuableScalingEnabled;
    public static ConfigEntry<float> mapScalingMultiplier;
    public static ConfigEntry<float> enemyScalingMultiplier;
    public static ConfigEntry<float> valuableScalingMultiplier;
    public static ConfigEntry<float> difficultyScalingMultiplier;
    public static ConfigEntry<float> difficultyScalingOffset;

    public static ConfigEntry<int> defaultMaxMapSize;
    public static ConfigEntry<float> maxEnemyDensity;
    public static ConfigEntry<float> downScalingMin;

    public static ConfigEntry<float> globalScalingMultiplier;
    public static ConfigEntry<int> numPlayersStartScaling;
    public static ConfigEntry<float> playerDivisor;

    public delegate float DifficultyDelegate();

    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;

        globalScalingMultiplier = Config.Bind("General Scaling", "Global Scaling Multiplier", 1f, new ConfigDescription("Multiplies player scaling by this number", new AcceptableValueRange<float>(0.3f, 10f)));
        numPlayersStartScaling = Config.Bind("General Scaling", "Player Scaling Minimum", 4, new ConfigDescription("Scaling only happens after the player count passes this number", new AcceptableValueRange<int>(1, 16)));
        playerDivisor = Config.Bind("General Scaling", "Player Scaling Divisor", 4f, new ConfigDescription("Number of players divided by this is the scaling factor", new AcceptableValueRange<float>(1f, 12f)));
        downScalingEnabled = Config.Bind("General Scaling", "Down Scaling Enabled", false, new ConfigDescription("Whether or not the map will be scaled down if less players than minimum"));
        downScalingMin = Config.Bind("General Scaling", "Minimum Downscaling Multiplier", 0.5f, new ConfigDescription("The lowest the downscaling factor will go, not recommended below 0.5", new AcceptableValueRange<float>(0.1f, 1f)));

        mapScalingEnabled = Config.Bind("Map Scaling", "Map Scaling Enabled", true, new ConfigDescription("Whether or not the map will be scaled to the number of players"));
        mapIteratorRewriteEnabled = Config.Bind("Map Scaling", "Map Iterator Rewrite Enabled", true, "Whether the custom map-generation iterator rewrite is installed; disable to isolate the map prefix from the transpiler");
        mapScalingMultiplier = Config.Bind("Map Scaling", "Map Scaling Multiplier", 1f, new ConfigDescription("Multiplies the map size by this number (including max size)", new AcceptableValueRange<float>(0.3f, 10f)));
        defaultMaxMapSize = Config.Bind("Map Scaling", "Default Max Map Size", 15, new ConfigDescription("Max map size before scaling", new AcceptableValueRange<int>(5,60)));

        enemyScalingMultiplier = Config.Bind("Enemies Scaling", "Enemies Scaling Multiplier", 1f, new ConfigDescription("Multiplies the number of enemies by this number (not including max)", new AcceptableValueRange<float>(0.3f, 10f)));
        enemyScalingEnabled = Config.Bind("Enemies Scaling", "Enemy Scaling Enabled", true, "Whether enemy-count scaling patches are installed");
        maxEnemyDensity = Config.Bind("Enemies Scaling", "Max Enemy Density", 0.8f, new ConfigDescription("Caps number of enemies per map module", new AcceptableValueRange<float>(0.3f, 5f)));

        valuableScalingEnabled = Config.Bind("Valuables Scaling", "Valuable Scaling Enabled", true, "Whether valuable-density and cosmetic-roll scaling patches are installed");
        valuableScalingMultiplier = Config.Bind("Valuables Scaling", "Valuables Scaling Multiplier", 1f, new ConfigDescription("Multiplies the amount of valuables by this number", new AcceptableValueRange<float>(0.3f, 10f)));

        difficultyScalingEnabled = Config.Bind("Difficulty Scaling", "Difficulty Scaling Enabled", true, new ConfigDescription("Whether or not the difficulty will be scaled to the number of players"));
        difficultyScalingMultiplier = Config.Bind("Difficulty Scaling", "Difficulty Scaling Multiplier", 1f, new ConfigDescription("Multiplies the difficulty by this number", new AcceptableValueRange<float>(0.3f, 4f)));
        difficultyScalingOffset = Config.Bind("Difficulty Scaling", "Difficulty Scaling Offset", 0.085f, new ConfigDescription("Offsets the general difficulty", new AcceptableValueRange<float>(0f, 1f)));

        ApplyPatchConfiguration();

        mapScalingEnabled.SettingChanged += OnPatchToggleChanged;
        mapIteratorRewriteEnabled.SettingChanged += OnPatchToggleChanged;
        difficultyScalingEnabled.SettingChanged += OnPatchToggleChanged;
        enemyScalingEnabled.SettingChanged += OnPatchToggleChanged;
        valuableScalingEnabled.SettingChanged += OnPatchToggleChanged;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnPatchToggleChanged(object sender, EventArgs args)
    {
        ApplyPatchConfiguration();
    }

    private void ApplyPatchConfiguration()
    {
        // Remove only this plugin's hooks before applying the current selection. This
        // also makes Config Manager changes effective without leaving stale patches.
        harmony.UnpatchSelf();

        if (mapScalingEnabled.Value)
        {
            if (mapIteratorRewriteEnabled.Value)
                harmony.PatchAll(typeof(TileGenerationPatchTrans));
            harmony.PatchAll(typeof(TileGenerationPatch));
        }

        if (difficultyScalingEnabled.Value)
            harmony.PatchAll(typeof(DifficultyPatch));

        if (enemyScalingEnabled.Value)
            harmony.PatchAll(typeof(EnemyAmountPatch));

        if (valuableScalingEnabled.Value)
        {
            harmony.PatchAll(typeof(ValuablePatch));
            harmony.PatchAll(typeof(CosmeticWorldObjectRollScalingPatch));
        }

        Logger.LogInfo(
            $"Patch toggles applied: map={mapScalingEnabled.Value}, " +
            $"mapIteratorRewrite={mapScalingEnabled.Value && mapIteratorRewriteEnabled.Value}, " +
            $"difficulty={difficultyScalingEnabled.Value}, enemies={enemyScalingEnabled.Value}, " +
            $"valuables={valuableScalingEnabled.Value}");
    }

    //This function is mostly redundant (and a bit ugly) from an old implementation, but I'll keep it around for now, mostly due to laziness.
    public static float PlayerScaling(ScalingType scalingType)
    {
        return scalingType switch
        {
            ScalingType.Valuable => valuableScalingEnabled.Value ? valuableScalingMultiplier.Value : 1,
            ScalingType.Enemy => enemyScalingEnabled.Value ? enemyScalingMultiplier.Value : 1,
            ScalingType.Map => mapScalingEnabled.Value ? mapScalingMultiplier.Value * PlayerScaling(ScalingType.Global) : 1,
            ScalingType.Difficulty => difficultyScalingEnabled.Value ? difficultyScalingMultiplier.Value * PlayerScaling(ScalingType.Global) : 1,
            _ => globalScalingMultiplier.Value * (((GameDirector.instance.PlayerList.Count < numPlayersStartScaling.Value) || downScalingEnabled.Value) ? 1 : Mathf.Max(GameDirector.instance.PlayerList.Count / playerDivisor.Value, downScalingMin.Value)),
        };
    }

    // REPO's current vanilla progression reaches 10 modules, then adds up to 5 more
    // from level 10 onward. Keep every density calculation on that same baseline.
    public static int VanillaMapSize(int levelsCompleted)
    {
        levelsCompleted = Mathf.Max(0, levelsCompleted);
        int moduleAmount = Mathf.Min(5 + levelsCompleted, 10);
        if (levelsCompleted >= 10)
            moduleAmount += Mathf.Min(levelsCompleted - 9, 5);

        return moduleAmount;
    }
    
    public static List<MethodBase> GetNumberedMethodInfos(Type source, string expectedBaseName, Type[] parameters, int checkMax, int checkMin = 0) {
        var methods = new List<MethodBase>();

        if (Traverse.Create(source).Method(expectedBaseName, parameters).MethodExists()) {
            methods.Add(Method(source, expectedBaseName, parameters));
        }
        
        for (var i = checkMin; i < checkMax; i++) {
            if (Traverse.Create(source).Method(expectedBaseName + i, parameters).MethodExists()) {
                methods.Add(Method(source, expectedBaseName + i, parameters));
            }
        }
        
        return methods;
    }
}

public enum ScalingType{
    Global,
    Enemy,
    Map,
    Valuable,
    Difficulty,
}
