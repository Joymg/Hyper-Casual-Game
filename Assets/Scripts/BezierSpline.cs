using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BezierSpline : MonoBehaviour
{
    
    /// <summary>
    /// Array of points forming the spline
    /// </summary>
    [SerializeField]
    private List<Vector3> points;

    public Vector3 this[int i]
    {
        get
        {
            return points[i];
        }
    }

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
                points[LoopIndex(index - 1)] += delta;
                points[LoopIndex(index + 1)] += delta;
                /*//first point
                if (index == 0)
                {
                    //next point gets displaced
                    points[1] += delta;
                    //last point's prevoius point gets displaced
                    //points[points.Count - 2] += delta;
                    //last point = first point
                    points[points.Count - 1] += delta;
                }
                //last point
                *//*else if (index == points.Count - 1)
                {
                    //first point = last point
                    points[0] = point;
                    //first point's next point gets displaced
                    points[1] += delta;
                    //previous points gets displaced
                    points[index - 1] += delta;
                }*//*
                else
                {
                    //previous and next points get displaced
                    points[index - 1] += delta;
                    points[index + 1] += delta;

                }*/

            }

            //Moving control points when moving anchorPoints
            else
            {

                if (index > 0)
                {
                    points[index - 1] += delta;
                }
                if (index + 1 < points.Count)
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
    public int PointCount => points.Count;

    /// <summary>
    /// Number of curves forming the spline
    /// </summary>
    public int SegmentCount 
    { get 
        {
            //return (points.Count - 1) / 3; 
            return points.Count / 3;
        }
    }

    public Vector3[] GetPointsInSegment(int i)
    {
        return new Vector3[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
    }


    [SerializeField]
    private bool loop;
    public bool Loop { 
        get => loop; 
        set { 
            loop = value;

            if (loop)
            {
                points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                points.Add(points[0] * 2 - points[1]);

                modes[modes.Count - 1] = modes[0];

                SetControlPoint(0, points[0]);

                if (autoSetControlPoints)
                {
                    AutoSetAnchorControlPoints(0);
                    AutoSetAnchorControlPoints(points.Count - 3);
                }
            }
            else
            {
                points.RemoveRange(points.Count - 2, 2);
                if (autoSetControlPoints)
                {
                    AutoSetStartAndEndControls();
                }
            }
        } 
    }

    [SerializeField]
    private bool autoSetControlPoints;
    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if (autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if (autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    [SerializeField]
    private List<BezierControlPointMode> modes;

    public BezierControlPointMode GetControlPointMode(int index)
    {
        return modes[LoopIndex(index +1) / 3];
    }

    public void SetControlPointMode (int index, BezierControlPointMode mode)
    {
        int modeIndex = LoopIndex(index + 1) / 3;
        modes[modeIndex] = mode;
        //making sure in case of a Loop the first and last noide have the same mode
        if (loop)
        {
            if (modeIndex == 0)
            {
                modes[modes.Count - 1] = mode;
            }
            else if (modeIndex == modes.Count - 1)
            {
                modes[0] = mode;
            }
        }
        EnforceMode(index);
    }


    private void EnforceMode(int index)
    {
        //take point mode taking care if the spline is a loop
        int modeIndex = LoopIndex((index + 1)) / 3;

        //check if is not necessary to force anything
        BezierControlPointMode mode = modes[modeIndex];

        if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Count - 1))
        {
            return;
        }

        int anchorIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        //if the control point previous to the anchor point is Selected or is the last point 
        if (index <= anchorIndex || index == PointCount-1)
        {
            //previous point is fixed
            fixedIndex = anchorIndex - 1;
            //check if fixed point wraps around the array
            if (fixedIndex < 0)
            {
                fixedIndex = points.Count - 1;
            }

            //next point is enforced
            enforcedIndex = anchorIndex + 1;
            //check if enforced point wraps around the array
            if (enforcedIndex >= points.Count)
            {
                enforcedIndex = 1;
            }
        }
        //if the control point next to the anchor point is Selected
        else
        {
            //that one is fixed
            fixedIndex = anchorIndex + 1;
            //check if fixed point wraps around the array
            if (fixedIndex >= points.Count)
            {
                fixedIndex = 1;
            }
            //adjust opposite
            enforcedIndex = anchorIndex - 1;
            //check if fixed point wraps around the array
            if (enforcedIndex < 0)
            {
                enforcedIndex = points.Count - 1;
            }
        }

        //MIRRORED CASE
        //get mirror axis point
        Vector3 axis = points[anchorIndex];
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
        points.Clear();
        points.Add(new Vector3(1f, 0f, 0f));
        points.Add(new Vector3(2f, 0f, 0f));
        points.Add(new Vector3(3f, 0f, 0f));
        points.Add(new Vector3(4f, 0f, 0f));
        /*points = new Vector3[] {
            new Vector3(1f,0f,0f),
            new Vector3(2f,0f,0f),
            new Vector3(3f,0f,0f),
            new Vector3(4f,0f,0f),
        };*/

        modes.Clear();
        modes.Add(BezierControlPointMode.Mirrored);
        modes.Add(BezierControlPointMode.Mirrored);

        //Storing the mode per curve
        /*modes = new BezierControlPointMode[] {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
        };*/
    }

    public Vector3 GetPoint(float t)
    {
        int i;
        //if t is at the end of the spline
        if (t >=1f)
        {
            t = 1f; //t is at the end
            //index is the first point of last spline's curve
            i = loop ?  PointCount - 3 : PointCount -4 ;
        }
        // if it is somewhere in the middle
        else
        {
            //calculate the fractional part
            t = Mathf.Clamp01(t) * SegmentCount;
            i = (int) t;
            t -= i;
            //index is the first point of the current curve
            i *= 3;
        }

        return transform.TransformPoint(Bezier.GetPoint(points[i], points[LoopIndex(i+1)], points[LoopIndex(i + 2)], points[LoopIndex(i + 3)], t));
        
    }

    public Vector3 GetVelocity(float t)
    {
        int i;
        //if t is at the end of the spline
        if (t >= 1f)
        {
            t = 1f; //t is at the end
            //index is the first point of last spline's curve
            i = loop ? PointCount - 3 : PointCount - 4;
        }
        // if it is somewhere in the middle
        else
        {
            //calculate the fractional part
            t = Mathf.Clamp01(t) * SegmentCount;
            i = (int)t;
            t -= i;
            //index is the first point of the current curve
            i *= 3;
        }
        return transform.TransformPoint(
            Bezier.GetFirstDerivative(
                points[i], points[i + 1], points[i + 2], points[LoopIndex(i + 3)], t) - transform.position);
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    public void AddSegment()
    {
        //last curve's point is the first of the new curve
        Vector3 lastPoint = points[PointCount - 1];

        lastPoint.x += 1f;
        points.Add(lastPoint);
        lastPoint.x += 1f;
        points.Add(lastPoint);
        lastPoint.x += 1f;
        points.Add(lastPoint);

        modes.Add(modes[modes.Count-1]);

        //constraints are enforced when a curve is added
        EnforceMode(points.Count - 4);

        if (loop)
        {
            points[points.Count - 1] = points[0];
            modes[modes.Count - 1] = modes[0];
            EnforceMode(0);
        }
    }


    public void AddSegment(Vector3 anchorPos)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);

        modes.Add(modes[modes.Count-1]);

        EnforceMode(points.Count - 4);

        if (loop)
        {
            points[points.Count - 1] = points[0];
            modes[modes.Count - 1] = modes[0];
            EnforceMode(0);
        }

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(PointCount - 1);
        }
    }

    public void SplitSegment( Vector3 anchorPos, int segmentIndex)
    {
        //after the first control point of the segment
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
        modes.InsertRange(segmentIndex, new BezierControlPointMode[] {modes[modes.Count - 1] });
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 2);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }
    }

    public void DeleteSegment(int anchorIndex)
    {
        if (SegmentCount > 2 || !loop && SegmentCount >1)
        {
            if (anchorIndex == 0)
            {
                if (loop)
                {
                    points[PointCount - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == PointCount -1 && !loop)
            {
                points.RemoveRange(0, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex-1,3);
            }
        }
    }

    public void MovePoint(int i, Vector3 pos)
    {
        Vector3 deltaMove = pos - points[i];

        if (i % 3 == 0 || !autoSetControlPoints)
        {
            points[i] = pos;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }
            else
            {

                if (i % 3 == 0)
                {
                    if (i + 1 < points.Count || loop)
                    {
                        points[LoopIndex(i + 1)] += deltaMove;
                    }
                    if (i - 1 >= 0 || loop)
                    {
                        points[LoopIndex(i - 1)] += deltaMove;
                    }
                }
                else
                {
                    bool nextPointIsAnchor = (i + 1) % 3 == 0;
                    int correspondingControlIndex = (nextPointIsAnchor) ? i + 2 : i - 2;
                    int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;

                    if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || loop)
                    {
                        float dst = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
                        Vector3 dir = (points[LoopIndex(anchorIndex)] - pos).normalized;
                        points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + dir * dst;
                    }
                }
            }
        }
    }

    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < PointCount || loop)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();
    }

    private void AutoSetAllControlPoints()
    {
        for (int i = 0; i < PointCount; i+=3)
        {
            AutoSetAnchorControlPoints(i);
        }
        AutoSetStartAndEndControls();
    }

    void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector3 anchorPos = points[anchorIndex];
        Vector3 dir = Vector3.zero;
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 3 >= 0 || loop)
        {
            Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0 || loop)
        {
            Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || loop)
            {
                points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f;
            }
        }
    }

    void AutoSetStartAndEndControls()
    {
        if (!loop)
        {
            points[1] = (points[0] + points[2]) * .5f;
            points[PointCount - 2] = (points[PointCount-1] + points[PointCount - 3]) * .5f;
        }
    }

    public int LoopIndex(int i)
    {
        return (i + points.Count) % PointCount;
    }


    public float GetApproxLenght(int precision = 8) {

        Vector3[] points = new Vector3[precision];
        for (int i = 0; i < precision; i++)
        {
            float t = i / (precision - 1);
            points[i] = GetPoint(t);
        }

        float dist = 0;
        for (int i = 0; i < precision-1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i+1];
            dist += Vector3.Distance(a, b);
        }
        return dist;
    }
    
}
