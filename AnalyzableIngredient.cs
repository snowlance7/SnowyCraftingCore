using Dawn;
using GameNetcodeStuff;
using System;

namespace SnowyCraftingCore
{
    public class AnalyzableIngredient : ChemistryIngredient, IEquatable<AnalyzableIngredient>
    {
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
