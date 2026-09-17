using HarmonyLib;
using SnowyLib;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.TerminalAdditions
{
    public class SmallItemDispenser : NetworkBehaviour
    {
        public static SmallItemDispenser? Instance { get; private set; } = null!;

        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] AudioClip openSFX = null!;
        [SerializeField] AudioClip closeSFX = null!;
        [SerializeField] Animator animator = null!;
        [SerializeField] Transform itemPosition = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;

        Coroutine? routine;

        public static bool DEBUG_testingSlot = false;

        public bool IsBeingUsed => routine != null;

        public bool AwaitingItemPlacement => AwaitingItems.Length > 0;

        public Item[] AwaitingItems { get; private set; } = [];

        public GrabbableObject? ItemInSlot { get; private set; }
        Vector3 itemInSlotPositionOffset = new Vector3();
        Vector3 itemInSlotRotationOffset = new Vector3();

        Transform? parentObject;

        static Vector3 positionOffset = new Vector3(-0.55f, 0.99f, 0.6f);
        static Vector3 rotationOffset = new Vector3(90, 0, 0);

        internal static void Init()
        {
            if (!IsServerOrHost) { return; }
            if (SnowyCraftingCoreContentHandler.Instance.SmallItemDispenser == null) { return; }
            var obj = Instantiate(SnowyCraftingCoreContentHandler.Instance.SmallItemDispenser.SmallItemDispenserPrefab, Utils.terminal.gameObject.transform.parent.parent.parent);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);

            positionOffset = PluginInstance.Config.Bind("Small Item Dispenser Options", "Position Offset", new Vector3(-0.55f, 0.99f, 0.6f), "Position offset from the terminals position").Value;
            rotationOffset = PluginInstance.Config.Bind("Small Item Dispenser Options", "Rotation Offset", new Vector3(90, 0, 0), "Rotation offset from the terminals position").Value;
        }

        private void Start()
        {
            Instance ??= this;
            transform.SetParent(Utils.terminal.gameObject.transform.parent.parent.parent);
            parentObject = Utils.terminal.gameObject.transform.parent.parent;
        }

        private void Update()
        {
            if (localPlayer == null) { return; }
            interactTriggerCollider.enabled = ItemInSlot == null && (AwaitingItemPlacement || DEBUG_testingSlot);
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && (AwaitingItems.Contains(localPlayer.currentlyHeldObjectServer.itemProperties) || DEBUG_testingSlot);
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
                if (ItemInSlot.isHeld) { ItemInSlot = null; return; }

                ItemInSlot.transform.rotation = itemPosition.rotation;
                ItemInSlot.transform.Rotate(itemInSlotRotationOffset);
                ItemInSlot.transform.position = itemPosition.position;
                Vector3 _positionOffset = itemInSlotPositionOffset;
                _positionOffset = itemPosition.rotation * _positionOffset;
                ItemInSlot.transform.position += _positionOffset;
            }
            else
            {
                itemInSlotPositionOffset = Vector3.zero;
                itemInSlotRotationOffset = Vector3.zero;
            }
        }

        public void OnInteract()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.isGrabbingObjectAnimation) { return; }
            if ((AwaitingItems.Length == 0 || !AwaitingItems.Contains(localPlayer.currentlyHeldObjectServer.itemProperties)) && !DEBUG_testingSlot) { return; }
            GrabbableObject insertingItem = localPlayer.currentlyHeldObjectServer;
            localPlayer.DiscardHeldObject(true, NetworkObject, NetworkObject.transform.InverseTransformPoint(itemPosition.position), false);
            SetItemInSlotRpc(insertingItem.NetworkObject);
        }

        internal void OpenPort(bool open)
        {
            animator.SetBool("open", open);

            if (open)
                audioSource.PlayOneShot(openSFX);
            else
                audioSource.PlayOneShot(closeSFX);
        }

        public void ItemModificationOperation(DispensableItem inputItem, Action<GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            ItemModificationOperation([inputItem], operation, inputTime, operationTime, outputTime);
        }

        public void ItemModificationOperation(DispensableItem[] inputItems, Action<GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemModificationOperation()
            {
                yield return null;

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItems = inputItems.Select(x => x.item).ToArray();

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot != null)
                {
                    var dispensableItemInSlot = inputItems.Where(x => x.item == ItemInSlot.itemProperties).First();
                    itemInSlotPositionOffset = dispensableItemInSlot.positionOffset;
                    itemInSlotRotationOffset = dispensableItemInSlot.rotationOffset;
                }

                AwaitingItems = [];

                OpenPort(false);

                yield return new WaitForSeconds(1f);

                if (ItemInSlot == null)
                {
                    AwaitingItems = [];
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

        public void ItemExchangeOperation(DispensableItem inputItem, DispensableItem outputItem, float inputTime, float operationTime, float outputTime)
        {
            ItemExchangeOperation([inputItem], outputItem, inputTime, operationTime, outputTime);
        }

        public void ItemExchangeOperation(DispensableItem[] inputItems, DispensableItem outputItem, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemExchangeOperation()
            {
                yield return null;

                // Open chute
                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItems = inputItems.Select(x => x.item).ToArray();

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot != null)
                {
                    var dispensableItemInSlot = inputItems.Where(x => x.item == ItemInSlot.itemProperties).First();
                    itemInSlotPositionOffset = dispensableItemInSlot.positionOffset;
                    itemInSlotRotationOffset = dispensableItemInSlot.rotationOffset;
                }

                AwaitingItems = [];

                OpenPort(false);

                yield return new WaitForSeconds(1f);

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
                    ItemInSlot = Utils.SpawnItem(outputItem.item, itemPosition); // TODO: Test this
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
                else
                {
                    itemInSlotPositionOffset = outputItem.positionOffset;
                    itemInSlotRotationOffset = outputItem.rotationOffset;
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

        public void ItemExchangeOperation(DispensableItem inputItem, DispensableItem outputItem, Action<GrabbableObject, GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            ItemExchangeOperation([inputItem], outputItem, operation, inputTime, operationTime, outputTime);
        }

        public void ItemExchangeOperation(DispensableItem[] inputItems, DispensableItem outputItem, Action<GrabbableObject, GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemExchangeOperation()
            {
                yield return null;

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItems = inputItems.Select(x => x.item).ToArray();

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot != null)
                {
                    var dispensableItemInSlot = inputItems.Where(x => x.item == ItemInSlot.itemProperties).First();
                    itemInSlotPositionOffset = dispensableItemInSlot.positionOffset;
                    itemInSlotRotationOffset = dispensableItemInSlot.rotationOffset;
                }

                AwaitingItems = [];

                OpenPort(false);

                yield return new WaitForSeconds(1f);

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, no item was inserted into the item slot");
                    routine = null;
                    yield break;
                }

                var spawnedInputItem = ItemInSlot;
                ItemInSlot = null;

                audioSource.Play();
                yield return new WaitForSeconds(operationTime);
                audioSource.Stop();

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(outputItem.item, itemPosition); // TODO: Test this
                    if (ItemInSlot == null)
                    {
                        logger.LogError("Operation failed, failed to spawn item");
                        spawnedInputItem.NetworkObject.Despawn(destroy: true);
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
                else
                {
                    itemInSlotPositionOffset = outputItem.positionOffset;
                    itemInSlotRotationOffset = outputItem.rotationOffset;
                }

                operation.Invoke(spawnedInputItem, ItemInSlot);

                if (IsServer)
                    spawnedInputItem.NetworkObject.Despawn(destroy: true);

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

        public void ItemExchangeOperation(DispensableItem[] inputItems, DispensableItem[] outputItems, Action<GrabbableObject, GrabbableObject> operation, float inputTime, float operationTime, float outputTime)
        {
            IEnumerator itemExchangeOperation()
            {
                yield return null;

                OpenPort(true);
                yield return new WaitForSeconds(1f);

                AwaitingItems = inputItems.Select(x => x.item).ToArray();

                float elapsedTime = 0f;
                while (elapsedTime < inputTime && ItemInSlot == null)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                if (ItemInSlot != null)
                {
                    var dispensableItemInSlot = inputItems.Where(x => x.item == ItemInSlot.itemProperties).First();
                    itemInSlotPositionOffset = dispensableItemInSlot.positionOffset;
                    itemInSlotRotationOffset = dispensableItemInSlot.rotationOffset;
                }

                AwaitingItems = [];

                OpenPort(false);

                yield return new WaitForSeconds(1f);

                if (ItemInSlot == null)
                {
                    logger.LogError("Operation failed, no item was inserted into the item slot");
                    routine = null;
                    yield break;
                }

                var spawnedInputItem = ItemInSlot;
                int itemIndex = Array.IndexOf(inputItems, spawnedInputItem);
                ItemInSlot = null;

                audioSource.Play();
                yield return new WaitForSeconds(operationTime);
                audioSource.Stop();

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(outputItems[itemIndex].item, itemPosition); // TODO: Test this
                    if (ItemInSlot == null)
                    {
                        logger.LogError("Operation failed, failed to spawn item");
                        spawnedInputItem.NetworkObject.Despawn(destroy: true);
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
                else
                {
                    var dispensableItemInSlot = outputItems.Where(x => x.item == ItemInSlot.itemProperties).First();
                    itemInSlotPositionOffset = dispensableItemInSlot.positionOffset;
                    itemInSlotRotationOffset = dispensableItemInSlot.rotationOffset;
                }

                operation.Invoke(spawnedInputItem, ItemInSlot);

                if (IsServer)
                    spawnedInputItem.NetworkObject.Despawn(destroy: true);

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

        public void ItemDispenseOperation(DispensableItem item, float outputTime)
        {
            IEnumerator itemDispenseOperation()
            {
                yield return null;

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(item.item, itemPosition); // TODO: Test this
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
                else
                {
                    itemInSlotPositionOffset = item.positionOffset;
                    itemInSlotRotationOffset = item.rotationOffset;
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

        public void ItemDispenseOperation(DispensableItem item, Action<GrabbableObject> operation, float outputTime)
        {
            IEnumerator itemDispenseOperation()
            {
                yield return null;

                if (IsServer)
                {
                    ItemInSlot = Utils.SpawnItem(item.item, itemPosition, worldPositionStays: true); // TODO: Test this
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
                else
                {
                    itemInSlotPositionOffset = item.positionOffset;
                    itemInSlotRotationOffset = item.rotationOffset;
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
        }
    }

    public class DispensableItem(Item item, Vector3 positionOffset = default, Vector3 rotationOffset = default)
    {
        public Item item = item;
        public Vector3 positionOffset = positionOffset;
        public Vector3 rotationOffset = rotationOffset;
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
