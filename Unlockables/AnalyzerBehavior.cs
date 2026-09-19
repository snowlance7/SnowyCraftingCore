using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.Unlockables
{
    internal class AnalyzerBehavior : NetworkBehaviour
    {
        public static List<AnalyzableIngredient> RegisteredIngredients { get; internal set; } = [];

        [SerializeField] Animator animator = null!;
        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;
        [SerializeField] MeshRenderer testTubeRenderer = null!;
        [SerializeField] MeshRenderer testTubeFluidRenderer = null!;

        AnalyzableIngredient? analyzingIngredient;

        PlayerControllerB? playerAnalyzing;
        GrabbableObject? heldObject;

        bool inAnimation;

        public void Update()
        {
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null || heldObject != null;
            interactTrigger.hoverTip = heldObject != null ? "Take ingredient [E]" : "Analyze [E]";
            interactTriggerCollider.enabled = !inAnimation;
        }

        public void OnFinishSpinning() // Animation
        {
            if (!analyzingIngredient!.holdItem)
            {
                testTubeRenderer.enabled = false;
                testTubeFluidRenderer.enabled = false;
            }

            inAnimation = false;
            var ingredient = analyzingIngredient;
            var player = playerAnalyzing;
            analyzingIngredient = null;
            playerAnalyzing = null;
            audioSource.Stop();
            ingredient?.result.Invoke(ingredient, player!);
        }

        public void OnTriggerInteract() // Interact trigger
        {
            if (inAnimation) { return; }

            if (heldObject != null)
            {
                GrabHeldItemRpc(localPlayer.actualClientId);
            }
            else if (localPlayer.currentlyHeldObjectServer != null && (localPlayer.currentlyHeldObjectServer is IAnalyzableIngredient || RegisteredIngredients.Any(x => x.item == localPlayer.currentlyHeldObjectServer.itemProperties.GetDawnInfo().Key)))
            {
                ProcessIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject);
            }
        }

        private void SetTestTubeColor(ChemistryLiquidAppearance color)
        {
            testTubeFluidRenderer.material.color = color.liquidColor;
            testTubeFluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            testTubeFluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void GrabHeldItemRpc(ulong clientId)
        {
            if (heldObject == null) { return; }
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            heldObject.EnableItemMeshes(true);
            heldObject.EnablePhysics(true);

            if (player == localPlayer)
                player.GrabGrabbableObject(heldObject);

            heldObject = null;
            testTubeRenderer.enabled = false;
            testTubeFluidRenderer.enabled = false;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void ProcessIngredientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { logger.LogError("Failed to get networkobject from networkobjectreference"); return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { logger.LogError("Failed to get grabbableobject from networkobject"); return; }

            NamespacedKey<DawnItemInfo> itemKey = item.itemProperties.GetDawnInfo().TypedKey;
            AnalyzableIngredient? ingredient = null;

            if (item is IAnalyzableIngredient _ingredient)
            {
                ChemistryIngredient? chemistryIngredient = _ingredient.GetIngredient();
                chemistryIngredient ??= new ChemistryIngredient(itemKey);
                ingredient = new AnalyzableIngredient(itemKey, _ingredient.OnAnalyze(), chemistryIngredient.chemistryLiquidAppearance, chemistryIngredient.specialInstructions, holdItem: _ingredient.HoldItem());
            }

            ingredient ??= RegisteredIngredients.Where(x => x.item == itemKey).FirstOrDefault();

            if (ingredient == null) { return; }

            analyzingIngredient = ingredient;
            SetTestTubeColor(ingredient.chemistryLiquidAppearance);

            if (ingredient.holdItem)
            {
                if (localPlayer == item.playerHeldBy)
                    localPlayer.DiscardHeldObject(true, NetworkObject, transform.position, false);

                item.EnableItemMeshes(false);
                item.EnablePhysics(false);
                heldObject = item;
            }

            testTubeFluidRenderer.enabled = true;
            testTubeRenderer.enabled = true;
            inAnimation = true;
            audioSource.Play();
            animator.SetTrigger("spin");
        }
    }
}
