using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.CodeDom;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Knife.Effects;
using Knife.Effects.SimpleController;
using UnityEngine.TextCore;
using UnityEngine.PlayerLoop;
using JetBrains.Annotations;
using System.Reflection;

namespace imcheat;

[BepInPlugin("immblind.plugins.imcheat", "imcheat plugin", "1.0.0.0")]
[BepInProcess("UltimateZombieDefense_64.exe")]
public class Plugin : BaseUnityPlugin
{

    public static new ManualLogSource Logger;
    
    public static ConfigEntry<int> maxBuilds;
    public static ConfigEntry<bool> EnableCustomBuildLimit;

    public static ConfigEntry<float> CustomCashReward;
    public static ConfigEntry<bool> EnableCustomCashReward;
    public static ConfigEntry<bool> InfiniteAmmo;

    public static ConfigEntry<int> CustomWaveSkip;
    public static ConfigEntry<bool> EnableWaveSkip;

    public static ConfigEntry<float> CustomWalkSpeed;
    public static ConfigEntry<bool> EnableCustomWalkSpeed;

    public static ConfigEntry<bool> NoBuildingBlock;

    public static ConfigEntry<bool> EnableGodmode;

    public static ConfigEntry<bool> InfiniteDamage;
    public static ConfigEntry<float> Damage;

    public static ConfigEntry<bool> EnableForceHalloween;

    public static ConfigEntry<bool> BuildAnytime;
    private void Awake()
    {
        //Host(or host and client have plugin) only features
        maxBuilds = Config.Bind("------ Host ------", "Max build limit", 99999999, "The maximum build limit (Applies to all buildings)");
        EnableCustomBuildLimit = Config.Bind("------ Host ------", "Enable custom build limit", true, "Enable or not the custom build limit.");

        CustomCashReward = Config.Bind("------ Host ------", "Cash reward per wave", 9999999f, "Custom cash reward for every wave");
        EnableCustomCashReward = Config.Bind("------ Host ------", "Enable custom cash reward", true, "Enable or not the custom cash reward value");

        CustomWaveSkip = Config.Bind("------ Host ------", "Custom wave skip", 5, "Allows to skip a number of waves after each wave.");
        EnableWaveSkip = Config.Bind("------ Host ------", "Enable custom wave skip", false, "Enables or not the custom wave skip value");

        NoBuildingBlock = Config.Bind("------ Host ------", "No building block", false, "Removes building limits.");

        EnableGodmode = Config.Bind("------ Host ------", "Godmode", false, "Enables or not godmode (Become invincile)");

        InfiniteDamage = Config.Bind("------ Host ------", "Infinite damage", false, "One shot everything.");

        EnableForceHalloween = Config.Bind("------ Host ------", "Force halloween", false, "Force the game to use exclusive halloween content");

        //Client features(work both as host and as client even if host doesnt have the plugin)
        CustomWalkSpeed = Config.Bind("------ Client ------", "Custom walk speed", 8f, new ConfigDescription("You can go as fast as you want! Default is around 8.", new AcceptableValueRange<float>(0.01f, 100f)));
        EnableCustomWalkSpeed = Config.Bind("------ Client ------", "Enable custom walk speed", false, "Enable or not your custom walk speed value");

        InfiniteAmmo = Config.Bind("------ Client ------", "Infinite ammo", true, "Shooting doesnt consume any ammo.");

        BuildAnytime = Config.Bind("------ Client ------", "Force build mode", true, "Build menu will always be usable, you can build, repair and sell builds at any time. Works in multiplayer.");

        Logger = base.Logger;
        var harmony = new Harmony("immblind.plugins.imcheat");
        harmony.PatchAll();

        int modcount = 10;
        Plugin.Logger.LogInfo($"Loaded imcheat with {modcount} mods.");

    }
}

[HarmonyPatch(typeof(ZS_BuildingLimit), "MaxCount", MethodType.Getter)]                         //Building count limit remover
class PatchMaxBuildCount
{
    static bool Prepare()
    {
        return Plugin.EnableCustomBuildLimit.Value;
    }
    static void Postfix(ref int __result)
    {
        __result = Plugin.maxBuilds.Value;
    }
    
}
[HarmonyPatch(typeof(ZS_WaveSystemServer), "CashRewardForWave")]                               //Infinite end wave money mod
class PatchCashReward
{
    static bool Prepare()
    {
        return Plugin.EnableCustomCashReward.Value;
    }
    static void Postfix(ref float __result)
    {
        __result = Plugin.CustomCashReward.Value;
    }
}

[HarmonyPatch(typeof(ZS_WaveSystemServer), "GenerateNewWave")]                                   //Wave skipper mod
class NexWavePatcher2                                                        //Does the same thing as in the game code but with a different value
{

    static bool Prepare()
    {
        return Plugin.EnableWaveSkip.Value && Plugin.CustomWaveSkip.Value>1;
    }
    static bool Prefix(ZS_WaveSystemServer __instance)
    {
        var trv = Traverse.Create(__instance);
        int waveskip = Plugin.CustomWaveSkip.Value;

        trv.Property("waveInProgress").SetValue(true); //this.waveInProgress = true;
        int nextwave = trv.Property("NextWave").GetValue<int>(); 
        trv.Property("CurrentWave").SetValue(nextwave); //this.CurrentWave = this.NextWave;
        int currentwave = trv.Property("CurrentWave").GetValue<int>();
        if(currentwave==1)
        {
            nextwave += waveskip - 1; //int num = this.NextWave + 1;
        }
        else
        {
            nextwave += waveskip;
        }
        trv.Property("NextWave").SetValue(nextwave); //this.NextWave = num;
        trv.Method("StartWave").GetValue(); //this.StartWave();
        return false;
    }
}

