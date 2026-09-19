using Dawn;
using HarmonyLib;
using SnowyCraftingCore.Interfaces;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    public class ApparatusPowerPort : NetworkBehaviour
    {
        public static ApparatusPowerPort? Instance { get; private set; } = null!;

        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] AudioClip openSFX = null!;
        [SerializeField] AudioClip closeSFX = null!;
        [SerializeField] Animator animator = null!;
        [SerializeField] Transform apparatusPosition = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;

        LungProp? apparatusInSlot;

        public bool IsApparatusInSlot => apparatusInSlot != null;

        public float ApparatusPowerLeft { get; private set; }

        public bool IsOpen { get; private set; }

        static Transform TerminalTransform => Utils.terminal.gameObject.transform.parent.parent;
        static Transform HangarShipTransform => Utils.terminal.gameObject.transform.parent.parent.parent;

        static Vector3 positionOffset = new Vector3(-0.05f, 0.395f, -0.7f);
        static Vector3 rotationOffset = new Vector3(90, 0, 0);

        internal static void Init()
        {
            if (IsServerOrHost)
            {
                if (SnowyCraftingCoreContentHandler.Instance.ApparatusPowerPort == null) { return; }
                var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.ApparatusPowerPort.ApparatusPowerPortPrefab, HangarShipTransform);
                obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
            }

            positionOffset = PluginInstance.Config.Bind("Apparatus Power Port Options", "Position Offset", new Vector3(-0.05f, 0.395f, -0.7f), "Position offset from the terminals position").Value;
            rotationOffset = PluginInstance.Config.Bind("Apparatus Power Port Options", "Rotation Offset", new Vector3(90, 0, 0), "Rotation offset from the terminals position").Value;

			TerminalAPI.RegisterTerminalCommand(new TerminalCommand("power", (args) =>
			{
                string message = $"{(Instance!.IsOpen ? "Closing" : "Opening")} apparatus power port\n\n";
                Instance!.TogglePortRpc();
                return message;
			}, "Other", "Power", "Open/close the apparatus power port"));
		}

        internal void Start()
        {
            Instance ??= this;
            transform.SetParent(HangarShipTransform);
        }

        internal void Update()
        {
            if (localPlayer == null) { return; }
            interactTriggerCollider.enabled = !IsApparatusInSlot && IsOpen;
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer is LungProp;

            if (apparatusInSlot != null)
            {
                if (apparatusInSlot.playerHeldBy != null || apparatusInSlot.isHeldByEnemy)
                {
                    var audioSource = apparatusInSlot.gameObject.GetComponent<AudioSource>();
                    audioSource.Stop();
                    audioSource.loop = false;
                    apparatusInSlot = null;
                }
            }
        }

        private void LateUpdate()
        {
            base.transform.rotation = TerminalTransform.rotation;
            base.transform.Rotate(rotationOffset);
            base.transform.position = TerminalTransform.position;
            Vector3 _positionOffset = positionOffset;
            _positionOffset = TerminalTransform.rotation * _positionOffset;
            base.transform.position += _positionOffset;

            if (apparatusInSlot != null)
            {
                apparatusInSlot.transform.position = apparatusPosition.position;
                apparatusInSlot.transform.rotation = apparatusPosition.rotation;
            }
        }

        private void TogglePort()
        {
            IsOpen = !IsOpen;
            animator.SetBool("open", IsOpen);

            if (IsOpen)
                audioSource.PlayOneShot(openSFX);
            else
                audioSource.PlayOneShot(closeSFX);

            if (apparatusInSlot != null)
            {
                apparatusInSlot.EnablePhysics(IsOpen);
                apparatusInSlot.EnableItemMeshes(IsOpen);
                apparatusInSlot.gameObject.GetComponentInChildren<Light>().enabled = IsOpen;

                var audioSource = apparatusInSlot.gameObject.GetComponent<AudioSource>();

                if (IsOpen)
                {
                    if (((ILungPropInterface)apparatusInSlot).PowerRemaining > 0f)
                    {
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                }
                else
                {
                    audioSource.Stop();
                }
            }
        }

        public bool CanUsePower(float amount)
        {
            if (apparatusInSlot == null) { return false; }
            return ((ILungPropInterface)apparatusInSlot).CanUsePower(amount);
        }

        public bool UsePower(float amount)
        {
            if (apparatusInSlot == null) { return false; }
            bool canUsePower = ((ILungPropInterface)apparatusInSlot).CanUsePower(amount);
            if (canUsePower)
            {
                UsePowerRpc(amount);
            }
            return canUsePower;
        }

        public void OnInteract() // TODO: Test this
        {
            if (IsApparatusInSlot || localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer is not LungProp || localPlayer.isGrabbingObjectAnimation) { return; }
            GrabbableObject insertingItem = localPlayer.currentlyHeldObjectServer;
            localPlayer.DiscardHeldObject(true, NetworkObject, NetworkObject.transform.InverseTransformPoint(apparatusPosition.position), false);
            SetApparatusInSlotRpc(insertingItem.NetworkObject);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void UsePowerRpc(float amount)
        {
            if (apparatusInSlot == null) { logger.LogError("Cant use power, apparatus not in slot"); return; }
            ((ILungPropInterface)apparatusInSlot).UsePower(amount);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void SetApparatusInSlotRpc(NetworkObjectReference netRef)
        {
            if (IsApparatusInSlot) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out LungProp apparatus)) { return; }
            apparatusInSlot = apparatus;
            apparatusInSlot.isLungDockedInElevator = true;

            var audioSource = apparatusInSlot.gameObject.GetComponent<AudioSource>();
            audioSource.PlayOneShot(apparatusInSlot.connectSFX);
            audioSource.loop = true;
            audioSource.Play();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void TogglePortRpc()
        {
            TogglePort();
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
