using HarmonyLib;
using InjectionLibrary;
using InjectionLibrary.Attributes;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

[assembly: RequiresInjections]
[assembly: HandleErrors(ErrorHandlingStrategy.LogError)]

namespace SnowyCraftingCore.Interfaces;

[HandleErrors(ErrorHandlingStrategy.LogError)]
[InjectInterface(typeof(LungProp))]
public interface ILungPropInterface
{
    float PowerRemaining { get; set; }

    float StartingLightIntensity { get; set; }

    [HandleErrors(ErrorHandlingStrategy.LogError)]
    public bool CanUsePower(float amount)
    {
        return (PowerRemaining - amount) >= 0;
    }

    [HandleErrors(ErrorHandlingStrategy.LogError)]
    public bool UsePower(float amount)
    {
        if ((PowerRemaining - amount) < 0) { return false; }
        SetPowerRemaining(PowerRemaining - amount);
        return true;
    }

    [HandleErrors(ErrorHandlingStrategy.LogError)]
    void SetPowerRemaining(float value)
    {
        PowerRemaining = value;

        float powerUsed = 1f - PowerRemaining;

        Color emission = Color.Lerp(
            new Color(5.99215698f, 5.70980406f, 2.47843146f, 0.921568632f),
            Color.black,
            powerUsed
        );

        ((LungProp)this).lungDeviceMesh.materials[1].SetColor("_EmissiveColor", emission);
        ((LungProp)this).gameObject.transform.Find("Point Light").GetComponent<Light>().intensity = Mathf.Lerp(StartingLightIntensity, 0f, powerUsed);
        ((LungProp)this).SetScrapValue((int)Mathf.Lerp(80, 40, powerUsed));
    }
}

[HarmonyPatch]
internal static class ILungPropInterfacePatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(LungProp), nameof(LungProp.Start))]
    public static void LungProp_Start_Postfix(LungProp __instance)
    {
        try
        {
            ((ILungPropInterface)__instance).PowerRemaining = 1f;
            ((ILungPropInterface)__instance).StartingLightIntensity = __instance.gameObject.transform.Find("Point Light").GetComponent<Light>().intensity;
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemDataToSave))]
    public static void LungProp_GetItemDataToSave_Postfix(GrabbableObject __instance, ref int __result)
    {
        try
        {
            if (__instance is not LungProp) { return; }
            __result = (int)(((ILungPropInterface)__instance).PowerRemaining * 100);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
    public static void LungProp_LoadItemSaveData_Postfix(GrabbableObject __instance, int saveData)
    {
        try
        {
            if (__instance is not LungProp) { return; }
            ((ILungPropInterface)__instance).SetPowerRemaining(saveData / 100);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
        }
    }
}
