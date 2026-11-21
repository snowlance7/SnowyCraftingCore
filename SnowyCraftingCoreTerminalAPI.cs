using SnowyLib;
using static SnowyCraftingCore.Plugin;

namespace SnowyCraftingCore;

internal static class SnowyCraftingCoreTerminalAPI
{
    public static int AnalyzeCost => Plugin.PluginInstance.Config.Bind("Terminal Commands", "Analyze Cost", 5, "How much it costs to request the item dropship for analysis").Value;

    [StaticInit]
    public static void Init()
    {
        TerminalAPI.RegisterTerminalCommand("danalyze", AnalyzeIngredient, "Other", $"DANALYZE  //  Price: ${AnalyzeCost}", $"Requests an item dropship to collect a sample to be analyzed. Cheap but takes a bit of time and you lose the item.");
    }

    public static TerminalResult AnalyzeIngredient(string[] args)
    {
        if (NetworkHandler.Instance.timeSinceRequestingAnalysis < 120) { return new TerminalResult($"Dropship Analysis request failed. Item dropship is in use or on cooldown. Try again in {120 - NetworkHandler.Instance.timeSinceRequestingAnalysis} seconds."); }
        int newGroupCredits = Utils.terminal!.groupCredits - AnalyzeCost;
        if (newGroupCredits < 0) { return new TerminalResult($"Dropship Analysis request failed. You could not afford this request!\nYour balance is {Utils.terminal.groupCredits}. Total cost of dropship analysis is {AnalyzeCost}."); }

        var callback1 = new TerminalCommand("confirm", (args) =>
        {
            NetworkHandler.Instance.RequestDropshipAnalysisRpc(localPlayer.actualClientId);
            Utils.SyncGroupCredits(newGroupCredits);
            return $"Dropship analysis confirmed. Dropship on route to location. Please input sample for analysis after ship has landed. Your new balance is {newGroupCredits}.";
        });
        var callback2 = new TerminalCommand("deny", (args) =>
        {
            return "Dropship analysis cancelled.";
        });

        return new TerminalResult($"You have requested the item dropship for item analysis.\nTotal cost of request is {AnalyzeCost}.\n\nPlease CONFIRM or DENY.");
    }
}