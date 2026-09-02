using SnowyCraftingCore.Unlockables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class ChemistryIngredient(Item item, ChemistryLiquidAppearance? chemistryLiquidAppearance = default, string specialInstructions = "") : IEquatable<ChemistryIngredient>
    {
        public Item item = item;
        public ChemistryLiquidAppearance chemistryLiquidAppearance = chemistryLiquidAppearance ?? new ChemistryLiquidAppearance();
        public string specialInstructions = specialInstructions;

        public bool Equals(ChemistryIngredient other)
        {
            return item == other.item;
        }

        public override string ToString()
        {
            return item.name;
        }
    }
}
