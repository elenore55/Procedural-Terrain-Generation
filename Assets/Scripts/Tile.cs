using UnityEngine;

public class Tile
{
    GameObject meshObject;
    Vector2 position;
    Bounds bounds;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    LODInfo[] detailLevels;
    LevelOfDetailMesh[] LODMeshes;
    LevelOfDetailMesh collisionLODMesh;
    float[,] heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;

    public Tile(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
    {
        this.detailLevels = detailLevels;
        position = coord * size;
        bounds = new Bounds(position, Vector2.one * size);
        MeshObjectInit(parent, material);
        LODMeshesInit();
        InfiniteTerrain.mapGenerator.RequestHeightMap(position, OnMapDataReceived);
    }

    void LODMeshesInit()
    {
        LODMeshes = new LevelOfDetailMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            LODMeshes[i] = new LevelOfDetailMesh(detailLevels[i].LOD, UpdateTile);
            if (detailLevels[i].useForCollider)
                collisionLODMesh = LODMeshes[i];
        }
    }

    void MeshObjectInit(Transform parent, Material material)
    {
        Vector3 position3D = new Vector3(position.x, 0, position.y);
        meshObject = new GameObject("Terrain Chunk");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;
        meshObject.transform.position = position3D * InfiniteTerrain.mapGenerator.scale;
        meshObject.transform.parent = parent;
        meshObject.transform.localScale = Vector3.one * InfiniteTerrain.mapGenerator.scale;
    }

    void OnMapDataReceived(float[,] heightMap)
    {
        this.heightMap = heightMap;
        heightMapReceived = true;
        UpdateTile();
    }

    public void UpdateTile()
    {
        if (heightMapReceived)
        {
            float cameraDistFromEdge = Mathf.Sqrt(bounds.SqrDistance(InfiniteTerrain.cameraPos));
            bool visible = cameraDistFromEdge <= InfiniteTerrain.maxViewDist;
            if (visible)
            {
                int LODIndex = 0;
                for (int i = 1; i < detailLevels.Length; i++)
                {
                    if (cameraDistFromEdge > detailLevels[i].visibleDistThreshold)
                        LODIndex = i;
                    else break;
                }
                if (LODIndex != previousLODIndex)
                {
                    LevelOfDetailMesh lodMesh = LODMeshes[LODIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = LODIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                        lodMesh.RequestMesh(heightMap);
                }
                if (LODIndex == 0)
                {
                    if (collisionLODMesh.hasMesh)
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                    else if (!collisionLODMesh.hasRequestedMesh)
                        collisionLODMesh.RequestMesh(heightMap);
                }
                InfiniteTerrain.tilesVisibleLastUpdate.Add(this);
            }
            SetVisible(visible);
        }
    }
    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }
}

class LevelOfDetailMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int LOD;
    System.Action updateCallback;

    public LevelOfDetailMesh(int LOD, System.Action updateCallback)
    {
        this.LOD = LOD;
        this.updateCallback = updateCallback;
    }

    void OnMeshDataReceived(MeshData meshData)
    {
        mesh = meshData.CreateMesh();
        hasMesh = true;
        updateCallback();
    }

    public void RequestMesh(float[,] heigthMap)
    {
        hasRequestedMesh = true;
        InfiniteTerrain.mapGenerator.RequestHeightMap(heigthMap, LOD, OnMeshDataReceived);
    }
}

[System.Serializable]
public struct LODInfo
{
    public int LOD;
    public float visibleDistThreshold;
    public bool useForCollider;
}
