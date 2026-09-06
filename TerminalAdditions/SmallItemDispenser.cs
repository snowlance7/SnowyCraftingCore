using HarmonyLib;
using SnowyLib;
using System;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    internal class SmallItemDispenser : NetworkBehaviour
    {
        public static SmallItemDispenser Instance { get; private set; } = null!;
        public static Terminal terminal = null!;

        public Animator animator = null!;
        public Transform itemPosition = null!;
        public InteractTrigger interactTrigger = null!;
        public Collider interactTriggerCollider = null!;

        bool awaitingItemPlacement;

        public static void Init()
        {
            if (!IsServerOrHost) { return; }
            var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.SnowyCraftingCoreAssets!.SmallItemDispenserPrefab, terminal.gameObject.transform.parent.parent);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
        }

        public void Start()
        {
            Instance ??= this;
            transform.SetParent(terminal.gameObject.transform.parent.parent);
        }

        public void Update()
        {
            interactTriggerCollider.enabled = awaitingItemPlacement;
        }

        public void OnInteract()
        {
            // TODO
        }
    }

    public class DispenserOperation(Item item, Action<GrabbableObject> onDispense)
    {
        public readonly Item item = item;
        public readonly Action<GrabbableObject> onDispense = onDispense;
    }

    [HarmonyPatch]
    internal static class SmallItemDispenserPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static void Terminal_Start_Postfix(Terminal __instance)
        {
            try
            {
                SmallItemDispenser.terminal = __instance;
                SmallItemDispenser.Init();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
