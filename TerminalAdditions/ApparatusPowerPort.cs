using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    internal class ApparatusPowerPort : NetworkBehaviour
    {
        public static ApparatusPowerPort? Instance { get; private set; } = null!;
        private static Terminal terminal = null!;

        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] AudioClip openSFX = null!;
        [SerializeField] AudioClip closeSFX = null!;
        [SerializeField] Animator animator = null!;
        [SerializeField] Transform apparatusPosition = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;

        LungProp? apparatusInSlot;

        bool slotOpen;

        Transform? parentObject;

        static Vector3 positionOffset = new Vector3(-0.05f, 0.395f, -0.7f);
        static Vector3 rotationOffset = new Vector3(90, 0, 0);

        internal static void Init()
        {
            if (!IsServerOrHost) { return; }
            if (SnowyCraftingCoreContentHandler.Instance.ApparatusPowerPort == null) { return; }
            terminal = FindObjectOfType<Terminal>();
            var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.ApparatusPowerPort.ApparatusPowerPortPrefab, terminal.gameObject.transform.parent.parent.parent);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);

            positionOffset = PluginInstance.Config.Bind("Apparatus Power Port Options", "Position Offset", new Vector3(-0.05f, 0.395f, -0.7f), "Position offset from the terminals position").Value;
            rotationOffset = PluginInstance.Config.Bind("Apparatus Power Port Options", "Rotation Offset", new Vector3(90, 0, 0), "Rotation offset from the terminals position").Value;
        }

        internal void Start()
        {
            Instance ??= this;
            transform.SetParent(terminal.gameObject.transform.parent.parent.parent);
            parentObject = terminal.gameObject.transform.parent.parent;
        }

        internal void Update()
        {
            interactTriggerCollider.enabled = apparatusInSlot == null && slotOpen;
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer is LungProp;
        }

        private void LateUpdate()
        {
            if (parentObject != null)
            {
                base.transform.rotation = parentObject.rotation;
                base.transform.Rotate(rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 _positionOffset = positionOffset;
                _positionOffset = parentObject.rotation * _positionOffset;
                base.transform.position += _positionOffset;
            }
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
                ApparatusPowerPort.Init();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
