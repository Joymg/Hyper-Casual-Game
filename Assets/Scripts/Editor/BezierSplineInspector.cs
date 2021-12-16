using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const int lineSteps = 10;
    private const int curveSteps = 10;
    private const float directionScale =0.5f;

    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private int selectedIndex = -1;

    private void OnSceneGUI()
    {
        spline = target as BezierSpline;
        handleTransform = spline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        for (int i = 1; i < spline.points.Length; i+=3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(i + 2);
       
            Handles.color = Color.grey;
            Handles.DrawLine(p0, p1);
            //Handles.DrawLine(p1, p2);
            Handles.DrawLine(p2, p3);

            Handles.DrawBezier(p0, p3, p1, p2, Color.yellow, null, 2f);
            //sets the first point of the new curve to the last of the previous one
            p0 = p3;
        }


        ShowDirections();
    }

    private void ShowDirections()
    {
        Handles.color = Color.cyan;
        Vector3 point = spline.GetPoint(0f);

        Handles.DrawLine(point, point + spline.GetDirection(0f)*directionScale);

        int steps = curveSteps * spline.CurveCount;
        for (int i = 1; i < steps; i++)
        {
            point = spline.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
        }
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = handleTransform.InverseTransformPoint(spline.points[index]);
        float size = HandleUtility.GetHandleSize(point);
        Handles.color = Color.white;
        if (Handles.Button(point,handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index;
        }
        if (selectedIndex == index)
        {

            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                // undo points drag operation is possible
                Undo.RecordObject(spline, "Move Point p0");
                //ask to save after moving a point
                EditorUtility.SetDirty(spline);

                //back to local coordiantes
                spline.points[index] = handleTransform.InverseTransformPoint(point);
            }
        }
        return point;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        spline = target as BezierSpline;
        if (GUILayout.Button("Add Curve"))
        {
            Undo.RecordObject(spline, "Add Curve");
            spline.AddCurve();
            EditorUtility.SetDirty(spline);
        }
    }
}
