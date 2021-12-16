using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier 
{
    public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2,float t)
    {
        //Lerping from 1st to 2nd and form 2nd to 3rd
        //return Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t);
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0 + 2 * oneMinusT * t * p1 + t * t * p2;
    }

    /// <summary>
    /// Calculates lines tangent to the cure, which can be interpreted as the speed we move along the curve
    /// </summary>
    public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return 2 * (1f - t) * (p1 - p0) + 2 * t *(p2 - p1);
    }
}
