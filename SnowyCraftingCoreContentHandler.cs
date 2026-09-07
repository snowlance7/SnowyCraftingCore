using Dusk;
using SnowyLib;
using UnityEngine;

namespace SnowyCraftingCore
{
    internal class SnowyCraftingCoreContentHandler : ContentHandler<SnowyCraftingCoreContentHandler>
    {
        public class SmallItemDispenserAssets(DuskMod mod, string filePath) : AssetBundleLoader<SmallItemDispenserAssets>(mod, filePath)
        {
            [LoadFromBundle("SmallItemDispenser.prefab")]
            public GameObject SmallItemDispenserPrefab { get; private set; } = null!;
        }
        public SmallItemDispenserAssets? SmallItemDispenser;

        public class ApparatusPowerPortAssets(DuskMod mod, string filePath) : AssetBundleLoader<ApparatusPowerPortAssets>(mod, filePath)
        {

            [LoadFromBundle("ApparatusPowerPort.prefab")]
            public GameObject ApparatusPowerPortPrefab { get; private set; } = null!;
        }
        public ApparatusPowerPortAssets? ApparatusPowerPort;

        public class AnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<AnalyzerAssets>(mod, filePath) { }
        public AnalyzerAssets? Analyzer;

        public class AlembicAssets(DuskMod mod, string filePath) : AssetBundleLoader<AlembicAssets>(mod, filePath) { }
        public AlembicAssets? Alembic;

        public class ChemicalMixerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalMixerAssets>(mod, filePath) { }
        public ChemicalMixerAssets? ChemicalMixer;

        public SnowyCraftingCoreContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("apparatus_power_port", out ApparatusPowerPort);
            RegisterContent("small_item_dispenser", out SmallItemDispenser);
            RegisterContent("analyzer", out Analyzer);
            RegisterContent("alembic", out Alembic);
            RegisterContent("chemical_mixer", out ChemicalMixer);
        }
    }
}