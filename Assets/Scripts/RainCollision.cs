using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainCollision : MonoBehaviour
{

    public ParticleSystem rainSystem;

    private void OnParticleCollision(GameObject other)
    {
        Debug.Log("I collided");
    }

    // Start is called before the first frame update
    void Start()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
