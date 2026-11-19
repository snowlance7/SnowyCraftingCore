using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

internal class NetworkHandler : NetworkBehaviour
{
    public static NetworkHandler Instance { get; private set; } = null!;

    private static AnalyzableIngredient? analyzingIngredient;
    private static ulong clientRequestingAnalysisId;
    public float timeSinceRequestingAnalysis { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsServer) { Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true); }
        Instance = this;
        logger?.LogDebug("NetworkHandler spawned");
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance != null && Instance == this)
        {
            Instance = null!;
        }
        base.OnNetworkDespawn();
    }

    public void Update()
    {
        timeSinceRequestingAnalysis += Time.deltaTime;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RequestDropshipAnalysisRpc(ulong clientRequestingId)
    {
        timeSinceRequestingAnalysis = 0f;
        clientRequestingAnalysisId = clientRequestingId;

        if (!IsServer) { return; }


        ItemDropshipOperations.RequestOperation(new OperationCondition((player) =>
        {
            NamespacedKey<DawnItemInfo> itemKey = player.currentlyHeldObjectServer.itemProperties.GetDawnInfo().TypedKey;
            AnalyzableIngredient? ingredient = null;

            if (player.currentlyHeldObjectServer is IAnalyzableIngredient _ingredient)
            {
                ChemistryIngredient? chemistryIngredient = _ingredient.GetIngredient();
                chemistryIngredient ??= new ChemistryIngredient(itemKey);
                ingredient = new AnalyzableIngredient(itemKey, _ingredient.OnAnalyze(), chemistryIngredient.chemistryLiquidAppearance, chemistryIngredient.specialInstructions);
            }

            ingredient ??= AnalyzableIngredient.RegisteredAnalyzableIngredients.FirstOrDefault(x => x.item == itemKey);

            if (ingredient == null) { return false; }
            analyzingIngredient = ingredient;
            SetDropshipAnalysisIngredientRpc(player.actualClientId);
            return true;
        }, "Item to be analyzed"), (result) =>
        {
            if (result)
            {
                InvokeAnalyzingIngredientRpc();
            }
            else
            {
                NotifyPlayerAnalysisFailedRpc();
            }
        }, 10f);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    private void SetDropshipAnalysisIngredientRpc(ulong clientHoldingIngredientId)
    {
        PlayerControllerB player = PlayerFromId(clientHoldingIngredientId);
        NamespacedKey<DawnItemInfo> itemKey = player.currentlyHeldObjectServer.itemProperties.GetDawnInfo().TypedKey;
        AnalyzableIngredient? ingredient = null;

        if (player.currentlyHeldObjectServer is IAnalyzableIngredient _ingredient)
        {
            ChemistryIngredient? chemistryIngredient = _ingredient.GetIngredient();
            chemistryIngredient ??= new ChemistryIngredient(itemKey);
            ingredient = new AnalyzableIngredient(itemKey, _ingredient.OnAnalyze(), chemistryIngredient.chemistryLiquidAppearance, chemistryIngredient.specialInstructions, holdItem: _ingredient.HoldItem());
        }

        ingredient ??= AnalyzableIngredient.RegisteredAnalyzableIngredients.FirstOrDefault(x => x.item == itemKey);

        if (ingredient == null) { logger.LogError("SetDropshipAnalysisIngredient: Failed to get AnalyzableIngredient from item"); return; }
        analyzingIngredient = ingredient;

        if (localPlayer.actualClientId == clientHoldingIngredientId)
        {
            localPlayer.DespawnHeldObject();
        }
        if (localPlayer.actualClientId == clientHoldingIngredientId || localPlayer.actualClientId == clientRequestingAnalysisId)
        {
            HUDManager.Instance.DisplayTip("Item accepted", "Item ship on route to company for analysis");
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void InvokeAnalyzingIngredientRpc()
    {
        analyzingIngredient?.result.Invoke(analyzingIngredient, PlayerFromId(clientRequestingAnalysisId));
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void NotifyPlayerAnalysisFailedRpc()
    {
        if (localPlayer.actualClientId != clientRequestingAnalysisId) { return; }
        HUDManager.Instance.DisplayTip("Analysis failed", "No valid item was inserted into the item dropship", isWarning: true);
    }
}

[HarmonyPatch]
internal class NetworkHandlerPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    static void StartOfRound_Awake_PostFix()
    {
        if (!IsServerOrHost) { return; }
        var networkHandlerHost = UnityEngine.Object.Instantiate(SnowyCraftingCoreContentHandler.Instance.SnowyCraftingCoreAssets!.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
        networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
    }
}