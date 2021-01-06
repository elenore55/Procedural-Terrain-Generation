using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class EndlessTerrain : MonoBehaviour
{
    const float cameraMoveThreshold = 25f;
    const float squareCameraMoveThreshold = cameraMoveThreshold * cameraMoveThreshold;
    public LODInfo[] detailLevels;
    public static float maxViewDst;
    public Material mapMaterial;
    public static Vector2 cameraPos;

    Vector2 cameraPosOld;
    static MapGenerator mapGenerator;
    int tileSize;
    int tilesVisibleInViewDst;
    Dictionary<Vector2, Tile> terrainTileDict = new Dictionary<Vector2, Tile>();
    static List<Tile> terrainTilesVisibleLastUpdate = new List<Tile>();

    Slider lacunaritySlider;
    Slider persistanceSlider;
    Slider octavesSlider;
    Slider seedSlider;
    Dropdown noiseChoice;

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        InitializeSliders();
        noiseChoice = GameObject.Find("Noises").GetComponent<Dropdown>();
        noiseChoice.onValueChanged.AddListener(delegate { UpdateNoise(); });
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        tileSize = MapGenerator.tileSize - 1;
        tilesVisibleInViewDst = Mathf.RoundToInt(maxViewDst / tileSize);
        UpdateTiles();
    }

    void InitializeSliders()
    {
        lacunaritySlider = GameObject.Find("Lacunarity").GetComponent<Slider>();
        persistanceSlider = GameObject.Find("Persistance").GetComponent<Slider>();
        octavesSlider = GameObject.Find("Octaves").GetComponent<Slider>();
        seedSlider = GameObject.Find("Seed").GetComponent<Slider>();
        lacunaritySlider.onValueChanged.AddListener(delegate { UpdateLacunarity(); });
        persistanceSlider.onValueChanged.AddListener(delegate { UpdatePersistance(); });
        octavesSlider.onValueChanged.AddListener(delegate { UpdateOctaves(); });
        seedSlider.onValueChanged.AddListener(delegate { UpdateSeed(); });
    }

    private void UpdateLacunarity()
    {
        mapGenerator.lacunarity = lacunaritySlider.value;
    }

    private void UpdatePersistance()
    {
        mapGenerator.persistance = persistanceSlider.value;
    }

    private void UpdateOctaves()
    {
        mapGenerator.octaves = (int)octavesSlider.value;
    }

    private void UpdateSeed()
    {
        mapGenerator.seed = (int)seedSlider.value;
    }

    private void UpdateNoise()
    {
        int chosen = noiseChoice.value;
        switch (chosen)
        {
            case 0:
                mapGenerator.noiseFunc = new PerlinNoiseFunction();
                break;
            case 1:
                mapGenerator.noiseFunc = new OpenSimplexNoise();
                break;
            case 2:
                mapGenerator.noiseFunc = new CustomNoise();
                break;
            default:
                mapGenerator.noiseFunc = new PerlinNoiseFunction();
                break;
        }
    }

    void Update()
    {
        cameraPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z) / mapGenerator.scale;
        if ((cameraPosOld - cameraPos).sqrMagnitude > squareCameraMoveThreshold)
        {
            cameraPosOld = cameraPos;
            UpdateTiles();
        }
    }

    private void UpdateTiles()
    {
        for (int i = 0; i < terrainTilesVisibleLastUpdate.Count; i++)
            terrainTilesVisibleLastUpdate[i].SetVisible(false);
        terrainTilesVisibleLastUpdate.Clear();
        int currentTileX = Mathf.RoundToInt(cameraPos.x / tileSize);
        int currentTileY = Mathf.RoundToInt(cameraPos.y / tileSize);
        for (int offsetY = -tilesVisibleInViewDst; offsetY <= tilesVisibleInViewDst; offsetY++)
        {
            for (int offsetX = -tilesVisibleInViewDst; offsetX <= tilesVisibleInViewDst; offsetX++)
            {
                Vector2 viewedTileCoord = new Vector2(currentTileX + offsetX, currentTileY + offsetY);
                if (terrainTileDict.ContainsKey(viewedTileCoord))
                    terrainTileDict[viewedTileCoord].UpdateTerrainChunk();
                else
                    terrainTileDict.Add(viewedTileCoord, new Tile(viewedTileCoord, tileSize, detailLevels, transform, mapMaterial));
            }
        }
    }

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
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public Tile(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);     
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3 * mapGenerator.scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.scale;
            SetVisible(false);      
            LODMeshes = new LevelOfDetailMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                LODMeshes[i] = new LevelOfDetailMesh(detailLevels[i].LOD, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = LODMeshes[i];
                }
            }
            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(cameraPos));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;
                if (visible)
                {
                    int LODIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                            LODIndex = i + 1;
                        else
                            break;
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
                            lodMesh.RequestMesh(mapData);
                    }
                    if (LODIndex == 0)
                    {
                        if (collisionLODMesh.hasMesh)
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        else if (!collisionLODMesh.hasRequestedMesh)
                            collisionLODMesh.RequestMesh(mapData);
                    }
                    terrainTilesVisibleLastUpdate.Add(this);
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

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, LOD, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int LOD;
        public float visibleDstThreshold;
        public bool useForCollider;
    }
}