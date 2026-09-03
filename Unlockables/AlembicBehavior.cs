using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using System;
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
        public static List<DistilleryRecipe> RegisteredRecipies { get; internal set; } = [];

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

        bool mixing;
        const float defaultMixingTime = 10f;

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

        private void SetFlaskColor(bool outputFlask, ChemistryLiquidAppearance color)
        {
            Material material = new(outputRenderer.materials[0]);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            if (outputFlask)
            {
                outputRenderer.enabled = true;
                outputRenderer.material = material;
            }
            else
            {
                inputRenderer.enabled = true;
                inputRenderer.material = material;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void InputIngredientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = true;

            if (item is IChemistryIngredient _ingredient)
            {
                ingredient = _ingredient.GetInputIngredient();
                despawningIngredientItem = _ingredient.DespawnItemAfterInput();
            }

            ingredient ??= ChemicalMixerBehavior.RegisteredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, 5f));
            }

            inputIngredient = ingredient;
            SetFlaskColor(outputFlask: false, ingredient.chemistryLiquidAppearance);

            currentlyMixingRecipe = RegisteredRecipies.Where(x => x.ingredient == inputIngredient).FirstOrDefault();

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
                    IEnumerator sendSpawnOutputIngredient()
                    {
                        yield return new WaitUntil(() => outputItem.NetworkObject != null && outputItem.NetworkObject.IsSpawned);
                        SpawnOutputIngredientRpc(clientId, outputItem.NetworkObject, outputIngredient.specialInstructions);
                    }

                    StartCoroutine(sendSpawnOutputIngredient());
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
                ingredient.OnOutputIngredient(specialInstructions);

            if (localPlayer.actualClientId == clientId)
                localPlayer.GrabGrabbableObject(item);
        }

        private void MixIngredients()
        {
            IEnumerator mixIngredients()
            {
                yield return null;

                inputParticleSystem.Play();
                audioSource.Play();

                float mixTime = currentlyMixingRecipe != null && currentlyMixingRecipe.mixTime > 0 ? currentlyMixingRecipe.mixTime : defaultMixingTime;

                yield return new WaitForSeconds(mixTime);

                if (currentlyMixingRecipe != null)
                {
                    outputIngredient = currentlyMixingRecipe.reaction.Invoke(inputIngredient!);
                    SetFlaskColor(outputFlask: true, outputIngredient.chemistryLiquidAppearance);
                }

                inputParticleSystem.Stop();
                outputParticleSystem.Play();
                inputIngredient = null;
                inputRenderer.enabled = false;
                mixing = false;
            }

            StartCoroutine(mixIngredients());
        }
    }
}
