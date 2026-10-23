using System;
using System.Collections.Generic;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

public class DistilleryRecipe(ChemistryIngredient ingredient, Func<ChemistryIngredient, ChemistryIngredient?> reaction, float mixTime = -1)
{
    public static List<DistilleryRecipe> RegisteredDistilleryRecipes { get; private set; } = [];

    public ChemistryIngredient ingredient = ingredient;

    public Func<ChemistryIngredient, ChemistryIngredient?> reaction = reaction;
    public float mixTime = mixTime;

    public override string ToString()
    {
        return ingredient + " = ???";
    }

    public static void Register(DistilleryRecipe distilleryRecipe)
    {
        if (RegisteredDistilleryRecipes.Contains(distilleryRecipe)) { logger.LogWarning($"Could not register distillery recipe {distilleryRecipe}, a similar recipe already exists"); return; }
        RegisteredDistilleryRecipes.Add(distilleryRecipe);
    }

    public static void Register(ChemistryIngredient ingredient, Func<ChemistryIngredient, ChemistryIngredient?> reaction, float mixTime = -1)
    {
        Register(new DistilleryRecipe(ingredient, reaction, mixTime));
    }
}