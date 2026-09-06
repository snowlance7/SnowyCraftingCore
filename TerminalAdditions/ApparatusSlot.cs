using HarmonyLib;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    internal class ApparatusSlot : NetworkBehaviour
    {
        public static ApparatusSlot Instance { get; private set; } = null!;
        public static Terminal terminal = null!;

        public Animator animator = null!;
        public Transform apparatusPosition = null!;
        public InteractTrigger interactTrigger = null!;
        public Collider interactTriggerCollider = null!;

        public static void Init()
        {
            if (!IsServerOrHost) { return; }
            var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.SnowyCraftingCoreAssets!.ApparatusSlotPrefab, terminal.gameObject.transform.parent.parent);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
        }

        public void Start()
        {
            Instance ??= this;
            transform.SetParent(terminal.gameObject.transform.parent.parent);
        }

        public void Update()
        {
            // TODO
        }

        public void OnInteract()
        {
            // TODO
        }
    }

    [HarmonyPatch]
    internal static class TerminalDispenserPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static void Terminal_Start_Postfix(Terminal __instance)
        {
            try
            {
                ApparatusSlot.terminal = __instance;
                ApparatusSlot.Init();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
