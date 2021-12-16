using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierCurve))]
public class BezierCurveInspector : Editor
{
    private BezierCurve curve;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const int lineSteps = 10;

    private void OnSceneGUI()
    {
        curve = target as BezierCurve;
        handleTransform = curve.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        Vector3 p1 = ShowPoint(1);
        Vector3 p2 = ShowPoint(2);

        Handles.color = Color.grey;
        Handles.DrawLine(p0, p1);
        Handles.DrawLine(p1, p2);

        Handles.color = Color.yellow;
        Vector3 lineStart = curve.GetPoint(0f);
        for (int i = 1; i < lineSteps; i++)
        {
            Vector3 lineEnd = curve.GetPoint(i / (float)lineSteps);
            Handles.DrawLine(lineStart, lineEnd);
            lineStart = lineEnd;
        }
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = handleTransform.InverseTransformPoint(curve.points[index]);
        EditorGUI.BeginChangeCheck();
        point = Handles.DoPositionHandle(point, handleRotation);
        if (EditorGUI.EndChangeCheck())
        {
            // undo points drag operation is possible
            Undo.RecordObject(curve, "Move Point p0");
            //ask to save after moving a point
            EditorUtility.SetDirty(curve);

            //back to local coordiantes
            curve.points[index] = handleTransform.InverseTransformPoint(point);
        }
        return point;
    }
}
