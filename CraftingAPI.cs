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
            if (AnalyzerBehavior.RegisteredAnalyzableIngredients.Contains(ingredient)) { logger.LogError($"Could not register analyzable ingredient {ingredient}, a similar analyzable ingredient already exists"); return; }
            AnalyzerBehavior.RegisteredAnalyzableIngredients.Add(ingredient);
        }

        public static void RegisterRecipe(ChemistryRecipe recipe)
        {
            if (ChemicalMixerBehavior.RegisteredRecipes.Contains(recipe)) { logger.LogError($"Could not register chemistry recipe {recipe}, a similar recipe already exists"); return; }
            ChemicalMixerBehavior.RegisteredRecipes.Add(recipe);
        }

        public static void RegisterRecipe(DistilleryRecipe recipe)
        {
            if (AlembicBehavior.RegisteredRecipes.Contains(recipe)) { logger.LogError($"Could not register distillery recipe {recipe}, a similar recipe already exists"); return; }
            AlembicBehavior.RegisteredRecipes.Add(recipe);
        }

        public static void LogRecipies()
        {
            logger.LogDebug("Alembic recipes");
            foreach (var recipe in AlembicBehavior.RegisteredRecipes)
            {
                logger.LogDebug(recipe);
            }
            logger.LogDebug("ChemistryMixer recipes");
            foreach (var recipe2 in ChemicalMixerBehavior.RegisteredRecipes)
            {
                logger.LogDebug(recipe2);
            }
        }
    }
}
