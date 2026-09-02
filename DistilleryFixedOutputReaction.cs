using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyCraftingCore
{
    public class DistilleryFixedOutputReaction(ChemistryIngredient ingredient, ChemistryIngredient output, float mixTime = -1) : DistilleryRecipe(ingredient, (ingredient) => output, mixTime);
}
