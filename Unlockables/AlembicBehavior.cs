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
        public static bool IsEnabled => LethalContent.Unlockables[SnowyCraftingCoreKeys.Alembic] != null;
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
        ChemistryLiquidAppearance inputDefaultColor;
        const float defaultMixingTime = 10f;

        public void Awake()
        {
            inputDefaultColor = new ChemistryLiquidAppearance(inputRenderer.material.color, 2f);
            inputParticleSystemRenderer = inputParticleSystem.GetComponent<ParticleSystemRenderer>();
        }

        public void Update()
        {
            inputTriggerCollider.enabled = inputIngredient == null && outputIngredient == null && localPlayer.currentlyHeldObjectServer != null;
            inputTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            outputTriggerCollider.enabled = outputIngredient != null;
        }

        public void InputTrigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }

            GrabbableObject item = localPlayer.currentlyHeldObjectServer;
            NamespacedKey<DawnItemInfo> itemKey = item.itemProperties.GetDawnInfo().TypedKey;

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = true;

            if (item is IChemistryIngredient _ingredient)
            {
                logger.LogDebug("item is IChemistryIngredient");
                ingredient = _ingredient.GetIngredient();
                logger.LogDebug($"Got IChemistryIngredient: {ingredient?.ToString()}");

                if (ingredient != null && _ingredient is IDistillableIngredient distillableIngredient)
                {
                    ChemistryIngredient? outputIngredient = distillableIngredient.DistilleryOutput();
                    float mixTime = distillableIngredient.DistilleryMixTime();
                    despawningIngredientItem = distillableIngredient.DespawnItemAfterDistilleryInput();

                    if (despawningIngredientItem) { localPlayer.DespawnHeldObject(); }

                    InputIngredientRpc(ingredient, outputIngredient, mixTime);
                    return;
                }
            }

            ingredient ??= ChemicalMixerBehavior.RegisteredIngredients.Where(x => x.item == itemKey).FirstOrDefault();

            if (ingredient == null)
            {
                logger.LogDebug($"Unable to find registered ingredient for {item.name}, creating default ingredient");
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(itemKey, new ChemistryLiquidAppearance(color, 5f));
            }

            if (despawningIngredientItem) { localPlayer.DespawnHeldObject(); }

            InputIngredientRpc(ingredient);
        }

        public void OutputTrigger_Interact()
        {
            if (inputIngredient == null && outputIngredient == null) { return; }
            OutputTrigger_InteractRpc(localPlayer.actualClientId);
        }

        private void SetInputFlaskColor(ChemistryLiquidAppearance color)
        {
            //inputRenderer.enabled = true;
            logger.LogDebug("Setting input flash color to " + color.ToString());
            inputRenderer.material.color = color.liquidColor;
            inputRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            inputRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
            inputParticleSystemRenderer.material.color = color.liquidColor;
            inputParticleSystemRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            inputParticleSystemRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);

        }

        private void SetOutputFlaskColor(ChemistryLiquidAppearance color)
        {
            logger.LogDebug("Setting output flash color to " + color.ToString());
            outputRenderer.enabled = true;
            outputRenderer.sharedMaterial.color = color.liquidColor;
            outputRenderer.sharedMaterial.SetColor("_EmissiveColor", color.liquidColor);
            outputRenderer.sharedMaterial.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void InputIngredientRpc(ChemistryIngredient ingredient)
        {
            logger.LogDebug("InputIngredientRpc");

            inputIngredient = null;
            currentlyMixingRecipe = null;


            

            inputIngredient = ingredient;
            SetInputFlaskColor(ingredient.chemistryLiquidAppearance);

            currentlyMixingRecipe ??= RegisteredRecipes.Where(x => x.ingredient.Equals(ingredient)).FirstOrDefault();
            logger.LogDebug(currentlyMixingRecipe != null ? "Recipe found" : "Recipe not found");

            mixing = true;
            MixIngredients();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void InputIngredientRpc(ChemistryIngredient ingredient, ChemistryIngredient? outputIngredient, float mixTime)
        {
            logger.LogDebug("InputIngredientRpc");

            inputIngredient = ingredient;
            currentlyMixingRecipe = new DistilleryFixedOutputReaction(ingredient, outputIngredient, mixTime);
            SetInputFlaskColor(ingredient.chemistryLiquidAppearance);

            currentlyMixingRecipe ??= RegisteredRecipes.Where(x => x.ingredient.Equals(ingredient)).FirstOrDefault();
            logger.LogDebug(currentlyMixingRecipe != null ? "Recipe found" : "Recipe not found");

            mixing = true;
            MixIngredients();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void OutputTrigger_InteractRpc(ulong clientId)
        {
            if (outputIngredient == null || mixing) { return; }

            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            if (IsServer && !(player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is IChemistryOutputContainer container && container.ReceiveChemistryOutput(outputIngredient)))
            {
                GrabbableObject? outputItem = Utils.SpawnItem(outputIngredient.item, player.transform.position);
                if (outputItem != null)
                {
                    IEnumerator sendSpawnOutputIngredient(ChemistryIngredient outputIngredient)
                    {
                        yield return new WaitUntil(() => outputItem.NetworkObject != null && outputItem.NetworkObject.IsSpawned);
                        SpawnOutputIngredientRpc(clientId, outputItem.NetworkObject, outputIngredient);
                    }

                    StartCoroutine(sendSpawnOutputIngredient(outputIngredient));
                }
            }

            outputParticleSystem.Stop();
            outputRenderer.enabled = false;
            outputIngredient = null;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void SpawnOutputIngredientRpc(ulong clientId, NetworkObjectReference netRef, ChemistryIngredient outputIngredient)
        {
            logger.LogDebug("SpawnOutputIngredientRpc");
            if (!netRef.TryGet(out NetworkObject netObj)) { logger.LogError("Failed to get network object from network object reference"); return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { logger.LogError("Failed to get grabbable object from network object"); return; }

            if (item is IChemistryIngredient ingredient)
            {
                logger.LogDebug("Outputting IChemistryIngredient");
                ingredient.OnChemicalOutput(outputIngredient);
            }

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

            logger.LogDebug("Mixing ingredients coroutine");
            StartCoroutine(mixIngredients());
        }
    }
}
