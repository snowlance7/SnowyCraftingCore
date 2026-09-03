using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IDistillableIngredient : IChemistryIngredient
    {
        public ChemistryIngredient? DistilleryOutput();
        public float DistilleryMixTime();
        public bool DespawnItemAfterDistilleryInput();
    }
}
