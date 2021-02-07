using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    public ParticleSystem rainSystem;
    public List<ParticleCollisionEvent> collisionEvents;
    ParticleSystem.Particle[] droplets;

    const int RADIUS = 3;
    const float INERTIA = 0.5f;
    const float WATER_CAPACITY = 3;
    const float SEDIMENT_CAPACITY = 4;
    const float MIN_SEDIMENT_CAPACITY = 0.1f;
    const float GRAVITY = 4;
    const float INITIAL_SPEED = 1;
    const float EROSION_SPEED = 0.3f;
    const float DEPOSIT_SPEED = 0.3f;
    const float EVAPORATION_SPEED = 0.01f;
    const int MAX_ITERS = 20;

    int[][] erosionBrushIndices;
    float[][] erosionBrushWeights;

    int currentErosionRadius;
    int currentMapSize;


    void Initialize(int mapSize)
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
        int numParticlesAlive = rainSystem.GetParticles(droplets);
        int sz = InfiniteTerrain.tileSize;
        float deltaHeight = 0.01f;
        Initialize(sz);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            
            Vector3 pos = collisionEvents[i].intersection;
            int x = (int)pos.x % sz;
            int y = (int)pos.y % sz;
            if (x < 0) x += sz;
            if (y < 0) y += sz;
            x = sz - x;
            y = sz - y;

            if (x < 1 || x > sz - 2 || y < 1 || y > sz - 2) continue;

            float u = Fract(pos.x);
            float v = Fract(pos.y);
            Vector2 dropletPos = new Vector2(x + u, y + v);
            Vector2 dir = new Vector2(0, 0);

            // float dirX = 0;
            // float dirY = 0;

            float water = WATER_CAPACITY;
            float speed = INITIAL_SPEED;
            float sediment = 0;

            for (int j = 0; j < MAX_ITERS; j++)
            {
                int nodeX = (int)dropletPos.x;
                int nodeY = (int)dropletPos.y;
                int dropletIndex = nodeY * sz + nodeX;

                float cellOffsetX = dropletPos.x - nodeX;  // u
                float cellOffsetY = dropletPos.y - nodeY;  // v

                HeightAndGrad heightAndGrad = CalculateHeightAndGrad(InfiniteTerrain.currentMap, sz, 
                                                                         dropletPos.x, dropletPos.y);

                dir.x = (dir.x * INERTIA - heightAndGrad.gradX * (1 - INERTIA));
                dir.y = (dir.y * INERTIA - heightAndGrad.gradY * (1 - INERTIA));

                float len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y);
                if (len != 0)
                {
                    dir.x /= len;
                    dir.y /= len;
                }
                dropletPos.x += dir.x;
                dropletPos.y += dir.y;

                if (dropletPos.x <= 0 || dropletPos.x >= sz - 1 || dropletPos.y <= 0 || dropletPos.y >= sz - 1)
                    break;

                float newHeight = CalculateHeightAndGrad(InfiniteTerrain.currentMap, sz, dropletPos.x, dropletPos.y).height;
                deltaHeight = newHeight - heightAndGrad.height;

                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * SEDIMENT_CAPACITY, MIN_SEDIMENT_CAPACITY);
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * DEPOSIT_SPEED;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    InfiniteTerrain.currentMap[nodeX, nodeY] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    InfiniteTerrain.currentMap[nodeX + 1, nodeY] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    InfiniteTerrain.currentMap[nodeX, nodeY + 1] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    InfiniteTerrain.currentMap[nodeX + 1, nodeY + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

                }
                else
                {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float h = InfiniteTerrain.currentMap[nodeX, nodeY];
                    float w = ErosionWeight(h);
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * EROSION_SPEED, -deltaHeight);

                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++)
                    {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                        int X = nodeIndex % sz;
                        int Y = nodeIndex / sz;
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (InfiniteTerrain.currentMap[X, Y] < weighedErodeAmount) ? InfiniteTerrain.currentMap[X, Y] : weighedErodeAmount;
                        InfiniteTerrain.currentMap[X, Y] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }
            }
            speed = Mathf.Sqrt(speed * speed + deltaHeight * GRAVITY);
            water *= (1 - EVAPORATION_SPEED);
        }
    }

    void Start()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        if (droplets == null || droplets.Length < rainSystem.main.maxParticles)
            droplets = new ParticleSystem.Particle[rainSystem.main.maxParticles];
    }

    float Fract(float number)
    {
        return Mathf.Abs(number - (int)number);
    }

    float ErosionWeight(float height)
    {
        return Mathf.Lerp(0, 0.5f, 1 / height);
    }

    HeightAndGrad CalculateHeightAndGrad(float[,] nodes, int mapSize, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float u = posX - coordX;
        float v = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[coordX, coordY];
        float heightNE = nodes[coordX + 1, coordY];
        float heightSW = nodes[coordX, coordY + 1];
        float heightSE = nodes[coordX + 1, coordY + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - v) + (heightSE - heightSW) * v;
        float gradientY = (heightSW - heightNW) * (1 - u) + (heightSE - heightNE) * u;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - u) * (1 - v) + heightNE * u * (1 - v) + heightSW * (1 - u) * v + heightSE * u * v;

        return new HeightAndGrad() { height = height, gradX = gradientX, gradY = gradientY };
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

class HeightAndGrad
{
    public float height;
    public float gradX;
    public float gradY;
}
