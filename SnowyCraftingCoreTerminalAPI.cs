using SnowyLib;

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
        if (Utils.terminal!.groupCredits - AnalyzeCost < 0) { return $"Analysis failed. You could not afford this request!\nYour balance is {Utils.terminal.groupCredits}. Total cost of analysis is {AnalyzeCost}."; }

        NetworkHandler.Instance.RequestDropshipAnalysisRpc();
        int newGroupCredits = Utils.terminal.groupCredits - AnalyzeCost;
        Utils.SyncGroupCredits(newGroupCredits);
        return $"Analysis dropship requested. Dropship on route to location. Please input sample for analysis. Your new balance is {Utils.terminal.groupCredits}";
    }
}