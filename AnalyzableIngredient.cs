using Dawn;
using GameNetcodeStuff;
using System;

namespace SnowyCraftingCore
{
    public class AnalyzableIngredient(NamespacedKey<DawnItemInfo> item, Action<AnalyzableIngredient, PlayerControllerB> result, ChemistryLiquidAppearance? chemistryLiquidAppearance = null, string specialInstructions = "", bool despawnItem = false, bool holdItem = true) : ChemistryIngredient(item, chemistryLiquidAppearance, specialInstructions), IEquatable<AnalyzableIngredient>
    {
        public Action<AnalyzableIngredient, PlayerControllerB> result = result;
        public bool despawnItem = despawnItem;
        public bool holdItem = holdItem;

        public bool Equals(AnalyzableIngredient other)
        {
            return item == other.item;
        }

        public override string ToString()
        {
            return item.ToString();
        }
    }
}
