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
            else if (localPlayer.currentlyHeldObjectServer != null && (localPlayer.currentlyHeldObjectServer is IAnalyzableIngredient || RegisteredIngredients.Any(x => x.item == localPlayer.currentlyHeldObjectServer.itemProperties)))
            {
                ProcessIngredientRpc(localPlayer.actualClientId, localPlayer.currentlyHeldObjectServer.NetworkObject);
            }
        }

        private void SetTestTubeColor(ChemistryLiquidAppearance color)
        {
            testTubeFluidRenderer.material.color = color.liquidColor;
            testTubeFluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            testTubeFluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone)]
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

        [Rpc(SendTo.Everyone)]
        private void ProcessIngredientRpc(ulong clientId, NetworkObjectReference netRef)
        {
            if (inAnimation) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            AnalyzableIngredient? ingredient = null;

            if (item is IAnalyzableIngredient _ingredient)
            {
                ChemistryIngredient? chemistryIngredient = _ingredient.GetIngredient();
                chemistryIngredient ??= new ChemistryIngredient(item.itemProperties);
                ingredient = new AnalyzableIngredient(item.itemProperties, _ingredient.OnAnalyze(), chemistryIngredient.chemistryLiquidAppearance, chemistryIngredient.specialInstructions, despawnItem: _ingredient.DespawnItemOnAnalyze(), holdItem: _ingredient.HoldItem());
            }

            ingredient ??= RegisteredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null) { return; }

            analyzingIngredient = ingredient;
            SetTestTubeColor(ingredient.chemistryLiquidAppearance);

            if (ingredient.despawnItem)
            {
                if (localPlayer == item.playerHeldBy)
                    localPlayer.DespawnHeldObject();
            }
            else if (ingredient.holdItem)
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