[HarmonyPatch(typeof(ZS_WeaponMagazine), "Fired")]                                      //Actual infinite ammo
class InfiniteAmmoPatcher                                               //Just removes the function that decrease ammo count on shot.
{
    static bool Prefix()
    {
        if(Plugin.InfiniteAmmo.Value)
        {
            return false;
        } else
        {
            return true; //Returning true, so harmony does nothing to the function.
        }
    }
}

[HarmonyPatch(typeof(ZS_PlayerControl), "MovementSpeed", MethodType.Getter)]        //Speedhack mod
class PlayerSpeedPatcher
{
    static bool Prepare()
    {
        return Plugin.EnableCustomWalkSpeed.Value;
    }
    static void Postfix(ref float __result)
    {
        __result = Plugin.CustomWalkSpeed.Value;
    }
}

[HarmonyPatch(typeof(ZS_BuildingManager), "CheckIfCoordinatesOkay")]                        //Buidling blocked remover (bad)
class PatchNoBuildingBlocked
{
    static bool Prepare()
    {
        return Plugin.NoBuildingBlock.Value;
    }
    static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(ZS_Health), "OnTakeDamage")]                                               //Godmode patcher + Infinite damage
class betterGodModePatcher                                                      //Doesnt use ingame godmode, just set attack damage to 0.
{
    static void Prefix(ZS_ConnectedPlayerObject player, ref float damage, ZS_Vector3 direction, EDamageType damageType, GameObject ownerObject, ref OnHealthChangeInfo info)
    {
        if(player==null && Plugin.EnableGodmode.Value) //Not a damage by a player but TO a player
        {
            damage = 0f;
        }
    }
}

[HarmonyPatch(typeof(ZS_WaveZombieDogWave), "Process")]                                         // Halloween Force Mod
class HalloweenForcePatch
{
    static void Prefix(ZS_WaveZombieDogWave __instance)
    {
        if(Plugin.EnableForceHalloween.Value)
        {
            var trv = Traverse.Create(__instance);
            DatabaseValueBool dbvalue = new()
            {
                value = true
            };
            trv.Field("isHalloween").SetValue(dbvalue);

        }

    }
}
[HarmonyPatch(typeof(ZS_Health), "OnTakeDamage")]                                                   //Working Infinite damage mod
class OnTakeDamageTracker                                               //On each attack, it take your damage and set it to the double of the enemy health (just in case)
{
    static void Prefix(ZS_ConnectedPlayerObject player, ref float damage, ZS_Health __instance)
    {
        
        if (player != null && Plugin.InfiniteDamage.Value)
        {
            var tvr = Traverse.Create(__instance);
            float health = tvr.Field("health").GetValue<float>();
            damage = health*2;
        }
        
    }
    static void Postfix(ZS_ConnectedPlayerObject player, float damage, bool __result, ZS_Health __instance)
    {
        if (player != null && Plugin.InfiniteDamage.Value)
        {
            var tvr = Traverse.Create(__instance);
            float health = tvr.Field("health").GetValue<float>();
            if(health>0f)
            {
                Plugin.Logger.LogError($"[InfiniteDamage] The zombie wasnt one shot ! health={health}, damage={damage}, return={__result}");
            }

        }

    }
}

                                            //New Build mode forcer
// -----------------------------------------------------------------------------------------------------------
[HarmonyPatch(typeof(ZS_NewInputSystemRequesterBuilding), "UpdateRequester")]
class OnEnterTracker
{  
    static void Prefix(ZS_NewInputSystemRequesterBuilding __instance)
    {
        var tvr = Traverse.Create(__instance);
        tvr.Field("canEnterBuildMode").SetValue(true);
    }

}

[HarmonyPatch(typeof(ZS_PlayerControl), "IsBuildPhase")]
class IsBuildPhasePatcher
{
    static void Postfix(ref bool __result)
    {
        __result = true;
    }
}

[HarmonyPatch(typeof(ZS_NewInputSystemRequesterBuilding), "EnterBuildMode", [])]
class EnterBuildModepPtcher2
{
    static bool Prefix(ZS_NewInputSystemRequesterBuilding __instance)
    {
        var playerControl = Traverse.Create(__instance).Property("GetPlayerControl").GetValue();
        if (playerControl != null)
        {
            ZS_IOnBuildModeEnteredHelper.Call((ZS_PlayerControl)playerControl, true);
        }
        return false;
    }
}

[HarmonyPatch(typeof(ZS_BuildModeManagerUI), "EnableBuildManagerUI")]
class BuildUiTracker
{
    static void Prefix(ZS_BuildModeManagerUI __instance, ref bool enable)
    {
        var tvr = Traverse.Create(__instance);
        bool canenter = tvr.Field("canEnterBuildMode").GetValue<bool>();
        enable = true;
        tvr.Field("canEnterBuildMode").SetValue(true);
    }
}
// -----------------------------------------------------------------------------------------------------------





