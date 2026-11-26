namespace SnowyCraftingCore
{
    public interface IDistillableIngredient : IChemistryIngredient
    {
        public ChemistryIngredient? DistilleryOutput();
        public float DistilleryMixTime();
        public bool DespawnItemAfterDistilleryInput();
    }
}
