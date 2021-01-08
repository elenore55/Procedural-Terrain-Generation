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

    Vector2 cameraPosOld;
    public static MapGenerator mapGenerator;
    int tileSize;
    int tilesVisibleDist;
    Dictionary<Vector2, Tile> terrainTileDict = new Dictionary<Vector2, Tile>();
    public static List<Tile> tilesVisibleLastUpdate = new List<Tile>();

    Slider lacunaritySlider;
    Slider persistanceSlider;
    Slider octavesSlider;
    Slider seedSlider;
    Dropdown noiseChoice;
    Dropdown interpChoice;

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        SlidersInit();
        DropdownsInit();
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
        seedSlider = GameObject.Find("Seed").GetComponent<Slider>();
        lacunaritySlider.onValueChanged.AddListener(delegate { UpdateLacunarity(); });
        persistanceSlider.onValueChanged.AddListener(delegate { UpdatePersistance(); });
        octavesSlider.onValueChanged.AddListener(delegate { UpdateOctaves(); });
        seedSlider.onValueChanged.AddListener(delegate { UpdateSeed(); });
    }

    private void DropdownsInit()
    {
        noiseChoice = GameObject.Find("Noises").GetComponent<Dropdown>();
        interpChoice = GameObject.Find("Interps").GetComponent<Dropdown>();
        noiseChoice.onValueChanged.AddListener(delegate { UpdateNoise(); });
        interpChoice.onValueChanged.AddListener(delegate { UpdateInterp(); });
    }

    private void UpdateLacunarity()
    {
        mapGenerator.lacunarity = lacunaritySlider.value;
    }

    private void UpdatePersistance()
    {
        mapGenerator.persistance = persistanceSlider.value;
        UpdateTiles();
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

    private void UpdateInterp()
    {
        int chosen = interpChoice.value;
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
                    terrainTileDict.Add(viewedTileCoord, new Tile(viewedTileCoord, tileSize, detailLevels, transform, mapMaterial));
            }
        }
    }

}