using Dawn;
using System;
using Unity.Netcode;

namespace SnowyCraftingCore
{
    public class ChemistryIngredient : IEquatable<ChemistryIngredient>, INetworkSerializable
    {
        public NamespacedKey<DawnItemInfo> item;
        public ChemistryLiquidAppearance chemistryLiquidAppearance;
        public string specialInstructions;

        public ChemistryIngredient()
        {
            item = new NamespacedKey<DawnItemInfo>();
            chemistryLiquidAppearance = new ChemistryLiquidAppearance();
            specialInstructions = "";
        }

        public ChemistryIngredient(NamespacedKey<DawnItemInfo> item, ChemistryLiquidAppearance? chemistryLiquidAppearance = default, string specialInstructions = "")
        {
            this.item = item;
            this.chemistryLiquidAppearance = chemistryLiquidAppearance ?? new ChemistryLiquidAppearance();
            this.specialInstructions = specialInstructions;
        }

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
