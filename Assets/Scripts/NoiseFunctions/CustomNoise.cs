using UnityEngine;

public class CustomNoise : GenericNoise
{
    public override float Generate(float x, float y)
    {
        float ix = (int)x;
        float fx = x - ix;
        float iy = (int)y;
        float fy = y - iy;
  
        Vector2 iV = new Vector2(ix, iy);
        Vector2 fV = new Vector2(fx, fy);

        float a = myRandom(iV);
        float b = myRandom(iV + new Vector2(1, 0));
        float c = myRandom(iV + new Vector2(0, 1));
        float d = myRandom(iV + new Vector2(1, 1));

        float ix0 = Acceleration(a, b, fx);
        float ix1 = Acceleration(c, d, fx);
        float result = Acceleration(ix0, ix1, fy);
        return result; 
    }

    private float Rand(float p)
    {
        p = p * 0.011f - (int)p;
        p *= p + 7.5f;
        p *= p + p;
        return p - (int)p;
    }

    private float Linear(float x, float y, float t)
    {
        return x + t * (y - x);
    }

    private float SmoothStep(float x, float y, float t)
    {
        return Linear(x, y, t * t * (3 - 2 * t));
    }

    private float Step(float x, float y, float t)
    {
        if (t < 0.5f) return x;
        return y;
    }

    private float Cosine(float x, float y, float t)
    {
        return Linear(x, y, (float)(-Mathf.Cos(Mathf.PI * t) / 2f + 0.5));
    }

    private float Acceleration(float x, float y, float t)
    {
        return Linear(x, y, t * t);
    }

    private float Deceleration(float x, float y, float t)
    {
        return Linear(x, y, 1 - (1 - t) * (1 - t));
    }

    float clamp(float x, float lowerlimit, float upperlimit)
    {
        if (x < lowerlimit)
            x = lowerlimit;
        if (x > upperlimit)
            x = upperlimit;
        return x;
    }

    float myRandom(Vector2 v)
    {
        Vector2 other = new Vector2(12.9898f, 78.233f);
        return fract(Mathf.Sin(Dot(v, other)) * 43758.5453123f);
    }

    float fract(float x)
    {
        return x - (int)x;
    }

    float Dot(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.x + v1.y * v2.y;
    }
}
