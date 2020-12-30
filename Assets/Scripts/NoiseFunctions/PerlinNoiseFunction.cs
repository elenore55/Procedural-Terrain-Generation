using UnityEngine;

public class PerlinNoiseFunction : GenericNoise
{
    public new float Generate(float a, float b)
    {
        return Mathf.PerlinNoise(a, b);
    }
}
