using Dusk;
using SnowyLib;
using UnityEngine;

namespace SnowyCraftingCore
{
    internal class SnowyCraftingCoreContentHandler : ContentHandler<SnowyCraftingCoreContentHandler>
    {
        public class ChemicalAnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalAnalyzerAssets>(mod, filePath) { }
        public ChemicalAnalyzerAssets? ChemicalAnalyzer;

        public class ChemicalDistilleryAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalDistilleryAssets>(mod, filePath) { }
        public ChemicalDistilleryAssets? ChemicalDistillery;

        public class ChemicalMixerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalMixerAssets>(mod, filePath) { }
        public ChemicalMixerAssets? ChemicalMixer;

        public SnowyCraftingCoreContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("chemical_analyzer", out ChemicalAnalyzer);
            RegisterContent("chemical_distillery", out ChemicalDistillery);
            RegisterContent("chemical_mixer", out ChemicalMixer);
        }
    }
}