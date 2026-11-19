using Dusk;
using UnityEngine;

namespace SnowyCraftingCore
{
    internal class SnowyCraftingCoreContentHandler : ContentHandler<SnowyCraftingCoreContentHandler>
    {
        public class AnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<AnalyzerAssets>(mod, filePath) { }
        public AnalyzerAssets? Analyzer;

        public class AlembicAssets(DuskMod mod, string filePath) : AssetBundleLoader<AlembicAssets>(mod, filePath) { }
        public AlembicAssets? Alembic;

        public class ChemicalMixerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ChemicalMixerAssets>(mod, filePath) { }
        public ChemicalMixerAssets? ChemicalMixer;

        public class SnowyCraftingCoreAssetsAssets(DuskMod mod, string filePath) : AssetBundleLoader<SnowyCraftingCoreAssetsAssets>(mod, filePath)
        {
            [LoadFromBundle("NetworkHandler.prefab")]
            public GameObject NetworkHandlerPrefab { get; private set; } = null!;

            [LoadFromBundle("ApparatusPowerPort.prefab")]
            public GameObject ApparatusPowerPortPrefab { get; private set; } = null!;
        }
        public SnowyCraftingCoreAssetsAssets? SnowyCraftingCoreAssets;

        public SnowyCraftingCoreContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("snowy_crafting_core_assets", out SnowyCraftingCoreAssets);
            RegisterContent("analyzer", out Analyzer);
            RegisterContent("alembic", out Alembic);
            RegisterContent("chemical_mixer", out ChemicalMixer);
        }
    }
}