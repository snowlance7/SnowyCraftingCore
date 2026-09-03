using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IAnalyzableIngredient : IChemistryIngredient
    {
        public Action<AnalyzableIngredient> OnAnalyze();
        public bool DespawnItemAfterAnalyzing();
    }
}
