using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.Unlockables
{
    internal class AnalyzerBehavior : NetworkBehaviour
    {
        public static List<AnalyzableIngredient> RegisteredAnalyzableIngredients { get; internal set; } = [];

        [SerializeField] Animator animator = null!;
        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] InteractTrigger interactTrigger = null!;
        [SerializeField] Collider interactTriggerCollider = null!;
        [SerializeField] MeshRenderer testTubeRenderer = null!;

        AnalyzableIngredient? analyzingIngredient;

        bool inAnimation;

        public void Update()
        {
            interactTrigger.interactable = localPlayer.currentlyHeldObjectServer != null;
            interactTriggerCollider.enabled = !inAnimation;
        }

        public void OnFinishSpinning() // Animation
        {
            inAnimation = false;
            var ingredient = analyzingIngredient;
            analyzingIngredient = null;
            ingredient?.result.Invoke(ingredient);
        }

        public void OnTriggerInteract() // Interact trigger
        {
            var obj = localPlayer.currentlyHeldObjectServer;
            if (inAnimation || obj == null || (obj is not IAnalyzableIngredient && !RegisteredAnalyzableIngredients.Any(x => x.item == obj.itemProperties))) { return; }
            ProcessIngredientRpc(obj.NetworkObject);
        }

        private void SetTestTubeColor(ChemistryLiquidAppearance color)
        {
            testTubeRenderer.material.color = color.liquidColor;
            testTubeRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            testTubeRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone)]
        private void ProcessIngredientRpc(NetworkObjectReference netRef)
        {
            if (inAnimation) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            AnalyzableIngredient? ingredient = null;
            bool despawningIngredientItem = true;

            if (item is IAnalyzableIngredient _ingredient)
            {
                ingredient = _ingredient.GetAnalyzableIngredient();
                despawningIngredientItem = _ingredient.DespawnItemAfterAnalyzing();
            }

            ingredient ??= RegisteredAnalyzableIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null) { return; }

            analyzingIngredient = ingredient;
            SetTestTubeColor(ingredient.chemistryLiquidAppearance);

            if (localPlayer == item.playerHeldBy && despawningIngredientItem)
                localPlayer.DespawnHeldObject();

            inAnimation = true;
            audioSource.Play();
            animator.SetTrigger("spin");
        }
    }
}
