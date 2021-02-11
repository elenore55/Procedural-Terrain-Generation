using UnityEngine;

public class PerlinNoiseFunction : GenericNoise
{
    public override float Generate(float a, float b)
    {
        return Mathf.PerlinNoise(a, b) * 2 - 1;
    }
}
