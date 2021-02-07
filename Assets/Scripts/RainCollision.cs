using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    ParticleSystem rainSystem;
    List<ParticleCollisionEvent> collisionEvents;
    ParticleSystem.Particle[] droplets;
    Slider gravitySlider;
    Slider maxIterSlider;
    Slider erosionSpeedSlider;
    Slider evapSpeedSlider;
    Slider depositSpeedSlider;
    Slider radiusSlider;
    Slider inertiaSlider;

    int RADIUS = 7;
    float INERTIA = 0.5f;
    const float WATER_CAPACITY = 3;
    const float SEDIMENT_CAPACITY = 4;
    const float MIN_SEDIMENT_CAPACITY = 0.1f;
    float GRAVITY = 4;
    const float INITIAL_SPEED = 1;
    float EROSION_SPEED = 0.3f;
    float DEPOSIT_SPEED = 0.3f;
    float EVAPORATION_SPEED = 0.01f;
    int MAX_ITERS = 20;

    int[][] erosionBrushIndices;
    float[][] erosionBrushWeights;

    int currentErosionRadius;
    int currentMapSize;

    private void Awake()
    {
        SlidersInit();
        rainSystem = FindObjectOfType<ParticleSystem>();
    }

    private void Start()
    {
        collisionEvents = new List<ParticleCollisionEvent>();
        if (droplets == null || droplets.Length < rainSystem.main.maxParticles)
            droplets = new ParticleSystem.Particle[rainSystem.main.maxParticles];
    }

    private void SlidersInit()
    {
        gravitySlider = GameObject.Find("Gravity").GetComponent<Slider>();
        maxIterSlider = GameObject.Find("LifeSpan").GetComponent<Slider>();
        erosionSpeedSlider = GameObject.Find("ErosionSpeed").GetComponent<Slider>();
        evapSpeedSlider = GameObject.Find("EvapSpeed").GetComponent<Slider>();
        depositSpeedSlider = GameObject.Find("DepositSpeed").GetComponent<Slider>();
        radiusSlider = GameObject.Find("Radius").GetComponent<Slider>();
        inertiaSlider = GameObject.Find("Inertia").GetComponent<Slider>();
        AddSliderListeners();
        
    }

    private void AddSliderListeners()
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
    private void UpdateRadius() { RADIUS = (int)radiusSlider.value; }
    private void UpdateInertia() { INERTIA = inertiaSlider.value; } 


    private void Initialize(int mapSize)
    {
        if (erosionBrushIndices == null || currentErosionRadius != RADIUS || currentMapSize != mapSize)
        {
            InitializeBrushIndices(mapSize, RADIUS);
            currentErosionRadius = RADIUS;
            currentMapSize = mapSize;
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = rainSystem.GetCollisionEvents(other, collisionEvents);
        RainMovement.IncreaseNumOfRaindrops(numCollisionEvents);
        int sz = InfiniteTerrain.tileSize;
        float deltaH = 0.01f;
        Initialize(sz);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 worldPos = collisionEvents[i].intersection;
            Vector2 dropletPos = GetOnMapPosition(worldPos, sz);
            if (dropletPos.x < 1 || dropletPos.x > sz - 2 || dropletPos.y < 1 || dropletPos.y > sz - 2)
                continue;
            Vector2 dir = new Vector2();
            float water = WATER_CAPACITY;
            float speed = INITIAL_SPEED;
            float sediment = 0;

            for (int j = 0; j < MAX_ITERS; j++)
            {
                int iX = (int)dropletPos.x;
                int iY = (int)dropletPos.y;
                int dropletArrIndex = iY * sz + iX;
                float u = dropletPos.x - iX; 
                float v = dropletPos.y - iY;

                Vector2 grad = CalculateGradient(dropletPos, InfiniteTerrain.erodedMap);
                float height = CalculateHeight(dropletPos, InfiniteTerrain.erodedMap);
                dir = dir * INERTIA - grad * (1 - INERTIA);
                dir.Normalize();
                dropletPos += dir;
                if (dropletPos.x <= 0 || dropletPos.x >= sz - 1 || dropletPos.y <= 0 || dropletPos.y >= sz - 1)
                    break;

                float heightNew = CalculateHeight(dropletPos, InfiniteTerrain.erodedMap);
                deltaH = heightNew - height;

                float sedimentCapacity = Mathf.Max(-deltaH * speed * water * SEDIMENT_CAPACITY, MIN_SEDIMENT_CAPACITY);
                if (sediment > sedimentCapacity || deltaH > 0)
                {
                    float depositAmount;
                    if (deltaH > 0) depositAmount = Mathf.Min(deltaH, sediment);
                    else depositAmount = (sediment - sedimentCapacity) * DEPOSIT_SPEED;
                    sediment -= depositAmount;
                    InfiniteTerrain.erodedMap[iX, iY] += depositAmount * (1 - u) * (1 - v);
                    InfiniteTerrain.erodedMap[iX + 1, iY] += depositAmount * u * (1 - v);
                    InfiniteTerrain.erodedMap[iX, iY + 1] += depositAmount * (1 - u) * v;
                    InfiniteTerrain.erodedMap[iX + 1, iY + 1] += depositAmount * u * v;

                }
                else
                {
                    float erosionAmount = Mathf.Min((sedimentCapacity - sediment) * EROSION_SPEED, -deltaH);
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletArrIndex].Length; brushPointIndex++)
                    {
                        int nodeIndex = erosionBrushIndices[dropletArrIndex][brushPointIndex];
                        int X = nodeIndex % sz;
                        int Y = nodeIndex / sz;
                        float weighedErodeAmount = erosionAmount * erosionBrushWeights[dropletArrIndex][brushPointIndex];
                        float deltaSediment;
                        if (InfiniteTerrain.erodedMap[X, Y] < weighedErodeAmount) deltaSediment = InfiniteTerrain.erodedMap[X, Y];
                        else deltaSediment = weighedErodeAmount;
                        InfiniteTerrain.erodedMap[X, Y] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }
            }
            speed = Mathf.Sqrt(speed * speed + deltaH * GRAVITY);
            water *= (1 - EVAPORATION_SPEED);
        }
    }

    Vector2 GetOnMapPosition(Vector3 worldPos, int mapSize)
    {
        int x = (int)worldPos.x % mapSize;
        int y = (int)worldPos.z % mapSize;
        if (x < 0) x += mapSize;
        if (y < 0) y += mapSize;
        float u = worldPos.x - (int)worldPos.x;
        float v = worldPos.z - (int)worldPos.z;
        return new Vector2(x + u, y + v);
    }

    Vector2 CalculateGradient(Vector2 pos, float[,] heightMap)
    {
        Vector2 grad = new Vector2();
        int x = (int)pos.x;
        int y = (int)pos.y;
        float u = pos.x - x;
        float v = pos.y - y;
        CellHeights ch = GetCellHeights(pos, heightMap);
        grad.x = (ch.height10 - ch.height00) * (1 - v) + (ch.height11 - ch.height01) * v;
        grad.y = (ch.height01 - ch.height00) * (1 - u) + (ch.height11 - ch.height10) * u;
        return grad;
    }

    float CalculateHeight(Vector2 pos, float[,] heightMap)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        float u = pos.x - x;
        float v = pos.y - y;
        CellHeights ch = GetCellHeights(pos, heightMap);
        return ch.height00 * (1 - u) * (1 - v) + ch.height10 * u * (1 - v) + 
               ch.height01 * (1 - u) * v + ch.height11 * u * v;
    }

    CellHeights GetCellHeights(Vector2 pos, float[,] map)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        return new CellHeights(map[x, y], map[x + 1, y], map[x, y + 1], map[x + 1, y + 1]);
    }

    void InitializeBrushIndices(int mapSize, int radius)
    {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength(0); i++)
        {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius)
            {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius)
                        {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize)
                            {
                                float weight = 1 - Mathf.Sqrt(sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }
            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++)
            {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }
}

class CellHeights
{ 
    public float height00;
    public float height10;
    public float height01;
    public float height11;

    public CellHeights(float h00, float h10, float h01, float h11)
    {
        height00 = h00;
        height10 = h10;
        height01 = h01;
        height11 = h11;
    }
}
