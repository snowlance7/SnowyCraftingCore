using Dawn;
using System;
using Unity.Netcode;

namespace SnowyCraftingCore
{
    public class ChemistryIngredient(NamespacedKey<DawnItemInfo> item, ChemistryLiquidAppearance? chemistryLiquidAppearance = default, string specialInstructions = "") : IEquatable<ChemistryIngredient>, INetworkSerializable
    {
        public NamespacedKey<DawnItemInfo> item = item;
        public ChemistryLiquidAppearance chemistryLiquidAppearance = chemistryLiquidAppearance ?? new ChemistryLiquidAppearance();
        public string specialInstructions = specialInstructions;

        public bool Equals(ChemistryIngredient other)
        {
            return item == other.item;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref item);
            serializer.SerializeValue(ref chemistryLiquidAppearance);
            serializer.SerializeValue(ref specialInstructions);
        }

        public override string ToString()
        {
            return item.Key;
        }
    }
}
