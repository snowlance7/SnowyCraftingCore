using GameNetcodeStuff;
using System;

namespace SnowyCraftingCore;

public interface IAnalyzableIngredient : IChemistryIngredient
{
    public Action<AnalyzableIngredient, PlayerControllerB> OnAnalyze();
    public bool HoldItem();
}