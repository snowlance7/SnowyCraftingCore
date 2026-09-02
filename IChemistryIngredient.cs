using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IChemistryIngredient
    {
        public ChemistryIngredient GetInputIngredient();
        public bool DespawnItemAfterInput();
        public void OnOutputIngredient(string specialInstructions);
    }
}
