using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SnowyCraftingCore
{
    public class ChemistryLiquidAppearance(Color liquidColor = default, float emissionIntensity = 0f)
    {
        public Color liquidColor = liquidColor;
        public float emissionIntensity = emissionIntensity;
    }
}
