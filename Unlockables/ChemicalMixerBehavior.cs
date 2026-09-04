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
    internal class ChemicalMixerBehavior : NetworkBehaviour
    {
        public static List<ChemistryIngredient> RegisteredIngredients { get; internal set; } = [];
        public static List<ChemistryRecipe> RegisteredRecipes { get; internal set; } = [];

        [SerializeField] InteractTrigger input1Trigger = null!;
        [SerializeField] InteractTrigger input2Trigger = null!;
        [SerializeField] InteractTrigger outputTrigger = null!;

        [SerializeField] Collider input1TriggerCollider = null!;
        [SerializeField] Collider input2TriggerCollider = null!;
        [SerializeField] Collider outputTriggerCollider = null!;

        [SerializeField] MeshRenderer input1Renderer = null!;
        [SerializeField] MeshRenderer input2Renderer = null!;
        [SerializeField] MeshRenderer outputRenderer = null!;

        [SerializeField] ParticleSystem input1ParticleSystem = null!;
        [SerializeField] ParticleSystem input2ParticleSystem = null!;
        [SerializeField] ParticleSystem outputParticleSystem = null!;

        [SerializeField] AudioSource audioSource = null!;
        [SerializeField] Transform explosionPosition = null!;

        [SerializeField] Sprite mixIcon = null!;
        [SerializeField] Sprite handIcon = null!;

        ChemistryIngredient? input1Ingredient;
        ChemistryIngredient? input2Ingredient;
        ChemistryIngredient? outputIngredient;

        ChemistryRecipe? currentlyMixingRecipe;

        bool mixing;

        const float defaultMixingTime = 10f;

        public void Awake()
        {
            input1TriggerCollider = input1Trigger.GetComponent<Collider>();
            input2TriggerCollider = input2Trigger.GetComponent<Collider>();
            outputTriggerCollider = outputTrigger.GetComponent<Collider>();
        }

        public void Update()
        {
            input1TriggerCollider.enabled = input1Ingredient == null && localPlayer.currentlyHeldObjectServer != null;

            input2TriggerCollider.enabled = input2Ingredient == null && localPlayer.currentlyHeldObjectServer != null;

            outputTriggerCollider.enabled = (input1Ingredient != null && input2Ingredient != null) || outputIngredient != null;

            if (input1Ingredient != null && input2Ingredient != null && outputIngredient == null)
            {
                outputTrigger.hoverTip = "Mix [E]";
                outputTrigger.hoverIcon = mixIcon;
            }
            else if (outputIngredient != null)
            {
                outputTrigger.hoverTip = "Take Ingredient [E]";
                outputTrigger.hoverIcon = handIcon;
            }
        }

        public void Input1Trigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            logger.LogDebug("Input1Trigger_Interact");
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject, 1);
        }

        public void Input2Trigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            logger.LogDebug("Input2Trigger_Interact");
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject, 2);
        }

        public void OutputTrigger_Interact()
        {
            if ((input1Ingredient == null || input2Ingredient == null) && outputIngredient == null) { return; }
            OutputTrigger_InteractRpc(localPlayer.actualClientId);
        }

        private void SetInput1FlaskColor(ChemistryLiquidAppearance color)
        {
            input1Renderer.enabled = true;
            input1Renderer.sharedMaterial.color = color.liquidColor;
            input1Renderer.sharedMaterial.SetColor("_EmissionColor", color.liquidColor);
            input1Renderer.sharedMaterial.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        private void SetInput2FlaskColor(ChemistryLiquidAppearance color)
        {
            input2Renderer.enabled = true;
            input2Renderer.sharedMaterial.color = color.liquidColor;
            input2Renderer.sharedMaterial.SetColor("_EmissionColor", color.liquidColor);
            input2Renderer.sharedMaterial.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        private void SetOutputFlaskColor(ChemistryLiquidAppearance color)
        {
            outputRenderer.enabled = true;
            outputRenderer.sharedMaterial.color = color.liquidColor;
            outputRenderer.sharedMaterial.SetColor("_EmissionColor", color.liquidColor);
            outputRenderer.sharedMaterial.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone)]
        private void InputIngredientRpc(NetworkObjectReference netRef, int flaskInputIndex)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = true;

            if (item is IChemistryIngredient _ingredient)
            {
                ingredient = _ingredient.GetIngredient();

                if (_ingredient is IMixableIngredient _mixableIngredient)
                    despawningIngredientItem = _mixableIngredient.DespawnItemAfterInput();
            }

            ingredient ??= RegisteredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, 0));
            }

            if (flaskInputIndex == 1)
            {
                input1Ingredient = ingredient;
                SetInput1FlaskColor(ingredient.chemistryLiquidAppearance);
            }
            else if (flaskInputIndex == 2)
            {
                input2Ingredient = ingredient;
                SetInput2FlaskColor(ingredient.chemistryLiquidAppearance);
            }

            if (localPlayer == item.playerHeldBy && despawningIngredientItem)
                localPlayer.DespawnHeldObject();
        }

        [Rpc(SendTo.Everyone)]
        private void OutputTrigger_InteractRpc(ulong clientId)
        {
            if (((input1Ingredient == null || input2Ingredient == null) && outputIngredient == null) || mixing) { return; }

            if (input1Ingredient != null && input2Ingredient != null && outputIngredient == null) // Mixing
            {
                currentlyMixingRecipe = RegisteredRecipes.Where(x => (x.ingredientA.Equals(input1Ingredient) && x.ingredientB.Equals(input2Ingredient)) || (x.ingredientA.Equals(input2Ingredient) && x.ingredientB.Equals(input1Ingredient))).FirstOrDefault();

                mixing = true;
                MixIngredients();
            }
            else if (outputIngredient != null) // Taking ingredient
            {
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

                input1ParticleSystem.Play();
                input2ParticleSystem.Play();
                audioSource.Play();

                float mixTime = currentlyMixingRecipe != null && currentlyMixingRecipe.mixTime > 0 ? currentlyMixingRecipe.mixTime : defaultMixingTime;

                yield return new WaitForSeconds(mixTime);

                if (currentlyMixingRecipe != null)
                {
                    outputIngredient = currentlyMixingRecipe.reaction.Invoke(input1Ingredient!, input2Ingredient!);
                    SetOutputFlaskColor(outputIngredient.chemistryLiquidAppearance);
                    outputParticleSystem.Play();
                }
                else
                {
                    Landmine.SpawnExplosion(explosionPosition.position, true, killRange: 0, nonLethalDamage: 5, physicsForce: 5f);
                }

                currentlyMixingRecipe = null;
                input1ParticleSystem.Stop();
                input2ParticleSystem.Stop();
                input1Ingredient = null;
                input2Ingredient = null;
                input1Renderer.enabled = false;
                input2Renderer.enabled = false;
                mixing = false;
            }

            StartCoroutine(mixIngredients());
        }
    }
}
