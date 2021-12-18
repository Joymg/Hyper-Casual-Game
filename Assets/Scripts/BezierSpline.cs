using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierSpline : MonoBehaviour
{
    
    /// <summary>
    /// Array of points forming the spline
    /// </summary>
    [SerializeField]
    private Vector3[] points;

    //Adding indirect acces to points, cause we want to set the same velocity between curves
    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    public void SetControlPoint(int index, Vector3 point)
    {
        //Control points move allong with middle points
        if (index % 3 == 0)
        {
            Vector3 delta = point - points[index];
            if (loop)
            {
                //first point
                if (index == 0)
                {
                    //next point gets displaced
                    points[1] += delta;
                    //last point's prevoius point gets displaced
                    points[points.Length - 2] += delta;
                    //last point = first point
                    points[points.Length - 1] = point;
                }
                //last point
                else if (index == points.Length -1)
                {
                    //first point = last point
                    points[0] = point;
                    //first point's next point gets displaced
                    points[1] += delta;
                    //previous points gets displaced
                    points[index - 1] += delta;
                }
                else
                {
                    //previous and next points get displaced
                    points[index - 1] += delta;
                    points[index + 1] += delta;

                }
            }
            else
            {
                if (index > 0)
                {
                    points[index - 1] += delta;
                }
                if (index + 1 < points.Length)
                {
                    points[index + 1] += delta;
                }
            }
        }

        points[index] = point;
        EnforceMode(index);
    }



    /// <summary>
    /// Number of control points
    /// </summary>
    public int ControlPointCount => points.Length;

    /// <summary>
    /// Number of curves forming the spline
    /// </summary>
    public int CurveCount { get { return (points.Length - 1) / 3; } }


    [SerializeField]
    private bool loop;
    public bool Loop { 
        get => loop; 
        set { 
            loop = value;
            if (value == true)
            {
                //forcing the first and last point of the spline to share the same mode
                modes[modes.Length - 1] = modes[0];
                SetControlPoint(0, points[0]);
            }
        } 
    }

    [SerializeField]
    private BezierControlPointMode[] modes;

    public BezierControlPointMode GetControlPointMode(int index)
    {
        return modes[(index + 1) / 3];
    }

    public void SetControlPointMode (int index, BezierControlPointMode mode)
    {
        int modeIndex = (index + 1) / 3;
        modes[modeIndex] = mode;
        //makinf sure in case of a Loop the first and last noide have the same mode
        if (loop)
        {
            if (modeIndex == 0)
            {
                modes[modes.Length - 1] = mode;
            }
            else if (modeIndex == modes.Length -1)
            {
                modes[0] = mode;
            }
        }
        EnforceMode(index);
    }


    private void EnforceMode(int index)
    {
        int modeIndex = (index + 1) / 3;

        //check if is not necessary to force anything
        BezierControlPointMode mode = modes[modeIndex];

        if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length -1))
        {
            return;
        }
        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        //if middle point is selected
        if (index <= middleIndex)
        {
            //previous point is fixed
            fixedIndex = middleIndex - 1;
            //check if fixed point wraps around the array
            if (fixedIndex < 0)
            {
                fixedIndex = points.Length - 2;
            }

            //next point is enforced
            enforcedIndex = middleIndex + 1;
            //check if enforced point wraps around the array
            if (enforcedIndex >= points.Length)
            {
                enforcedIndex = 1;
            }
        }
        //if other point is selected
        else
        {
            //that one is fixed
            fixedIndex = middleIndex + 1;
            //check if fixed point wraps around the array
            if (fixedIndex >= points.Length)
            {
                fixedIndex = 1;
            }
            //adjust opposite
            enforcedIndex = middleIndex - 1;
            //check if fixed point wraps around the array
            if (enforcedIndex < 0)
            {
                enforcedIndex = points.Length - 2;
            }
        }

        //MIRRORED CASE
        //get mirror axis point
        Vector3 axis = points[middleIndex];
        //calculate the vector from middle to fixed point
        Vector3 enforcedTangent = axis - points[fixedIndex];

        if (mode == BezierControlPointMode.Aligned)
        {
            //for the aligned case its necessary to check that the new Tanget has the same lenght as the old onw
            enforcedTangent = enforcedTangent.normalized * Vector3.Distance(axis, points[enforcedIndex]);
        }

        //enforced position is axis position + vector
        points[enforcedIndex] = axis + enforcedTangent;
    }

    //Initializes points positions
    public void Reset()
    {
        points = new Vector3[] {
            new Vector3(1f,0f,0f),
            new Vector3(2f,0f,0f),
            new Vector3(3f,0f,0f),
            new Vector3(4f,0f,0f),
        };

        //Storing the mode per curve
        modes = new BezierControlPointMode[] {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
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

        //when adding a curve a single curve is added
        Array.Resize(ref modes, modes.Length + 1);
        //with the mode of the previous one
        modes[modes.Length - 1] = modes[modes.Length - 2];

        //constraints are enforced when a curve is added
        EnforceMode(points.Length - 4);

        if (loop)
        {
            points[points.Length - 1] = points[0];
            modes[modes.Length - 1] = modes[0];
            EnforceMode(0);
        }
    }
}
