using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IChemistryOutputContainer
    {
        public bool ReceiveChemistryOutput(ChemistryIngredient ingredient);
    }
}
