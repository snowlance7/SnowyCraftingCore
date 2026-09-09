using HarmonyLib;
using SnowyLib;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    public class SmallItemDispenser : NetworkBehaviour
    {
        public static SmallItemDispenser? Instance { get; private set; } = null!;
        private static Terminal terminal = null!;

        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] AudioClip openSFX = null!;
        [SerializeField] AudioClip closeSFX = null!;
        [SerializeField] Animator animator = null!;
        [SerializeField] Transform itemPosition = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;

        Coroutine? routine;

        public bool IsBeingUsed => routine != null;

        public bool AwaitingItemPlacement => AwaitingItem != null;

        public Item? AwaitingItem { get; private set; }

        public GrabbableObject? ItemInSlot { get; private set; }

        Transform? parentObject;

        static Vector3 positionOffset = new Vector3(-0.55f, 0.99f, 0.6f);
        static Vector3 rotationOffset = new Vector3(90, 0, 0);

        internal static void Init()
        {
            if (!IsServerOrHost) { return; }
            if (SnowyCraftingCoreContentHandler.Instance.SmallItemDispenser == null) { return; }
            terminal = FindObjectOfType<Terminal>();
            var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.SmallItemDispenser.SmallItemDispenserPrefab, terminal.gameObject.transform.parent.parent.parent);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);

            positionOffset = PluginInstance.Config.Bind("Small Item Dispenser Options", "Position Offset", new Vector3(-0.55f, 0.99f, 0.6f), "Position offset from the terminals position").Value;
            rotationOffset = PluginInstance.Config.Bind("Small Item Dispenser Options", "Rotation Offset", new Vector3(90, 0, 0), "Rotation offset from the terminals position").Value;
        }

        private void Start()
        {
            Instance ??= this;
            transform.SetParent(terminal.gameObject.transform.parent.parent.parent);
            parentObject = terminal.gameObject.transform.parent.parent;
        }

        private void Update()
        {
            interactTriggerCollider.enabled = AwaitingItemPlacement;
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties == AwaitingItem;
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

            if (ItemInSlot != null)
            {
                ItemInSlot.transform.position = itemPosition.position;
                ItemInSlot.transform.rotation = itemPosition.rotation;
            }
        }

        public void OnInteract()
        {
            if (AwaitingItem == null || localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties != AwaitingItem || localPlayer.isGrabbingObjectAnimation) { return; }
            GrabbableObject insertingItem = localPlayer.currentlyHeldObjectServer;
            localPlayer.DiscardHeldObject(true, NetworkObject, NetworkObject.transform.InverseTransformPoint(itemPosition.position), false);
            SetItemInSlotRpc(insertingItem.NetworkObject);
        }

        private void OpenPort(bool open)
        {
            animator.SetBool("open", open);

            if (open)
                audioSource.PlayOneShot(openSFX);
            else
                audioSource.PlayOneShot(closeSFX);
        }

        public void ItemModificationOperation(Item inputItem, Action<GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemModificationOperation()
            {
                yield return null;

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItem = inputItem;

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                AwaitingItem = null;

                OpenPort(false);

                elapsedTime = 0f;
                while (elapsedTime < 1f)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;

                    if (ItemInSlot != null)
                        ItemInSlot.transform.position = itemPosition.position;
                }

                if (ItemInSlot == null)
                {
                    AwaitingItem = null;
                    logger.LogError("Operation failed, no item was inserted into the item slot");
                    routine = null;
                    yield break;
                }

                audioSource.Play();
                yield return new WaitForSeconds(operationTime);
                audioSource.Stop();
                operation.Invoke(ItemInSlot);

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                elapsedTime = 0f;
                while (elapsedTime < outputTime && ItemInSlot.playerHeldBy == null && !ItemInSlot.isHeldByEnemy)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot.playerHeldBy != null || ItemInSlot.isHeldByEnemy)
                    ItemInSlot = null;

                OpenPort(false);
                yield return new WaitForSeconds(1f);

                if (ItemInSlot != null && IsServer)
                    ItemInSlot.NetworkObject.Despawn(destroy: true);

                ItemInSlot = null;
                routine = null;
            }

            if (routine != null) { logger.LogError("Operation failed, dispenser is in use"); return; }
            routine = StartCoroutine(itemModificationOperation());
        }

        public void ItemExchangeOperation(Item inputItem, Item outputItem, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemExchangeOperation()
            {
                yield return null;

                // Open chute
                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItem = inputItem;

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }
                AwaitingItem = null;

                OpenPort(false);

                elapsedTime = 0f;
                while (elapsedTime < 1f)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;

                    //if (ItemInSlot != null) ItemInSlot.transform.position = itemPosition.position;
                }

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, no item was inserted into the item slot");
                    routine = null;
                    yield break;
                }

                if (IsServer)
                    ItemInSlot.NetworkObject.Despawn(destroy: true);

                ItemInSlot = null;

                audioSource.Play();
                yield return new WaitForSeconds(operationTime);
                audioSource.Stop();

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(outputItem, itemPosition); // TODO: Test this
                    if (ItemInSlot == null)
                    {
                        logger.LogError("Operation failed, failed to spawn item");
                        routine = null;
                        yield break;
                    }

                    yield return new WaitUntil(() => ItemInSlot.NetworkObject != null && ItemInSlot.NetworkObject.IsSpawned);
                    SetItemInSlotRpc(ItemInSlot.NetworkObject);
                }

                elapsedTime = 0f;
                while (ItemInSlot == null && elapsedTime < 10)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, failed to spawn item");
                    routine = null;
                    yield break;
                }

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                elapsedTime = 0f;
                while (elapsedTime < outputTime && ItemInSlot.playerHeldBy == null && !ItemInSlot.isHeldByEnemy)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot.playerHeldBy != null || ItemInSlot.isHeldByEnemy)
                    ItemInSlot = null;

                OpenPort(false);
                yield return new WaitForSeconds(1f);

                if (ItemInSlot != null && IsServer)
                    ItemInSlot.NetworkObject.Despawn(destroy: true);

                ItemInSlot = null;
                routine = null;
            }

            if (routine != null) { logger.LogError("Operation failed, dispenser is in use"); return; }
            routine = StartCoroutine(itemExchangeOperation());
        }

        public void ItemDispenseOperation(Item item, float outputTime)
        {
            IEnumerator itemDispenseOperation()
            {
                yield return null;

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(item, itemPosition); // TODO: Test this
                    if (ItemInSlot == null)
                    {
                        logger.LogError("Operation failed, failed to spawn item");
                        routine = null;
                        yield break;
                    }

                    yield return new WaitUntil(() => ItemInSlot.NetworkObject != null && ItemInSlot.NetworkObject.IsSpawned);
                    SetItemInSlotRpc(ItemInSlot.NetworkObject);
                }

                float elapsedTime = 0f;
                while (ItemInSlot == null && elapsedTime < 10)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, failed to spawn item");
                    routine = null;
                    yield break;
                }

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                elapsedTime = 0f;
                while (elapsedTime < outputTime && ItemInSlot.playerHeldBy == null && !ItemInSlot.isHeldByEnemy)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot.playerHeldBy != null || ItemInSlot.isHeldByEnemy)
                    ItemInSlot = null;

                OpenPort(false);
                yield return new WaitForSeconds(1f);

                if (ItemInSlot != null && IsServer)
                    ItemInSlot.NetworkObject.Despawn(destroy: true);

                ItemInSlot = null;
                routine = null;
            }

            if (routine != null) { logger.LogError("Operation failed, dispenser is in use"); return; }
            routine = StartCoroutine(itemDispenseOperation());
        }

        public void ItemDispenseOperation(Item item, Action<GrabbableObject> operation, float outputTime)
        {
            IEnumerator itemDispenseOperation()
            {
                yield return null;

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(item, itemPosition, worldPositionStays: true); // TODO: Test this
                    if (ItemInSlot == null)
                    {
                        logger.LogError("Operation failed, failed to spawn item");
                        routine = null;
                        yield break;
                    }

                    yield return new WaitUntil(() => ItemInSlot.NetworkObject != null && ItemInSlot.NetworkObject.IsSpawned);
                    SetItemInSlotRpc(ItemInSlot.NetworkObject);
                }

                float elapsedTime = 0f;
                while (ItemInSlot == null && elapsedTime < 10)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, failed to spawn item");
                    routine = null;
                    yield break;
                }

                operation.Invoke(ItemInSlot);

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                elapsedTime = 0f;
                while (elapsedTime < outputTime && ItemInSlot.playerHeldBy == null && !ItemInSlot.isHeldByEnemy)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot.playerHeldBy != null || ItemInSlot.isHeldByEnemy)
                    ItemInSlot = null;

                OpenPort(false);
                yield return new WaitForSeconds(1f);

                if (ItemInSlot != null && IsServer)
                    ItemInSlot.NetworkObject.Despawn(destroy: true);

                ItemInSlot = null;
                routine = null;
            }

            if (routine != null) { logger.LogError("Operation failed, dispenser is in use"); return; }
            routine = StartCoroutine(itemDispenseOperation());
        }

        [Rpc(SendTo.Everyone)]
        private void SetItemInSlotRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }
            ItemInSlot = item;
            //ItemInSlot.transform.SetParent(itemPosition, true);
        }
    }

    [HarmonyPatch]
    internal static class SmallItemDispenserPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static void Terminal_Start_Postfix(Terminal __instance)
        {
            try
            {
                SmallItemDispenser.Init();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
