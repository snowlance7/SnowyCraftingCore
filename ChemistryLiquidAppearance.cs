using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowyCraftingCore
{
    public struct ChemistryLiquidAppearance(Color liquidColor = default, float emissionIntensity = 0f) : INetworkSerializable
    {
        public Color liquidColor = liquidColor;
        public float emissionIntensity = emissionIntensity;

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
