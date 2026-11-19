using SnowyLib;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

internal static class SnowyCraftingCoreTerminalAPI
{
    public static int AnalyzeCost => Plugin.PluginInstance.Config.Bind("Terminal Commands", "Analyze Cost", 10, "How much it costs to request the item dropship for analysis").Value;

    [StaticInit]
    public static void Init()
    {
        TerminalAPI.RegisterTerminalCommand("danalyze", AnalyzeIngredient, "Other", $"DANALYZE  //  Price: ${AnalyzeCost}", $"Requests an item dropship to collect a sample to be analyzed. Cheap but takes a bit of time.");
    }

    public static string AnalyzeIngredient(string[] args)
    {
        if (NetworkHandler.Instance.timeSinceRequestingAnalysis < 120) { return $"Dropship Analysis request failed. Item dropship is in use or on cooldown. Try again in {120 - NetworkHandler.Instance.timeSinceRequestingAnalysis} seconds."; }
        if (Utils.terminal!.groupCredits - AnalyzeCost < 0) { return $"Dropship Analysis request failed. You could not afford this request!\nYour balance is {Utils.terminal.groupCredits}. Total cost of dropship analysis is {AnalyzeCost}."; }

        NetworkHandler.Instance.RequestDropshipAnalysisRpc(localPlayer.actualClientId);
        int newGroupCredits = Utils.terminal.groupCredits - AnalyzeCost;
        Utils.SyncGroupCredits(newGroupCredits);
        return $"Analysis dropship requested. Dropship on route to location. Please input sample for analysis after ship has landed. Your new balance is {Utils.terminal.groupCredits}.";
    }
}