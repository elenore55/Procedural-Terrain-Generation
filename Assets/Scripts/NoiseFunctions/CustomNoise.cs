using UnityEngine;

public static class CustomNoise
{
    public static float Generate(float x, float y)
    {
        float result = 0f;
        System.Random rnd;
        int a = (int)x;
        int c = (int)y;
        rnd = new System.Random(a + c * 45);
        float ra = rnd.Next();
        float rb = rnd.Next();
        float rc = rnd.Next();
        float rd = rnd.Next();
        float u = x - (float)a;
        float v = y - (float)c;
        float ix0 = Cosine(ra, rb, u);
        float ix1 = Cosine(rc, rd, u);
        result = Cosine(ix0, ix1, v);
        return result;
    }

    // float hash(float p) { p = fract(p * 0.011); p *= p + 7.5; p *= p + p; return fract(p); }
    private static float Rand(float p)
    {
        p = p * 0.011f - (int)p;
        p *= p + 7.5f;
        p *= p + p;
        return p - (int)p;
    }

    private static float Interp(float x, float y, float t)
    {
        return x * (1 - t) + y * t;
    }

    private static float Interp1(float y1, float y2, float p)
    {
        float n = y1;
        float k = y2 - y1;
        return k * p + n;
    }

    private static float SmoothStep(float y1, float y2, float p)
    {
        return Interp1(y1, y2, p * p * (3 - 2 * p));
    }

    private static float Cosine(float y1, float y2, float p)
    {
        return Interp1(y1, y2, (float)(-Mathf.Cos(Mathf.PI * p) / 2f + 0.5));
    }

    private static float Acceleration(float y1, float y2, float p)
    {
        return Interp1(y1, y2, p * p);
    }
}
