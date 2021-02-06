using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    public ParticleSystem rainSystem;
    public List<ParticleCollisionEvent> collisionEvents;
    ParticleSystem.Particle[] droplets;

    const float WATER_THRESHOLD = 0.25f;
    const float INERTIA = 0.5f;
    const float WATER_CAPACITY = 3;
    const float SEDIMENT_CAPACITY = 2;
    const float MIN_SEDIMENT_CAPACITY = 0.1f;
    const float GRAVITY = 2;
    const float INITIAL_SPEED = 1;
    const float EROSION_SPEED = 0.3f;
    const float DEPOSIT_SPEED = 0.3f;
    const int MAX_ITERS = 3;


    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = rainSystem.GetCollisionEvents(other, collisionEvents);
        int numParticlesAlive = rainSystem.GetParticles(droplets);
        int sz = InfiniteTerrain.tileSize;
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

            float dirX = 0;
            float dirY = 0;

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

                dirX = (dirX * INERTIA - heightAndGrad.gradX * (1 - INERTIA));
                dirY = (dirY * INERTIA - heightAndGrad.gradY * (1 - INERTIA));

                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }
                dropletPos.x += dirX;
                dropletPos.y += dirY;

                if (dropletPos.x <= 0 || dropletPos.x >= sz - 1 || dropletPos.y <= 0 || dropletPos.y >= sz - 1)
                    break;

                float newHeight = CalculateHeightAndGrad(InfiniteTerrain.currentMap, sz, dropletPos.x, dropletPos.y).height;
                float deltaHeight = newHeight - heightAndGrad.height;

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
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * EROSION_SPEED, -deltaHeight) * w;

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius

                    // int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                    float deltaSediment = (InfiniteTerrain.currentMap[nodeX, nodeY] < amountToErode) ? InfiniteTerrain.currentMap[nodeX, nodeY] : amountToErode;
                    InfiniteTerrain.currentMap[nodeX, nodeY] -= deltaSediment;
                    sediment += deltaSediment;
                }
            }

            //if (InfiniteTerrain.currentMap[x, y] > WATER_THRESHOLD)
                //InfiniteTerrain.currentMap[x, y] -= 4;
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
}

class HeightAndGrad
{
    public float height;
    public float gradX;
    public float gradY;
}
