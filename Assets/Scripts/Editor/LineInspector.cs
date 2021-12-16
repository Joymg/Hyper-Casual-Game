using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Line))]
public class LineInspector : Editor
{
    private void OnSceneGUI()
    {
        Line line = target as Line;

        //converting points into world coordinates
        Transform handleTransform = line.transform;
        Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation: Quaternion.identity;
        Vector3 p0 = handleTransform.TransformPoint(line.p0);
        Vector3 p1 = handleTransform.TransformPoint(line.p1);

        Handles.color = Color.white;
        Handles.DrawLine(p0, p1);


        //show position handles of line's points and making it functional
        EditorGUI.BeginChangeCheck();
        p0 = Handles.DoPositionHandle(p0, handleRotation);
        if (EditorGUI.EndChangeCheck())
        {
            // undo points drag operation is possible
            Undo.RecordObject(line, "Move Point p0");
            //ask to save after moving a point
            EditorUtility.SetDirty(line);

            //back to local coordiantes
            line.p0 = handleTransform.InverseTransformPoint(p0);
        }
        EditorGUI.BeginChangeCheck();
        p1 = Handles.DoPositionHandle(p1, handleRotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(line, "Move Point p0");
            EditorUtility.SetDirty(line);

            //back to local coordiantes
            line.p1 = handleTransform.InverseTransformPoint(p1);
        }
    }
}
