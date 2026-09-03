using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class AnalyzableIngredient(Item item, Action<AnalyzableIngredient> result, ChemistryLiquidAppearance? chemistryLiquidAppearance = null, string specialInstructions = "") : ChemistryIngredient(item, chemistryLiquidAppearance, specialInstructions), IEquatable<AnalyzableIngredient>
    {
        public Action<AnalyzableIngredient> result = result;

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
