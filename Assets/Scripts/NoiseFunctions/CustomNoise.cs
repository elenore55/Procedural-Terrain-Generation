using UnityEngine;
using System;

public class CustomNoise : GenericNoise
{
    int type;
    Func<float, float, float, float> Callback;

    public CustomNoise(int t) { type = t; }

    public override float Generate(float x, float y)
    {
        int iX = (int)x;
        int iY = (int)y;
        float u = x - iX;
        float v = y - iY;

        Vector2Int iV = new Vector2Int(iX, iY);

        switch (type)
        {
            case (int)InterpIndices.Cosine:
                Callback = Cosine;
                break;
            case (int)InterpIndices.Acceleration:
                Callback = Acceleration;
                break;
            case (int)InterpIndices.Linear:
                Callback = Linear;
                break;
        }

        if (type < (int)InterpIndices.Quadratic)
        {
            float h1 = MyRandom(iV);
            float h2 = MyRandom(iV + new Vector2(1, 0));
            float h3 = MyRandom(iV + new Vector2(0, 1));
            float h4 = MyRandom(iV + new Vector2(1, 1));

            float x1 = Callback(h1, h2, u);
            float x2 = Callback(h3, h4, u);
            float result = Callback(x1, x2, v);
            return result;
        }
        else if (type == (int)InterpIndices.Quadratic)
        {
            float h1 = MyRandom(iV + new Vector2(-1, -1));
            float h2 = MyRandom(iV + new Vector2(0, -1));
            float h3 = MyRandom(iV + new Vector2(1, -1));

            float h4 = MyRandom(iV + new Vector2(-1, 0));
            float h5 = MyRandom(iV);
            float h6 = MyRandom(iV + new Vector2(1, 0));

            float h7 = MyRandom(iV + new Vector2(-1, 1));
            float h8 = MyRandom(iV + new Vector2(0, 1));
            float h9 = MyRandom(iV + new Vector2(1, 1));

            float x1 = Polynomial(h1, h2, h3, u);
            float x2 = Polynomial(h4, h5, h6, u);
            float x3 = Polynomial(h7, h8, h9, u);

            return Polynomial(x1, x2, x3, v);
        }

        else
        {
            float h1 = MyRandom(iV + new Vector2(-1, -1));
            float h2 = MyRandom(iV + new Vector2(0, -1));
            float h3 = MyRandom(iV + new Vector2(1, -1));
            float h4 = MyRandom(iV + new Vector2(2, -1));

            float h5 = MyRandom(iV + new Vector2(-1, 0));
            float h6 = MyRandom(iV);
            float h7 = MyRandom(iV + new Vector2(1, 0));
            float h8 = MyRandom(iV + new Vector2(2, 0));

            float h9 = MyRandom(iV + new Vector2(-1, 1));
            float h10 = MyRandom(iV + new Vector2(0, 1));
            float h11 = MyRandom(iV + new Vector2(1, 1));
            float h12 = MyRandom(iV + new Vector2(2, 1));

            float h13 = MyRandom(iV + new Vector2(-1, 2));
            float h14 = MyRandom(iV + new Vector2(0, 2));
            float h15 = MyRandom(iV + new Vector2(1, 2));
            float h16 = MyRandom(iV + new Vector2(2, 2));

            float x1 = Polynomial(h1, h2, h3, h4, u);
            float x2 = Polynomial(h5, h6, h7, h8, u);
            float x3 = Polynomial(h9, h10, h11, h12, u);
            float x4 = Polynomial(h13, h14, h15, h16, u);

            return Polynomial(x1, x2, x3, x4, v);
        }
    }

    float MyRandom(Vector2 v)
    {
        Vector2 other = new Vector2(22.9898f, 78.233f);
        return Fract(Mathf.Sin(Dot(v, other)) * 437.5453123f);
    }

    float Fract(float x)
    {
        return x - (int)x;
    }

    float Dot(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.x + v1.y * v2.y;
    }

    float Linear(float x, float y, float t)
    {
        return x + t * (y - x);
    }

    float Cosine(float x, float y, float t)
    {
        return Linear(x, y, (float)(-Mathf.Cos(Mathf.PI * t) / 2f + 0.5));
    }

    float Acceleration(float x, float y, float t)
    {
        return Linear(x, y, t * t);
    }

    float Polynomial(float h1, float h2, float h3, float t)
    {
        float a = t * (t - 1) / 2;
        float b = (t + 1) * (1 - t);
        float c = t * (t + 1) / 2;
        return a * h1 + b * h2 + c * h3;
    }

    float Polynomial(float h1, float h2, float h3, float h4, float t)
    {
        float a = t * (t - 1) * (2 - t) / 6;
        float b = (t + 1) * (t - 1) * (t - 2) / 2;
        float c = t * (t + 1) * (2 - t) / 2;
        float d = t * (t - 1) * (t + 1) / 6;
        return a * h1 + b * h2 + c * h3 + d * h4;
    }
}
