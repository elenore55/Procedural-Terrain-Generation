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
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;
        }
    }

    void Start()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        if (droplets == null || droplets.Length < rainSystem.main.maxParticles)
            droplets = new ParticleSystem.Particle[rainSystem.main.maxParticles];
    }

    void Update()
    {
        
    }
}
