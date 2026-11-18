using HarmonyLib;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

internal class NetworkHandler : NetworkBehaviour
{
    public static NetworkHandler Instance { get; private set; } = null!;

    public enum SoundEffect
    {
        Sneeze,
        Cough,
        CoughHeavy,
        Paranoia,
        Duck,
        Sponge
    }

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

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RequestDropshipAnalysisRpc()
    {
        if (!IsServer) { return; }
        ItemDropshipOperations.RequestOperation(new ItemDropshipOperations.OperationCondition()) // TODO
    }
}

[HarmonyPatch]
internal class NetworkHandlerPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    static void StartOfRound_Awake_PostFix()
    {
        if (!IsServerOrHost) { return; }
        var networkHandlerHost = UnityEngine.Object.Instantiate(SnowyCraftingCoreContentHandler.Instance.SnowyCraftingCoreAssets?.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
        networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
    }
}