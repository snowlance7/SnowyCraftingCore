using SnowyCraftingCore.Unlockables;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore
{
    public static class CraftingAPI
    {
        public static void RegisterIngredient(ChemistryIngredient ingredient)
        {
            if (ChemicalMixerBehavior.registeredIngredients.Contains(ingredient)) { logger.LogError($"Could not register chemistry ingredient {ingredient}, a similar chemistry ingredient already exists"); return; }
            ChemicalMixerBehavior.registeredIngredients.Add(ingredient);
        }

        public static void RegisterIngredient(AnalyzableIngredient ingredient)
        {
            if (ChemicalAnalyzerBehavior.registeredIngredients.Contains(ingredient)) { logger.LogError($"Could not register analyzable ingredient {ingredient}, a similar analyzable ingredient already exists"); return; }
            ChemicalAnalyzerBehavior.registeredIngredients.Add(ingredient);
        }

        public static void RegisterRecipe(ChemistryRecipe recipe)
        {
            if (ChemicalMixerBehavior.registeredRecipies.Contains(recipe)) { logger.LogError($"Could not register chemistry recipe {recipe}, a similar recipe already exists"); return; }
            ChemicalMixerBehavior.registeredRecipies.Add(recipe);
        }

        public static void RegisterRecipe(DistilleryRecipe recipe)
        {
            if (ChemicalDistilleryBehavior.registeredRecipies.Contains(recipe)) { logger.LogError($"Could not register distillery recipe {recipe}, a similar recipe already exists"); return; }
            ChemicalDistilleryBehavior.registeredRecipies.Add(recipe);
        }
    }
}
