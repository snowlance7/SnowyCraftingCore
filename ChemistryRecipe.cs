using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

public class ChemistryRecipe(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction, float mixTime = -1) : IEquatable<ChemistryRecipe>
{
    public static List<ChemistryRecipe> RegisteredChemistryRecipes { get; internal set; } = [];

    public ChemistryIngredient ingredientA = ingredientA;
    public ChemistryIngredient ingredientB = ingredientB;

    public Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction = reaction;
    public float mixTime = mixTime;

    public bool Equals(ChemistryRecipe other)
    {
        return (ingredientA == other.ingredientA && ingredientB == other.ingredientB) || (ingredientA == other.ingredientB && ingredientB == other.ingredientA);
    }

    public override string ToString()
    {
        return ingredientA + " + " + ingredientB + " = ???";
    }

    public static void Register(ChemistryRecipe chemistryRecipe)
    {
        if (RegisteredChemistryRecipes.Contains(chemistryRecipe)) { logger.LogWarning($"Could not register chemistry recipe {chemistryRecipe}, a similar recipe already exists"); return; }
        RegisteredChemistryRecipes.Add(chemistryRecipe);
    }

    public static void Register(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction, float mixTime = -1)
    {
        Register(new ChemistryRecipe(ingredientA, ingredientB, reaction, mixTime));
    }
}