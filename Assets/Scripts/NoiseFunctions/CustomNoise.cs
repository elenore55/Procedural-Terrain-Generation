using UnityEngine;
using System;

public class CustomNoise : GenericNoise
{
    public Func<float, float, float, float> Callback;

    public override float Generate(float x, float y)
    {
        if (Callback == null)
            Callback = Cosine;
        float ix = (int)x;
        float fx = Fract(x);
        float iy = (int)y;
        float fy = Fract(y);
  
        Vector2 iV = new Vector2(ix, iy);

        float a = MyRandom(iV);
        float b = MyRandom(iV + new Vector2(1, 0));
        float c = MyRandom(iV + new Vector2(0, 1));
        float d = MyRandom(iV + new Vector2(1, 1));

        float x1 = Callback(a, b, fx);
        float x2 = Callback(c, d, fx);
        float result = Callback(x1, x2, fy);
        return result; 
    }

    public float Linear(float x, float y, float t)
    {
        return x + t * (y - x);
    }

    public float SmoothStep(float x, float y, float t)
    {
        float m = t * t * (3 - 2 * t);
        return Linear(x, y, m);
    }

    public float Cosine(float x, float y, float t)
    {
        return Linear(x, y, (float)(-Mathf.Cos(Mathf.PI * t) / 2f + 0.5));
    }

    public float Acceleration(float x, float y, float t)
    {
        return Linear(x, y, t * t);
    }

    public float Deceleration(float x, float y, float t)
    {
        return Linear(x, y, 1 - (1 - t) * (1 - t));
    }

    float MyRandom(Vector2 v)
    {
        Vector2 other = new Vector2(12.9898f, 78.233f);
        return Fract(Mathf.Sin(Dot(v, other)) * 43758.5453123f);
    }

    float Fract(float x)
    {
        return x - (int)x;
    }

    float Dot(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.x + v1.y * v2.y;
    }
}
