namespace SnowyCraftingCore
{
    public interface IChemistryIngredient
    {
        public ChemistryIngredient? GetIngredient();
        public void OnChemicalMixerOutput(string specialInstructions);
    }
}