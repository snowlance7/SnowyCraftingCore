using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public interface IDistillableIngredient
    {
        public ChemistryIngredient? GetDistilleryOutput();
        public float GetDistilleryMixTime();
    }
}
