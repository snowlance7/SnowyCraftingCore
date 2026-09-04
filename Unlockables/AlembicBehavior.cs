using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore.Unlockables
{
    internal class AlembicBehavior : NetworkBehaviour
    {
        public static List<DistilleryRecipe> RegisteredRecipes { get; internal set; } = [];

        [SerializeField] InteractTrigger inputTrigger = null!;

        [SerializeField] Collider inputTriggerCollider = null!;
        [SerializeField] Collider outputTriggerCollider = null!;

        [SerializeField] MeshRenderer inputRenderer = null!;
        [SerializeField] MeshRenderer outputRenderer = null!;

        [SerializeField] ParticleSystem inputParticleSystem = null!;
        [SerializeField] ParticleSystem outputParticleSystem = null!;

        [SerializeField] AudioSource audioSource = null!;

        ChemistryIngredient? inputIngredient;
        ChemistryIngredient? outputIngredient;

        DistilleryRecipe? currentlyMixingRecipe;

        ParticleSystemRenderer inputParticleSystemRenderer = null!;

        bool mixing;
        ChemistryLiquidAppearance inputDefaultColor = null!;
        const float defaultMixingTime = 10f;

        public void Awake()
        {
            inputDefaultColor = new ChemistryLiquidAppearance(inputRenderer.material.color, 2f);
            inputParticleSystemRenderer = inputParticleSystem.GetComponent<ParticleSystemRenderer>();
        }

        public void Update()
        {
            inputTriggerCollider.enabled = inputIngredient == null && localPlayer.currentlyHeldObjectServer != null;
            inputTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            outputTriggerCollider.enabled = outputIngredient != null;
        }

        public void InputTrigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject);
        }

        public void OutputTrigger_Interact()
        {
            if (inputIngredient == null && outputIngredient == null) { return; }
            OutputTrigger_InteractRpc(localPlayer.actualClientId);
        }

        private void SetInputFlaskColor(ChemistryLiquidAppearance color)
        {
            //inputRenderer.enabled = true;
            inputRenderer.material.color = color.liquidColor;
            inputRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            inputRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
            inputParticleSystemRenderer.material.color = color.liquidColor;
            inputParticleSystemRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            inputParticleSystemRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);

        }

        private void SetOutputFlaskColor(ChemistryLiquidAppearance color)
        {
            outputRenderer.enabled = true;
            outputRenderer.sharedMaterial.color = color.liquidColor;
            outputRenderer.sharedMaterial.SetColor("_EmissionColor", color.liquidColor);
            outputRenderer.sharedMaterial.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone)]
        private void InputIngredientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            inputIngredient = null;
            currentlyMixingRecipe = null;

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = true;

            if (item is IChemistryIngredient _ingredient)
            {
                ingredient = _ingredient.GetIngredient();

                if (ingredient != null && _ingredient is IDistillableIngredient distillableIngredient)
                {
                    currentlyMixingRecipe = new DistilleryFixedOutputReaction(ingredient, distillableIngredient.DistilleryOutput(), distillableIngredient.DistilleryMixTime());
                    despawningIngredientItem = distillableIngredient.DespawnItemAfterDistilleryInput();
                }
                logger.LogDebug($"Got IChemistryIngredient: {ingredient?.ToString()}");
            }

            ingredient ??= ChemicalMixerBehavior.RegisteredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                logger.LogDebug($"Unable to find registered ingredient for {item.name}, creating default ingredient");
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, 5f));
            }

            inputIngredient = ingredient;
            SetInputFlaskColor(ingredient.chemistryLiquidAppearance);

            currentlyMixingRecipe ??= RegisteredRecipes.Where(x => x.ingredient.Equals(ingredient)).FirstOrDefault();
            logger.LogDebug(currentlyMixingRecipe != null ? "Recipe found" : "Recipe not found");

            if (localPlayer == item.playerHeldBy && despawningIngredientItem)
                localPlayer.DespawnHeldObject();

            mixing = true;
            MixIngredients();
        }

        [Rpc(SendTo.Everyone)]
        private void OutputTrigger_InteractRpc(ulong clientId)
        {
            if (outputIngredient == null || mixing) { return; }

            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            if (IsServer && !(player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is IChemistryOutputContainer container && container.ReceiveChemistryOutput(outputIngredient)))
            {
                GrabbableObject? outputItem = Utils.SpawnItem(outputIngredient!.item.GetDawnInfo().TypedKey, player.transform.position);
                if (outputItem != null)
                {
                    IEnumerator sendSpawnOutputIngredient(string specialInstructions)
                    {
                        yield return new WaitUntil(() => outputItem.NetworkObject != null && outputItem.NetworkObject.IsSpawned);
                        SpawnOutputIngredientRpc(clientId, outputItem.NetworkObject, specialInstructions);
                    }

                    StartCoroutine(sendSpawnOutputIngredient(outputIngredient.specialInstructions));
                }
            }

            outputParticleSystem.Stop();
            outputRenderer.enabled = false;
            outputIngredient = null;
        }

        [Rpc(SendTo.Everyone)]
        private void SpawnOutputIngredientRpc(ulong clientId, NetworkObjectReference netRef, string specialInstructions)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            if (item is IChemistryIngredient ingredient)
                ingredient.OnChemicalMixerOutput(specialInstructions);

            if (localPlayer.actualClientId == clientId)
                localPlayer.GrabGrabbableObject(item);
        }

        private void MixIngredients()
        {
            IEnumerator mixIngredients()
            {
                yield return null;

                audioSource.Play();

                float mixTime = currentlyMixingRecipe != null && currentlyMixingRecipe.mixTime > 0 ? currentlyMixingRecipe.mixTime : defaultMixingTime;

                yield return new WaitForSeconds(mixTime);

                if (currentlyMixingRecipe != null)
                {
                    outputIngredient = currentlyMixingRecipe.reaction.Invoke(inputIngredient!);
                    if (outputIngredient != null)
                    {
                        SetOutputFlaskColor(outputIngredient.chemistryLiquidAppearance);
                        outputParticleSystem.Play();
                    }
                }

                SetInputFlaskColor(inputDefaultColor);
                currentlyMixingRecipe = null;
                inputIngredient = null;
                mixing = false;
            }

            StartCoroutine(mixIngredients());
        }
    }
}
