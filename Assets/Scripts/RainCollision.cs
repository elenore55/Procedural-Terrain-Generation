using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{
    public ParticleSystem rainSystem;
    public List<ParticleCollisionEvent> collisionEvents;

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = rainSystem.GetCollisionEvents(other, collisionEvents);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;
        }
    }

    void Start()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void Update()
    {
        
    }
}
