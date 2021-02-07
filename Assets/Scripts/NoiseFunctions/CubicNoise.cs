using UnityEngine;

public class CubicNoise : GenericNoise
{
    private static int RND_A = 134775813;
    private static int RND_B = 1103515245;

    public override float Generate(float x, float y)
    {
        int ix = (int)x;
        float fx = Fract(x);
        int iy = (int)y;
        float fy = Fract(y);

        

        float[] xSamples = new float[4];
        for (int i = 0; i < 4; i++)
        {
            Vector2 iV = new Vector2(ix * i, iy * i);
            float a = MyRandom(iV);
            float b = MyRandom(iV + new Vector2(1, 0));
            float c = MyRandom(iV + new Vector2(0, 1));
            float d = MyRandom(iV + new Vector2(1, 1));
            xSamples[i] = CubicInterp(a, b, c, d, fx);
        }
        return CubicInterp(xSamples[0], xSamples[1], xSamples[2], xSamples[3], fy) * 0.666666f + 0.166666f;
    }

    float MyRandom(int x, int y)
    {
        return (float)((((x ^ y) * RND_A) ^ x) * (((RND_B * x) << 16) ^ (RND_B * y) - RND_A)) / int.MaxValue;
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

    float CubicInterp(float a, float b, float c, float d, float t)
    {
        // x(x(x(−a+b−c+d)+2a−2b+c−d)−a+c)+b
        return t * (t * (t * (-a + b - c + d) + 2 * a - 2 * b + c - d) - a + c) + b;
    }
}
