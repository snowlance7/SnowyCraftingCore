using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IAnalyzableIngredient
    {
        public AnalyzableIngredient GetAnalyzableIngredient();
        public bool DespawnItemAfterAnalyzing();
    }
}
