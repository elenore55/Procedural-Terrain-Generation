using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, GenericNoise noiseFunc)
    {
        System.Random rnd = new System.Random(seed);
        float[,] noiseMap = new float[mapSize, mapSize];
        Vector2[] octaveOffsets = new Vector2[octaves];
        float maxHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        float maxH = float.MinValue;
        float minH = float.MaxValue;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rnd.Next(-100000, 100000) + offset.x;
            float offsetY = rnd.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
            scale = 0.001f;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - mapSize / 2f + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - mapSize / 2f + octaveOffsets[i].y) / scale * frequency;
                    float perlinValue = noiseFunc.Generate(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                noiseMap[x, y] = noiseHeight;
                if (noiseHeight < minH)
                    minH = noiseHeight;
                if (noiseHeight > maxH)
                    maxH = noiseHeight;
            }
        }
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float normalizedHeight = (noiseMap[x, y] + 1) / (maxHeight / 0.9f); 
                // noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                // noiseMap[x, y] = Mathf.Clamp(normalizedHeight, minH, maxH);
                noiseMap[x, y] = Mathf.InverseLerp(minH, maxH, noiseMap[x, y]);
                // noiseMap[x, y] = Mathf.InverseLerp(0, normalizedHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }

}