namespace SnowyCraftingCore
{
    public interface IChemistryIngredient
    {
        public ChemistryIngredient? GetIngredient();
        public bool OnChemicalOutput(ChemistryIngredient ingredient);
    }
}