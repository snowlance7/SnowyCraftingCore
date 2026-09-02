using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class AnalyzableIngredient(Item item, Action<ChemistryIngredient> result, ChemistryLiquidAppearance? chemistryLiquidAppearance = null, string specialInstructions = "") : ChemistryIngredient(item, chemistryLiquidAppearance, specialInstructions), IEquatable<AnalyzableIngredient>
    {
        public Action<ChemistryIngredient> result = result;

        public bool Equals(AnalyzableIngredient other)
        {
            return item == other.item;
        }

        public override string ToString()
        {
            return item.name;
        }
    }
}
