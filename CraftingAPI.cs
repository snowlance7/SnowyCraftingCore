using SnowyCraftingCore.Unlockables;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore
{
    public static class CraftingAPI
    {
        public static void RegisterIngredient(ChemistryIngredient ingredient)
        {
            if (ChemicalMixerBehavior.RegisteredIngredients.Contains(ingredient)) { logger.LogError($"Could not register chemistry ingredient {ingredient}, a similar chemistry ingredient already exists"); return; }
            ChemicalMixerBehavior.RegisteredIngredients.Add(ingredient);
        }

        public static void RegisterIngredient(AnalyzableIngredient ingredient)
        {
            if (AnalyzerBehavior.RegisteredIngredients.Contains(ingredient)) { logger.LogError($"Could not register analyzable ingredient {ingredient}, a similar analyzable ingredient already exists"); return; }
            AnalyzerBehavior.RegisteredIngredients.Add(ingredient);
        }

        public static void RegisterRecipe(ChemistryRecipe recipe)
        {
            if (ChemicalMixerBehavior.RegisteredRecipies.Contains(recipe)) { logger.LogError($"Could not register chemistry recipe {recipe}, a similar recipe already exists"); return; }
            ChemicalMixerBehavior.RegisteredRecipies.Add(recipe);
        }

        public static void RegisterRecipe(DistilleryRecipe recipe)
        {
            if (AlembicBehavior.RegisteredRecipies.Contains(recipe)) { logger.LogError($"Could not register distillery recipe {recipe}, a similar recipe already exists"); return; }
            AlembicBehavior.RegisteredRecipies.Add(recipe);
        }
    }
}
