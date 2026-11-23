using UnityEngine;

namespace SnowyCraftingCore;

[System.Serializable]
public class FluidMaterial
{
    [SerializeField] Renderer renderer = null!;
    [SerializeField] int materialIndex = 0;
    public Material Material => renderer.materials[materialIndex];
}