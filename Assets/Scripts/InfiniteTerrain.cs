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
    public static float[,] erodedMap;

    public static bool rains = false;
    public static bool startedNow = true;
    private int counter = 0;

    Vector2 cameraPosOld;
    public static MapGenerator mapGenerator;
    public static int tileSize;
    int tilesVisibleDist;
    public Dictionary<Vector2, Tile> terrainTileDict = new Dictionary<Vector2, Tile>();
    public static List<Tile> tilesVisibleLastUpdate = new List<Tile>();

    Slider lacunaritySlider;
    Slider persistanceSlider;
    Slider octavesSlider;
    Slider scaleSlider;
    Dropdown noiseChoice;
    Dropdown interpChoice;

    private void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        SlidersInit();
        DropdownsInit();
    }

    private void Start()
    {
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        tileSize = MapGenerator.tileSize -1 ;
        tilesVisibleDist = Mathf.RoundToInt(maxViewDist / tileSize);
        UpdateTiles();
    }

    private void SlidersInit()
    {
        lacunaritySlider = GameObject.Find("Lacunarity").GetComponent<Slider>();
        persistanceSlider = GameObject.Find("Persistance").GetComponent<Slider>();
        octavesSlider = GameObject.Find("Octaves").GetComponent<Slider>();
        scaleSlider = GameObject.Find("Scale").GetComponent<Slider>();
        AddSliderListeners();
    }

    private void AddSliderListeners()
    {
        // Azurira se pri generisanju novih blokova terena
        lacunaritySlider.onValueChanged.AddListener(delegate { UpdateLacunarity(); });
        persistanceSlider.onValueChanged.AddListener(delegate { UpdatePersistance(); });
        octavesSlider.onValueChanged.AddListener(delegate { UpdateOctaves(); });
        scaleSlider.onValueChanged.AddListener(delegate { UpdateScale(); });
    }

    private void DropdownsInit()
    {
        noiseChoice = GameObject.Find("Noises").GetComponent<Dropdown>();
        interpChoice = GameObject.Find("Interps").GetComponent<Dropdown>();
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
                mapGenerator.noiseFunc = new CustomNoise();
                break;
            case (int)NoiseIndices.Cubic:
                mapGenerator.noiseFunc = new CubicNoise();
                break;
            default:
                mapGenerator.noiseFunc = new PerlinNoiseFunction();
                break;
        }
    }

    private void UpdateInterp()
    {
        noiseChoice.SetValueWithoutNotify((int)NoiseIndices.Custom);
        int chosen = interpChoice.value;
        CustomNoise cn = new CustomNoise();
        switch(chosen)
        {
            case (int)InterpIndices.Cosine:
                cn.Callback = cn.Cosine;
                break;
            case (int)InterpIndices.Acceleration:
                cn.Callback = cn.Acceleration;
                break;
            case (int)InterpIndices.Smoothstep:
                cn.Callback = cn.SmoothStep;
                break;
            case (int)InterpIndices.Deceleration:
                cn.Callback = cn.Deceleration;
                break;
            case (int)InterpIndices.Linear:
                cn.Callback = cn.Linear;
                break;
        }
        mapGenerator.noiseFunc = cn;
    }

    void Update()
    {
        if (rains)
        {
            counter++;
            int currentTileX = Mathf.RoundToInt(cameraPos.x / tileSize);
            int currentTileY = Mathf.RoundToInt(cameraPos.y / tileSize);
            if (startedNow)
            {
                Vector2 viewedTileCoord = new Vector2(currentTileX, currentTileY);
                viewedTileCoord *= tileSize;
                erodedMap = mapGenerator.GenerateHeightMap(viewedTileCoord);
                startedNow = false;
            }
            RegenerateTile(currentTileX, currentTileY, erodedMap);

            //RegenerateTile(currentTileX + 1, currentTileY);
            //RegenerateTile(currentTileX - 1, currentTileY);
            //RegenerateTile(currentTileX - 1, currentTileY + 1);
            //RegenerateTile(currentTileX + 1, currentTileY + 1);
            //RegenerateTile(currentTileX, currentTileY + 1);
        }
        cameraPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z) / mapGenerator.scale;
        if ((cameraPosOld - cameraPos).sqrMagnitude > squareCameraMoveThreshold)
        {
            cameraPosOld = cameraPos;
            UpdateTiles();
        }
    }

    private void RegenerateTile(int coordX, int coordY, float[,] heightMap)
    {
        Vector2 viewedTileCoord = new Vector2(coordX, coordY);
        Tile t0 = terrainTileDict[viewedTileCoord];
        t0.SetVisible(false);
        terrainTileDict.Remove(viewedTileCoord);
        Tile t = new Tile(viewedTileCoord, tileSize, detailLevels, transform, mapMaterial);
        t.OnMapDataReceived(heightMap);
        terrainTileDict[viewedTileCoord] = t;
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