using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class ChemistryFixedOutputReaction(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, ChemistryIngredient output, float mixTime = -1) : ChemistryRecipe(ingredientA, ingredientB, (ingredientA, ingredientB) => output, mixTime);
}
