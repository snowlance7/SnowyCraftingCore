using HarmonyLib;
using SnowyCraftingCore.Interfaces;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using UnityEngine;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace SnowyCraftingCore
{
    [HarmonyPatch]
    internal static class TESTING
    {
        static bool open = false;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            try
            {
                if (!Utils.testing) { return; }
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.testing) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                switch (args[0])
                {
                    case "/recipes":
                        CraftingAPI.LogRecipies();
                        break;
                    case "/power":
                        if (ApparatusPowerPort.Instance == null || args.Length == 1 || !float.TryParse(args[1], out float amount)) { return; }
                        bool result = ApparatusPowerPort.Instance.UsePower(amount);
                        HUDManager.Instance.DisplayTip("UsePower", $"UsePower {(result ? "success" : "failed")}");
                        break;
                    case "/powera":
                        if (args.Length == 1 || !float.TryParse(args[1], out float amount2)) { return; }
                        var apparatus = GameObject.FindObjectOfType<LungProp>();
                        if (apparatus == null) { return; }
                        bool result2 = ((ILungPropInterface)apparatus).UsePower(amount2);
                        HUDManager.Instance.DisplayTip("UsePowera", $"UsePowera {(result2 ? "success" : "failed")}");
                        break;
                    case "/test":
                        HUDManager.Instance.DisplayTip("test", "test");
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                return;
            }
        }
    }
}