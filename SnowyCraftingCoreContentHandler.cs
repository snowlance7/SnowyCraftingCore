using Dusk;
using SnowyLib;
using UnityEngine;

namespace SnowyCraftingCore
{
    internal class SnowyCraftingCoreContentHandler : ContentHandler<SnowyCraftingCoreContentHandler>
    {
        public class SnowyCraftingCoreAssetsAssets(DuskMod mod, string filePath) : AssetBundleLoader<SnowyCraftingCoreAssetsAssets>(mod, filePath)
        {
            [LoadFromBundle("SmallItemDispenser.prefab")]
            public GameObject SmallItemDispenserPrefab { get; private set; } = null!;

            [LoadFromBundle("ApparatusSlot.prefab")]
            public GameObject ApparatusSlotPrefab { get; private set; } = null!;
        }
        public SnowyCraftingCoreAssetsAssets? SnowyCraftingCoreAssets;

        public class AnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<AnalyzerAssets>(mod, filePath) { }
        public AnalyzerAssets? Analyzer;

        public class AlembicAssets(DuskMod mod, string filePath) : AssetBundleLoader<AlembicAssets>(mod, filePath) { }
        public AlembicAssets? Alembic;

        public class ChemicalMixerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalMixerAssets>(mod, filePath) { }
        public ChemicalMixerAssets? ChemicalMixer;

        public SnowyCraftingCoreContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("snowycraftingcore_assets", out SnowyCraftingCoreAssets);
            RegisterContent("analyzer", out Analyzer);
            RegisterContent("alembic", out Alembic);
            RegisterContent("chemical_mixer", out ChemicalMixer);
        }
    }
}