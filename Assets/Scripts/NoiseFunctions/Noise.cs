using UnityEngine;

public static class Noise
{
    public static float[,] GenerateHeightMap(int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, GenericNoise noiseFunc)
    {
        System.Random rnd = new System.Random(seed);
        float[,] heightMap = new float[mapSize, mapSize];
        Vector2[] octaveOffsets = new Vector2[octaves];
        float amplitude = 1;
        float frequency = 1;
        float maxH = 0;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rnd.Next(-100000, 100000) + offset.x;
            float offsetY = rnd.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxH += amplitude;
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
                    float sampleX = (x - mapSize / 2f + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - mapSize / 2f + octaveOffsets[i].y) / scale * frequency;
                    float val = noiseFunc.Generate(sampleX, sampleY); 
                    height += val * amplitude;
                    amplitude *= persistance; 
                    frequency *= lacunarity;  
                }
                heightMap[x, y] = height;
            }
        }
        for (int y = 0; y < mapSize; y++) { 
            for (int x = 0; x < mapSize; x++) {
                float hNorm = (heightMap[x, y] + 1f) / (maxH / 0.85f);
                heightMap[x, y] = Mathf.Clamp01(hNorm);
            }
        }
        return heightMap;
    }

}