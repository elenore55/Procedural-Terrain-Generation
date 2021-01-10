using UnityEngine;

public static class Noise
{
    public static float[,] GenerateHeightMap(int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, GenericNoise noiseFunc)
    {
        System.Random rnd = new System.Random(seed);
        float[,] heightMap = new float[mapSize, mapSize];
        // we want each octave to be sampled from a different location
        Vector2[] octaveOffsets = new Vector2[octaves];
        float amplitude = 1;
        float frequency = 1;

        float maxH = float.MinValue;
        float minH = float.MaxValue;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rnd.Next(-100000, 100000) + offset.x;
            float offsetY = rnd.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            amplitude *= persistance;
        }

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                amplitude = 1;
                frequency = 1;
                float height = 0;
                for (int i = 0; i < octaves; i++)
                {
                    // integer values will give us same value every time
                    // zato nije sampleX = x
                    // moze ici sampleX = x / scale -> dobije se non-integer
                    // the higher the frequency, the further apart sample points will be ->
                             // height values will change more rapidly
                    // mapSize / 2 da bi se skaliralo u odnosu na centar
                    float sampleX = (x - mapSize / 2f + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - mapSize / 2f + octaveOffsets[i].y) / scale * frequency;
                    float val = noiseFunc.Generate(sampleX, sampleY); 
                    height += val * amplitude;
                    amplitude *= persistance; // per je [0, 1] -> ampl se smanjuje
                    frequency *= lacunarity;  // lac > 1 -> freq se povecava
                }
                heightMap[x, y] = height;
                if (height < minH) minH = height;
                if (height > maxH) maxH = height;
            }
        }
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                heightMap[x, y] = Mathf.InverseLerp(minH, maxH, heightMap[x, y]);
        return heightMap;
    }

}