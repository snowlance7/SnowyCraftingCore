using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowyCraftingCore;

public struct ChemistryLiquidAppearance : INetworkSerializable
{
    public Color liquidColor;
    public float emissionIntensity;

    public ChemistryLiquidAppearance()
    {
        liquidColor = new Color();
        emissionIntensity = 0f;
    }

    public ChemistryLiquidAppearance(Color liquidColor = default, float emissionIntensity = 0f)
    {
        this.liquidColor = liquidColor;
        this.emissionIntensity = emissionIntensity;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref liquidColor);
        serializer.SerializeValue(ref emissionIntensity);
    }

    public override string ToString()
    {
        return $"Color: {liquidColor.ToString()}, EmissionIntensity: {emissionIntensity}";
    }

    public static void SetFluidColor(ref Material material, ChemistryLiquidAppearance chemistryLiquidAppearance)
    {
        material.color = chemistryLiquidAppearance.liquidColor;
        if (material.IsKeywordEnabled("_EMISSION"))
        {
            material.SetColor("_EmissiveColor", chemistryLiquidAppearance.liquidColor);
            material.SetFloat("_EmissiveIntensity", chemistryLiquidAppearance.emissionIntensity);
        }
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

    public static void SetFluidColors(ref Renderer renderer, ChemistryLiquidAppearance chemistryLiquidAppearance)
    {
        renderer.material.color = chemistryLiquidAppearance.liquidColor;
        if (renderer.material.IsKeywordEnabled("_EMISSION"))
        {
            renderer.material.SetColor("_EmissiveColor", chemistryLiquidAppearance.liquidColor);
            renderer.material.SetFloat("_EmissiveIntensity", chemistryLiquidAppearance.emissionIntensity);
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