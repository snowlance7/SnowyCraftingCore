using Dawn;

namespace SnowyCraftingCore
{
    internal static class SnowyCraftingCoreKeys
    {
        public static readonly NamespacedKey<DawnUnlockableItemInfo> ChemicalAnalyzer = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "chemical_analyzer");
        public static readonly NamespacedKey<DawnUnlockableItemInfo> ChemicalDistillery = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "chemical_distillery");
        public static readonly NamespacedKey<DawnUnlockableItemInfo> ChemicalMixer = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "chemical_mixer");
    }
}