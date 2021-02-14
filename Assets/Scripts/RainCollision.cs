using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    ParticleSystem rainSystem;
    List<ParticleCollisionEvent> collisionEvents;

    public Slider gravitySlider;
    public Slider maxIterSlider;
    public Slider erosionSpeedSlider;
    public Slider evapSpeedSlider;
    public Slider depositSpeedSlider;
    public Slider radiusSlider;
    public Slider inertiaSlider;

    int RADIUS = 5;
    float INERTIA = 0.5f;
    const float WATER_CAPACITY = 3;
    const float SEDIMENT_CAPACITY = 4;
    const float MIN_SEDIMENT_CAPACITY = 0.1f;
    float GRAVITY = 4;
    const float INITIAL_SPEED = 1;
    float EROSION_SPEED = 0.5f;
    float DEPOSIT_SPEED = 0.3f;
    float EVAPORATION_SPEED = 0.01f;
    int MAX_ITERS = 20;

    Dictionary<Vector2Int, List<Vector2Int>> radiusIndices;
    Dictionary<Vector2Int, List<float>> radiusWeights;

    private void Awake()
    {
        SlidersInit();
        rainSystem = FindObjectOfType<ParticleSystem>();
    }

    private void Start()
    {
        collisionEvents = new List<ParticleCollisionEvent>();
        radiusIndices = new Dictionary<Vector2Int, List<Vector2Int>>();
        radiusWeights = new Dictionary<Vector2Int, List<float>>();
        DetermineErosionRadiusIndices(InfiniteTerrain.tileSize);
    }

    private void SlidersInit()
    {
        gravitySlider.onValueChanged.AddListener(delegate { UpdateGravity(); });
        maxIterSlider.onValueChanged.AddListener(delegate { UpdateLifeSpan(); });
        erosionSpeedSlider.onValueChanged.AddListener(delegate { UpdateErosionSpeed(); });
        evapSpeedSlider.onValueChanged.AddListener(delegate { UpdateEvapSpeed(); });
        depositSpeedSlider.onValueChanged.AddListener(delegate { UpdateDepositSpeed(); });
        radiusSlider.onValueChanged.AddListener(delegate { UpdateRadius(); });
        inertiaSlider.onValueChanged.AddListener(delegate { UpdateInertia(); });

    }

    private void UpdateGravity() { GRAVITY = gravitySlider.value; }
    private void UpdateErosionSpeed() { EROSION_SPEED = erosionSpeedSlider.value; }
    private void UpdateDepositSpeed() { DEPOSIT_SPEED = depositSpeedSlider.value; }
    private void UpdateEvapSpeed() { EVAPORATION_SPEED = evapSpeedSlider.value; }
    private void UpdateLifeSpan() { MAX_ITERS = (int)maxIterSlider.value; }
    private void UpdateInertia() { INERTIA = inertiaSlider.value; }
    private void UpdateRadius()
    {
        RADIUS = (int)radiusSlider.value;
        radiusIndices = new Dictionary<Vector2Int, List<Vector2Int>>();
        radiusWeights = new Dictionary<Vector2Int, List<float>>();
        DetermineErosionRadiusIndices(InfiniteTerrain.tileSize);
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = rainSystem.GetCollisionEvents(other, collisionEvents);
        RainMovement.IncreaseNumOfRaindrops(numCollisionEvents);
        int sz = InfiniteTerrain.tileSize;
        float deltaH = 0.01f;
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 worldPos = collisionEvents[i].intersection;
            Vector2 tileCoords = GetTileCoords(worldPos, sz);
            if (!InfiniteTerrain.mapsToErode.ContainsKey(tileCoords))
                continue;
            float[,] mapToErode = InfiniteTerrain.mapsToErode[tileCoords];
            Vector2 dropletPos = GetOnMapPosition(worldPos, sz);
            if (IsBorderPosition(dropletPos, sz))
                continue;
            Vector2 dir = new Vector2();
            float water = WATER_CAPACITY;
            float speed = INITIAL_SPEED;
            float sediment = 0;
            for (int j = 0; j < MAX_ITERS; j++)
            {
                int iX = (int)dropletPos.x;
                int iY = (int)dropletPos.y;
                float u = dropletPos.x - iX; 
                float v = dropletPos.y - iY;
                Vector2 grad = CalculateGradient(dropletPos, mapToErode);
                float height = CalculateHeight(dropletPos, mapToErode);
                dir = dir * INERTIA - grad * (1 - INERTIA); 
                dir.Normalize();
                dropletPos += dir;
                if (!InRange(dropletPos, sz))
                    break;

                float heightNew = CalculateHeight(dropletPos, mapToErode);
                deltaH = heightNew - height;

                float sedimentCapacity = Mathf.Max(-deltaH * speed * water * SEDIMENT_CAPACITY, MIN_SEDIMENT_CAPACITY);
                if (deltaH > 0 || sediment > sedimentCapacity)
                {
                    float depositAmount;
                    if (deltaH > 0) depositAmount = Mathf.Min(deltaH, sediment);
                    else depositAmount = (sediment - sedimentCapacity) * DEPOSIT_SPEED;
                    sediment -= depositAmount;
                    mapToErode[iX, iY] += depositAmount * (1 - u) * (1 - v);
                    mapToErode[iX + 1, iY] += depositAmount * u * (1 - v);
                    mapToErode[iX, iY + 1] += depositAmount * (1 - u) * v;
                    mapToErode[iX + 1, iY + 1] += depositAmount * u * v;
                    if (deltaH > 0)
                        break;
                }
                else
                {
                    float erosionAmount = Mathf.Min((sedimentCapacity - sediment) * EROSION_SPEED, Mathf.Abs(deltaH));
                    Vector2Int key = new Vector2Int(iX, iY);
                    List<Vector2Int> nodeIndicesList = radiusIndices[key];
                    List<float> weightsList = radiusWeights[key];
                    for (int k = 0; k < weightsList.Count; k++)
                    {
                        float weighedErosionAmt = erosionAmount * weightsList[k];
                        int X = nodeIndicesList[k].x;
                        int Y = nodeIndicesList[k].y;
                        float deltaSediment;
                        if (mapToErode[X, Y] < weighedErosionAmt)
                            deltaSediment = mapToErode[X, Y];
                        else deltaSediment = weighedErosionAmt;
                        mapToErode[X, Y] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }
                InfiniteTerrain.mapsToErode[tileCoords] = mapToErode;
            }
            water *= (1 - EVAPORATION_SPEED);
            speed = Mathf.Sqrt(Mathf.Pow(speed, 2) + deltaH * GRAVITY);
        }
    }

    private Vector2 GetOnMapPosition(Vector3 worldPos, int mapSize)
    {
        int x = (int)worldPos.x % mapSize;
        int y = (int)worldPos.z % mapSize;
        if (x < 0) x += mapSize;
        if (y < 0) y += mapSize;
        float u = worldPos.x - (int)worldPos.x;
        float v = worldPos.z - (int)worldPos.z;
        return new Vector2(x + u, y + v);
    }

    private Vector2 GetTileCoords(Vector3 worldPos, int mapSize)
    {
        worldPos /= InfiniteTerrain.mapGenerator.scale;
        int coordX = Mathf.RoundToInt(worldPos.x / mapSize);
        int coordY = Mathf.RoundToInt(worldPos.z / mapSize);
        return new Vector2(coordX, coordY);
    }

    private Vector2 CalculateGradient(Vector2 pos, float[,] heightMap)
    {
        Vector2 grad = new Vector2();
        int x = (int)pos.x;
        int y = (int)pos.y;
        float u = pos.x - x;
        float v = pos.y - y;
        CellHeights ch = GetCellHeights(pos, heightMap);
        grad.x = (ch.h10() - ch.h00()) * (1 - v) + (ch.h11() - ch.h01()) * v;
        grad.y = (ch.h01() - ch.h00()) * (1 - u) + (ch.h11() - ch.h10()) * u;
        return grad;
    }

    private float CalculateHeight(Vector2 pos, float[,] heightMap)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        float u = pos.x - x;
        float v = pos.y - y;
        CellHeights ch = GetCellHeights(pos, heightMap);
        float height1x = ch.h00() + u * (ch.h10() - ch.h00());
        float height2x = ch.h01() + u * (ch.h11() - ch.h01());
        return height1x + v * (height2x - height1x);
    }

    private CellHeights GetCellHeights(Vector2 pos, float[,] map)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        return new CellHeights(map[x, y], map[x + 1, y], map[x, y + 1], map[x + 1, y + 1]);
    }

    private void DetermineErosionRadiusIndices(int mapSize)
    {
        List<Vector2Int> coords = new List<Vector2Int>();
        float weightSum = 0;
        for (int row = 0; row < mapSize; row++)
        {
            for (int column = 0; column < mapSize; column++)
            {
                Vector2Int key = new Vector2Int(row, column);
                if (!radiusIndices.ContainsKey(key)) radiusIndices[key] = new List<Vector2Int>();
                if (!radiusWeights.ContainsKey(key)) radiusWeights[key] = new List<float>();
                List<float> weightsTemp = new List<float>();
                weightSum = 0;
                for (int y = -RADIUS + 1; y < RADIUS; y++)
                {
                    for (int x = -RADIUS + 1; x < RADIUS; x++)
                    {
                        float distance = x * x + y * y;
                        if (distance < RADIUS * RADIUS)
                        {
                            Vector2Int currentNode = new Vector2Int(column + x, row + y);
                            if (InRange(currentNode, mapSize))
                            {
                                radiusIndices[key].Add(currentNode);
                                float weight = Mathf.Abs(RADIUS - (key - currentNode).magnitude);
                                weightSum += weight;
                                weightsTemp.Add(weight);
                            }
                        }
                    }
                }
                for (int k = 0; k < weightsTemp.Count; k++)
                    radiusWeights[key].Add(weightsTemp[k] / weightSum);
            }
        }
    }

    private bool InRange(Vector2 pos, int size)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < size && pos.y < size;
    }

    private bool IsBorderPosition(Vector2 pos, int size)
    {
        return pos.x < 1 || pos.x > size - 2 || pos.y < 1 || pos.y > size - 2;
    }

}

class CellHeights
{ 
    float height00;
    float height10;
    float height01;
    float height11;

    public CellHeights(float h00, float h10, float h01, float h11)
    {
        height00 = h00;
        height10 = h10;
        height01 = h01;
        height11 = h11;
    }

    public float h00() { return height00; }
    public float h10() { return height10; }
    public float h01() { return height01; }
    public float h11() { return height11; }
}
