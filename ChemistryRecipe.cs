using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class ChemistryRecipe(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction, float mixTime = -1) : IEquatable<ChemistryRecipe>
    {
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
            return ingredientA + "|" + ingredientB;
        }
    }
}
