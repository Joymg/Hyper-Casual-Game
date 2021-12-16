using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierSpline : MonoBehaviour
{
    
    /// <summary>
    /// Array of points forming the spline
    /// </summary>
    public Vector3[] points;

    /// <summary>
    /// Number of curves forming the spline
    /// </summary>
    public int CurveCount { get { return (points.Length - 1) / 3; } }

    //Initializes points positions
    public void Reset()
    {
        points = new Vector3[] {
            new Vector3(1f,0f,0f),
            new Vector3(2f,0f,0f),
            new Vector3(3f,0f,0f),
            new Vector3(4f,0f,0f),
        };
    }

    public Vector3 GetPoint(float t)
    {
        int i;
        //if t is at the end of the spline
        if (t >=1f)
        {
            t = 1f; //t is at the end
            i = points.Length - 4; //index is the first point of last spline's curve
        }
        // if it is somewhere in the middle
        else
        {
            //calculate the fractional part
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int) t;
            t -= i;
            //index is the first point of the current curve
            i *= 3;
        }
        return transform.TransformPoint(Bezier.GetPoint(points[i], points[i+1], points[i+2], points[i + 3], t));
    }

    public Vector3 GetVelocity(float t)
    {
        int i;
        //if t is at the end of the spline
        if (t >= 1f)
        {
            t = 1f; //t is at the end
            i = points.Length - 4; //index is the first point of last spline's curve
        }
        // if it is somewhere in the middle
        else
        {
            //calculate the fractional part
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            //index is the first point of the current curve
            i *= 3;
        }
        return transform.TransformPoint(
            Bezier.GetFirstDerivative(
                points[i], points[i + 1], points[i + 2], points[i + 3], t) - transform.position);
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    public void AddCurve()
    {
        //last curve's point is the first of the new curve
        Vector3 lastPoint = points[points.Length - 1];

        //Increase array size to allow for 3 new points, for a total of 4 with the last of the previous curve
        Array.Resize(ref points, points.Length + 3);
        lastPoint.x += 1f;
        points[points.Length - 3] = lastPoint;
        lastPoint.x += 1f;
        points[points.Length - 2] = lastPoint;
        lastPoint.x += 1f;
        points[points.Length - 1] = lastPoint;
    }
}
