using Dawn;

namespace SnowyCraftingCore
{
    internal static class SnowyCraftingCoreKeys
    {
        public static readonly NamespacedKey<DawnUnlockableItemInfo> Analyzer = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "analyzer");
        public static readonly NamespacedKey<DawnUnlockableItemInfo> Alembic = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "alembic");
        public static readonly NamespacedKey<DawnUnlockableItemInfo> ChemicalMixer = NamespacedKey<DawnUnlockableItemInfo>.From("snowy_crafting_core", "chemical_mixer");
    }
}