using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IAnalyzableIngredient : IChemistryIngredient
    {
        public Action<AnalyzableIngredient, PlayerControllerB> OnAnalyze();
        public bool HoldItem();
    }
}
