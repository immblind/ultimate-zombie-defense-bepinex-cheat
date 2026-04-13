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
    private void Awake()
    {
        maxBuilds = Config.Bind("Host", "Max build limit", 99999999, "The maximum build limit (Applies to all buildings)");
        EnableCustomBuildLimit = Config.Bind("Host", "Enable custom nuild limit", true, "Enable or not the custom build limit.");

        CustomCashReward = Config.Bind("Host", "Cash reward per wave", 9999999f, "Custom cash reward for every wave");
        EnableCustomCashReward = Config.Bind("Host", "Enable custom cash reward", true, "Enable or not the custom cash reward value");

        CustomWaveSkip = Config.Bind("Host", "Custom wave skip", 5, "Allows to skip a number of waves after each wave.");
        EnableWaveSkip = Config.Bind("Host", "Enable custom wave skip", false, "Enables or not the custom wave skip value");

        CustomWalkSpeed = Config.Bind("Client", "Custom walk speed", 8f, new ConfigDescription("You can go as fast as you want! Default is around 8.", new AcceptableValueRange<float>(0.01f, 100f)));
        EnableCustomWalkSpeed = Config.Bind("Client", "Enable custom walk speed", false, "Enable or not your custom walk speed value");

        NoBuildingBlock = Config.Bind("Host", "No building block", false, "Removes building limits.");

        EnableGodmode = Config.Bind("Host", "Godmode", false, "Enables or not godmode (Become invincile)");

        InfiniteDamage = Config.Bind("Host", "Infinite damage", false, "One shot everything.");

        EnableForceHalloween = Config.Bind("Host", "Force halloween", false, "Force the game to use exclusive halloween content");

        InfiniteAmmo = Config.Bind("Client", "Infinite ammo", true, "Shooting doesnt consume any ammo.");

        Logger = base.Logger;
        var harmony = new Harmony("immblind.plugins.imcheat");
        harmony.PatchAll();
        int modcount = 9;
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
        return Plugin.EnableWaveSkip.Value;
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

        //Plugin.Logger.LogInfo($"Nextwave {trv.Property("NextWave").GetValue<int>()}");
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
        }
        return true;
        
    }
}
[HarmonyPatch(typeof(ZS_PlayerControl), "MovementSpeed", MethodType.Getter)]                //Speedhack mod
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
    static void Prefix(ref ZS_ConnectedPlayerObject player, ref float damage)
    {
        if(player==null && Plugin.EnableGodmode.Value) //Not a damage by a player but TO a player
        {
            damage = 0f;
        }
    }
    static void Postfix(ref bool __result)
    {
        //Plugin.Logger.LogInfo("Postfix infinitedamage patcher was here");
        __result = true;   // Will use     Plugin.InfiniteDamage.Value
    }
}


//[HarmonyPatch(typeof(ZS_Health), "OnTakeDamage")]
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
            //float armour = tvr.Field("armour").GetValue<float>();
            damage = health*2;
            //Plugin.Logger.LogInfo($"[Prefix] player : {player}, damage : {damage}, health : {health}");

        }
        
    }
    static void Postfix(ZS_ConnectedPlayerObject player, float damage, bool __result, ZS_Health __instance)
    {
        if (player != null && Plugin.InfiniteDamage.Value)
        {
            var tvr = Traverse.Create(__instance);
            //float armour = tvr.Field("armour").GetValue<float>();
            float health = tvr.Field("health").GetValue<float>();
            if(health>0)
            {
                Plugin.Logger.LogWarning($"[InfiniteDamage] The zombie wasnt one shot ! health={health}, damage={damage}, return={__result}");
            }

        }

    }
}