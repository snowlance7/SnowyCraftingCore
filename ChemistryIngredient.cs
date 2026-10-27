using Dawn;
using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

public class ChemistryIngredient : IEquatable<ChemistryIngredient>, INetworkSerializable
{
    public static List<ChemistryIngredient> RegisteredChemistryIngredients { get; private set; } = [];

    public NamespacedKey<DawnItemInfo> item;
    public ChemistryLiquidAppearance chemistryLiquidAppearance;
    public string specialInstructions;

    public ChemistryIngredient()
    {
        item = new NamespacedKey<DawnItemInfo>();
        chemistryLiquidAppearance = new ChemistryLiquidAppearance();
        specialInstructions = "";
    }

    public ChemistryIngredient(NamespacedKey<DawnItemInfo> item, ChemistryLiquidAppearance? chemistryLiquidAppearance = default, string specialInstructions = "")
    {
        this.item = item;
        this.chemistryLiquidAppearance = chemistryLiquidAppearance ?? new ChemistryLiquidAppearance();
        this.specialInstructions = specialInstructions;
    }

    public ChemistryIngredient(NamespacedKey<DawnItemInfo> item, Color color, string specialInstructions = "")
    {
        this.item = item;
        this.chemistryLiquidAppearance = new ChemistryLiquidAppearance(color);
        this.specialInstructions = specialInstructions;
    }

    public bool Equals(ChemistryIngredient other)
    {
        return this.item.ToString() == other.item.ToString();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref item);
        serializer.SerializeValue(ref chemistryLiquidAppearance);
        serializer.SerializeValue(ref specialInstructions);
    }

    public override string ToString()
    {
        return item.Key;
    }

    public static void Register(ChemistryIngredient chemistryIngredient)
    {
        if (RegisteredChemistryIngredients.Contains(chemistryIngredient)) { logger.LogWarning($"Could not register chemistry ingredient {chemistryIngredient}, a similar chemistry ingredient already exists"); return; }
        RegisteredChemistryIngredients.Add(chemistryIngredient);
    }

    public static void Register(NamespacedKey<DawnItemInfo> item, ChemistryLiquidAppearance? chemistryLiquidAppearance = default, string specialInstructions = "")
    {
        Register(new ChemistryIngredient(item, chemistryLiquidAppearance, specialInstructions));
    }

    public static void Register(NamespacedKey<DawnItemInfo> item, Color color, string specialInstructions = "")
    {
        Register(new ChemistryIngredient(item, color, specialInstructions));
    }

    public static void SetFluidColors(ref Material[] materials, ChemistryLiquidAppearance chemistryLiquidAppearance)
    {
        foreach (var material in materials)
        {
            material.color = chemistryLiquidAppearance.liquidColor;
            if (material.IsKeywordEnabled("_EMISSION"))
            {
                material.SetColor("_EmissiveColor", chemistryLiquidAppearance.liquidColor);
                material.SetFloat("_EmissiveIntensity", chemistryLiquidAppearance.emissionIntensity);
            }
        }
    }

    public static void SetFluidColors(ref Renderer[] renderers, ChemistryLiquidAppearance chemistryLiquidAppearance)
    {
        foreach (var renderer in renderers)
        {
            renderer.material.color = chemistryLiquidAppearance.liquidColor;
            if (renderer.material.IsKeywordEnabled("_EMISSION"))
            {
                renderer.material.SetColor("_EmissiveColor", chemistryLiquidAppearance.liquidColor);
                renderer.material.SetFloat("_EmissiveIntensity", chemistryLiquidAppearance.emissionIntensity);
            }
        }
    }
}