using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class InfiniteTerrain : MonoBehaviour
{
    const float squareCameraMoveThreshold = 625f;
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Material mapMaterial;
    public static Vector2 cameraPos;

    private static bool rains = false;
    private static bool startedNow = true;

    Vector2 cameraPosOld;
    public static MapGenerator mapGenerator;
    public static int tileSize;
    int tilesVisibleDist;
    public Dictionary<Vector2, Tile> terrainTileDict = new Dictionary<Vector2, Tile>();
    public static List<Tile> tilesVisibleLastUpdate = new List<Tile>();

    public static Dictionary<Vector2, float[,]> mapsToErode;

    public Slider lacunaritySlider;
    public Slider persistanceSlider;
    public Slider octavesSlider;
    public Slider scaleSlider;
    public Dropdown noiseChoice;
    public Dropdown interpChoice;
    int chosenInterp = 0;

    private void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        SlidersInit();
        DropdownsInit();
    }

    private void Start()
    {
        mapsToErode = new Dictionary<Vector2, float[,]>();
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        tileSize = MapGenerator.tileSize -1 ;
        tilesVisibleDist = Mathf.RoundToInt(maxViewDist / tileSize);
        UpdateTiles();
    }

    private void SlidersInit()
    {
        // Azurira se pri generisanju novih blokova terena
        lacunaritySlider.onValueChanged.AddListener(delegate { UpdateLacunarity(); });
        persistanceSlider.onValueChanged.AddListener(delegate { UpdatePersistance(); });
        octavesSlider.onValueChanged.AddListener(delegate { UpdateOctaves(); });
        scaleSlider.onValueChanged.AddListener(delegate { UpdateScale(); });
    }

    private void DropdownsInit()
    {
        noiseChoice.onValueChanged.AddListener(delegate { UpdateNoise(); });
        interpChoice.onValueChanged.AddListener(delegate { UpdateInterp(); });
    }

    private void UpdateLacunarity() { mapGenerator.lacunarity = lacunaritySlider.value; }
    private void UpdatePersistance() { mapGenerator.persistance = persistanceSlider.value; }
    private void UpdateOctaves() { mapGenerator.octaves = (int)octavesSlider.value; }
    private void UpdateScale() { mapGenerator.noiseScale = scaleSlider.value; }

    private void UpdateNoise()
    {
        int chosen = noiseChoice.value;
        switch (chosen)
        {
            case (int)NoiseIndices.Perlin:
                mapGenerator.noiseFunc = new PerlinNoiseFunction();
                break;
            case (int)NoiseIndices.OpenSimplex:
                mapGenerator.noiseFunc = new OpenSimplexNoise();
                break;
            case (int)NoiseIndices.Custom:
                mapGenerator.noiseFunc = new CustomNoise(chosenInterp);
                break;
            default:
                mapGenerator.noiseFunc = new PerlinNoiseFunction();
                break;
        }
    }

    private void UpdateInterp()
    {
        noiseChoice.SetValueWithoutNotify((int)NoiseIndices.Custom);
        chosenInterp = interpChoice.value;
        CustomNoise cn = new CustomNoise(chosenInterp);
        mapGenerator.noiseFunc = cn;
    }

    public static void SetRains(bool r) { rains = r; }

    public static void ResetRainSettings()
    {
        rains = false;
        startedNow = true;
        mapsToErode.Clear();
    }

    void Update()
    {
        if (rains)
        {
            int currentTileX = Mathf.RoundToInt(cameraPos.x / tileSize);
            int currentTileY = Mathf.RoundToInt(cameraPos.y / tileSize);
            if (startedNow)
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j < 1; j++)
                    {
                        Vector2 tileCoord = new Vector2(currentTileX + i, currentTileY + j);
                        mapsToErode[tileCoord] = mapGenerator.GenerateHeightMap(tileCoord * tileSize);
                    }
                }
                startedNow = false;
            }
            foreach (Vector2 coords in mapsToErode.Keys)
                RegenerateTile(coords, mapsToErode[coords]);
        }
        cameraPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z) / mapGenerator.scale;
        if ((cameraPosOld - cameraPos).sqrMagnitude > squareCameraMoveThreshold)
        {
            cameraPosOld = cameraPos;
            UpdateTiles();
        }
    }

    private void RegenerateTile(Vector2 coords, float[,] heightMap)
    {
        Tile t0 = terrainTileDict[coords];
        t0.SetVisible(false);
        terrainTileDict.Remove(coords);
        Tile t = new Tile(coords, tileSize, detailLevels, transform, mapMaterial);
        t.OnMapDataReceived(heightMap);
        terrainTileDict[coords] = t;
    }

    private void UpdateTiles()
    {
        for (int i = 0; i < tilesVisibleLastUpdate.Count; i++)
            tilesVisibleLastUpdate[i].SetVisible(false);
        tilesVisibleLastUpdate.Clear();
        int currentTileX = Mathf.RoundToInt(cameraPos.x / tileSize);
        int currentTileY = Mathf.RoundToInt(cameraPos.y / tileSize);
        for (int y = -tilesVisibleDist; y <= tilesVisibleDist; y++)
        {
            for (int x = -tilesVisibleDist; x <= tilesVisibleDist; x++)
            {
                Vector2 viewedTileCoord = new Vector2(currentTileX + x, currentTileY + y);
                if (terrainTileDict.ContainsKey(viewedTileCoord))
                    terrainTileDict[viewedTileCoord].UpdateTile();
                else
                {
                    Tile t = new Tile(viewedTileCoord, tileSize, detailLevels, transform, mapMaterial);
                    terrainTileDict.Add(viewedTileCoord, t);
                }
                    
            }
        }
    }

}