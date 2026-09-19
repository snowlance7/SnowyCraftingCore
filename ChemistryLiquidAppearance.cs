using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowyCraftingCore
{
    public struct ChemistryLiquidAppearance : INetworkSerializable
    {
        public Color liquidColor;
        public float emissionIntensity;

        public ChemistryLiquidAppearance()
        {
            liquidColor = new Color();
            emissionIntensity = 0f;
        }

        public ChemistryLiquidAppearance(Color liquidColor = default, float emissionIntensity = 0f)
        {
            this.liquidColor = liquidColor;
            this.emissionIntensity = emissionIntensity;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref liquidColor);
            serializer.SerializeValue(ref emissionIntensity);
        }

        public override string ToString()
        {
            return $"Color: {liquidColor.ToString()}, EmissionIntensity: {emissionIntensity}";
        }
    }
}
