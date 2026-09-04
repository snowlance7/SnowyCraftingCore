namespace SnowyCraftingCore
{
    public interface IChemistryIngredient
    {
        public ChemistryIngredient? GetIngredient();
        public void OnChemicalOutput(string specialInstructions);
    }
}