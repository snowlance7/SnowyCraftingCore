namespace SnowyCraftingCore
{
    public class ChemistryFixedOutputReaction(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, ChemistryIngredient output, float mixTime = -1) : ChemistryRecipe(ingredientA, ingredientB, (ingredientA, ingredientB) => output, mixTime);
}
