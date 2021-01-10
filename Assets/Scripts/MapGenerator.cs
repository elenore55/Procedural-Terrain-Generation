using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

[Serializable]
public class MapGenerator : MonoBehaviour
{
    public const int tileSize = 241;
    public float noiseScale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public Material terrainMaterial;
    public TextureData textureData;
    public GenericNoise noiseFunc = new PerlinNoiseFunction();
    public bool rains = false;

    Queue<MapThreadInfo<float[,]>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<float[,]>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public float minHeight
    {
        get { return scale * meshHeightMultiplier * meshHeightCurve.Evaluate(0); }
    }

    public float maxHeight
    {
        get { return scale * meshHeightMultiplier * meshHeightCurve.Evaluate(1); }
    }

    public float scale
    {
        get { return 2f; }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void Start()
    {
        textureData.UpdateMeshHeights(terrainMaterial, minHeight, maxHeight);
    }

    public void RequestHeightMap(Vector2 centre, Action<float[,]> callback)
    {
        ThreadStart threadStart = delegate {
            HeightMapThread(centre, callback);
        };
        new Thread(threadStart).Start();
    }

    void HeightMapThread(Vector2 centre, Action<float[,]> callback)
    {
        float[,] heightMap = GenerateHeightMap(centre);
        lock (heightMapThreadInfoQueue)
        {
            heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<float[,]>(callback, heightMap));
        }
    }

    public void RequestHeightMap(float[,] heightMap, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate {
            MeshDataThread(heightMap, lod, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(float[,] heightMap, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateMesh(heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (heightMapThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < heightMapThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<float[,]> threadInfo = heightMapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public float[,] GenerateHeightMap(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateHeightMap(tileSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, noiseFunc);
        return noiseMap;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
