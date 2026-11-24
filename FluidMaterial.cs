using UnityEngine;

namespace SnowyCraftingCore;

[System.Serializable]
public class FluidMaterial
{
    public bool disableRendererWhenEmpty = false;
    public Renderer renderer = null!;
    public int materialIndex = 0;

    public Material Material => renderer.materials[materialIndex];
}