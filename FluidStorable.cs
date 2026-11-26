using Dawn;
using Dawn.Interfaces;
using Newtonsoft.Json.Linq;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

public abstract class FluidStorable : PhysicsProp, IDawnSaveData, IChemistryIngredient // UPDATE/TODO: Expand this so it can store an amount of liquid
{
    public FluidMaterial[] fluidMaterials = [];
    public ChemistryLiquidAppearance defaultFluidColor = new ChemistryLiquidAppearance(Color.clear);
    
    [HideInInspector] public string StoredFluid { get; private set; } = "";

    [HideInInspector] public bool IsFilled { get; private set; } = false;

    [HideInInspector] public ChemistryLiquidAppearance FluidColor { get; private set; } = new ChemistryLiquidAppearance(Color.clear);

    public virtual void SetFluidColor(ChemistryLiquidAppearance color)
    {
        logger.LogDebug("Setting fluid color to " + color.ToString());

        foreach (var fluidMaterial in fluidMaterials)
        {
            fluidMaterial.Material.color = color.liquidColor;

            if (fluidMaterial.Material.HasProperty("_EmissiveColor"))
            {
                fluidMaterial.Material.SetColor("_EmissiveColor", color.liquidColor);
            }
            if (fluidMaterial.Material.HasProperty("_EmissiveIntensity"))
            {
                fluidMaterial.Material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
            }

            if (fluidMaterial.disableRendererWhenEmpty)
            {
                fluidMaterial.renderer.enabled = true;
            }
        }

        FluidColor = color;
    }

    public virtual void SetFluidColor(Color color)
    {
        logger.LogDebug("Setting fluid color to " + color);

        foreach (var fluidMaterial in fluidMaterials)
        {
            fluidMaterial.Material.color = color;

            if (fluidMaterial.Material.HasProperty("_EmissiveColor"))
            {
                fluidMaterial.Material.SetColor("_EmissiveColor", color);
            }

            if (fluidMaterial.disableRendererWhenEmpty)
            {
                fluidMaterial.renderer.enabled = true;
            }
        }

        FluidColor = new ChemistryLiquidAppearance(color);
    }

    public virtual void SetFluidColorToDefault()
    {
        foreach (var fluidMaterial in fluidMaterials)
        {
            fluidMaterial.Material.color = defaultFluidColor.liquidColor;

            if (fluidMaterial.Material.HasProperty("_EmissiveColor"))
            {
                fluidMaterial.Material.SetColor("_EmissiveColor", defaultFluidColor.liquidColor);
            }
            if (fluidMaterial.Material.HasProperty("_EmissiveIntensity"))
            {
                fluidMaterial.Material.SetFloat("_EmissiveIntensity", defaultFluidColor.emissionIntensity);
            }

            if (fluidMaterial.disableRendererWhenEmpty)
            {
                fluidMaterial.renderer.enabled = false;
            }
        }

        FluidColor = defaultFluidColor;
    }

    public virtual void Fill(string fluidString, ChemistryLiquidAppearance color, bool doAnimation = false)
    {
        IsFilled = true;
        StoredFluid = fluidString;
        SetFluidColor(color);

        if (doAnimation)
        {
            gameObject.GetComponent<Animator>()?.SetTrigger("fill");
        }
        else
        {
            gameObject.GetComponent<Animator>()?.SetTrigger("filled");
        }
    }

    public virtual void Empty(bool doAnimation = false, float animationTime = 1f)
    {
        IsFilled = false;
        StoredFluid = "";

        if (doAnimation)
        {
            gameObject.GetComponent<Animator>()?.SetTrigger("empty");
            Utils.InvokeAfterDelay(() => SetFluidColor(Color.clear), animationTime);
        }
        else
        {
            gameObject.GetComponent<Animator>()?.SetTrigger("emptied");
            SetFluidColorToDefault();
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void FillRpc(string fluidString, ChemistryLiquidAppearance color, bool doAnimation = false)
    {
        Fill(fluidString, color, doAnimation);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void EmptyRpc(bool doAnimation = false)
    {
        Empty(doAnimation);
    }

    public virtual JToken GetDawnDataToSave()
    {
        if (!IsFilled) { return null!; }
        return JToken.FromObject(GetIngredient()!);
    }

    public virtual void LoadDawnSaveData(JToken saveData)
    {
        ChemistryIngredient? ingredient = saveData.Value<ChemistryIngredient>();
        if (ingredient == null) { return; }
        Fill(ingredient.specialInstructions, ingredient.chemistryLiquidAppearance);
    }

    public virtual bool OnChemicalOutput(ChemistryIngredient ingredient)
    {
        if (IsFilled || ingredient == null) { return false; }
        Fill(ingredient.specialInstructions, ingredient.chemistryLiquidAppearance);
        return true;
    }

    public virtual ChemistryIngredient? GetIngredient()
    {
        if (!IsFilled) { return null; }
        return new ChemistryIngredient(itemProperties.GetDawnInfo().TypedKey, FluidColor, StoredFluid);
    }
}