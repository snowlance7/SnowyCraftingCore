using Dawn;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

public class AnalyzableIngredient : ChemistryIngredient, IEquatable<AnalyzableIngredient>
{
    public static List<AnalyzableIngredient> RegisteredAnalyzableIngredients { get; private set; } = [];

    public Action<AnalyzableIngredient, PlayerControllerB> result;
    public bool holdItem;

    public AnalyzableIngredient()
    {
        item = new NamespacedKey<DawnItemInfo>();
        chemistryLiquidAppearance = new ChemistryLiquidAppearance();
        specialInstructions = "";
        result = (ingredient, player) => { };
        holdItem = true;
    }

    public AnalyzableIngredient(NamespacedKey<DawnItemInfo> item, Action<AnalyzableIngredient, PlayerControllerB> result, ChemistryLiquidAppearance? chemistryLiquidAppearance = null, string specialInstructions = "", bool holdItem = true)
    {
        this.item = item;
        this.chemistryLiquidAppearance = chemistryLiquidAppearance ?? new ChemistryLiquidAppearance();
        this.specialInstructions = specialInstructions;
        this.result = result;
        this.holdItem = holdItem;
    }

    public AnalyzableIngredient(NamespacedKey<DawnItemInfo> item, Action<AnalyzableIngredient, PlayerControllerB> result, Color color, string specialInstructions = "", bool holdItem = true)
    {
        this.item = item;
        this.chemistryLiquidAppearance = new ChemistryLiquidAppearance(color);
        this.specialInstructions = specialInstructions;
        this.result = result;
        this.holdItem = holdItem;
    }



    public bool Equals(AnalyzableIngredient other)
    {
        return item == other.item;
    }

    public override string ToString()
    {
        return item.ToString();
    }

    public static void Register(AnalyzableIngredient analyzableIngredient)
    {
        if (RegisteredAnalyzableIngredients.Contains(analyzableIngredient)) { logger.LogWarning($"Could not register analyzable ingredient {analyzableIngredient}, a similar analyzable ingredient already exists"); return; }
        RegisteredAnalyzableIngredients.Add(analyzableIngredient);
    }

    public static void Register(NamespacedKey<DawnItemInfo> item, Action<AnalyzableIngredient, PlayerControllerB> result, ChemistryLiquidAppearance? chemistryLiquidAppearance = null, string specialInstructions = "", bool holdItem = true)
    {
        Register(new AnalyzableIngredient(item, result, chemistryLiquidAppearance, specialInstructions, holdItem));
    }

    public static void Register(NamespacedKey<DawnItemInfo> item, Action<AnalyzableIngredient, PlayerControllerB> result, Color color, string specialInstructions = "", bool holdItem = true)
    {
        Register(item, result, color, specialInstructions, holdItem);
    }
}