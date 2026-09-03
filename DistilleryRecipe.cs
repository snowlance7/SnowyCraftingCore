using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class DistilleryRecipe(ChemistryIngredient ingredient, Func<ChemistryIngredient, ChemistryIngredient?> reaction, float mixTime = -1)
    {
        public ChemistryIngredient ingredient = ingredient;

        public Func<ChemistryIngredient, ChemistryIngredient?> reaction = reaction;
        public float mixTime = mixTime;

        public override string ToString()
        {
            return ingredient + " = ???";
        }
    }
}
