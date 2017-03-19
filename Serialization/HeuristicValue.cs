using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeuristicValue
{
    //Actual points
    Vector3 a, b;
    public Vector3 A
    {
        get { return a; }
        set
        {
            a = value;
            Vector2 g = CalculateGradients(a, b);
            zx = g.x;
            yx = g.y;
        }
    }

    public Vector3 B
    {
        get { return b; }
        set
        {
            b = value;
            Vector2 g = CalculateGradients(a, b);
            zx = g.x;
            yx = g.y;
        }
    }

    //Gradient between points
    float zx, yx;
    public float ZX { get; }
    public float YX { get; }

    public HeuristicValue()
    {
        a = b = new Vector3(0, 0, 0);
        zx = yx = 0.0f;
    }

    public HeuristicValue(Vector3 _a, Vector3 _b)
    {
        a = _a;
        b = _b;
        Vector2 g = CalculateGradients(a, b);
        zx = g.x;
        yx = g.y;
    }

    /// <summary>
    /// Calculates the Z and Y planar gradients given 2 points.
    /// X component is the z/x gradient (Z plane)
    /// Y component is the y/x gradient (Y plane)
    /// </summary>
    public static Vector2 CalculateGradients(Vector3 a, Vector3 b)
    {
        Vector2 result = new Vector2();
        float bax = b.x - a.x;
        result.x = (b.z - a.z) / bax;
        result.y = (b.y - a.y) / bax;
        return result;
    }
}
