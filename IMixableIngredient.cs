using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IMixableIngredient : IChemistryIngredient
    {
        public bool DespawnItemAfterInput();
    }
}
