using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    public ParticleSystem rainSystem;
    public List<ParticleCollisionEvent> collisionEvents;
    ParticleSystem.Particle[] droplets;

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = rainSystem.GetCollisionEvents(other, collisionEvents);
        int numParticlesAlive = rainSystem.GetParticles(droplets);
        int sz = InfiniteTerrain.currentTile.GetLength(0);
        // Debug.Log(sz);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            
            Vector3 pos = collisionEvents[i].intersection;
            int x = (int)pos.x % sz;
            int y = (int)pos.y % sz;
            if (x < 0) x += sz;
            if (y < 0) y += sz;

            // Debug.Log(x + " " + y);
            InfiniteTerrain.currentTile[x, y] -= 4;

            //for (int k = 0; k < 5; k++)
            //{
            //    for (int m = 70; m < 200; m += 10)
            //    {
            //        for (int j = 50; j < 220; j += 15)
            //            InfiniteTerrain.currentTile[m, j] -= 0.5f;
            //    }
            //}
        }
    }

    void Start()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        if (droplets == null || droplets.Length < rainSystem.main.maxParticles)
            droplets = new ParticleSystem.Particle[rainSystem.main.maxParticles];
    }
}
