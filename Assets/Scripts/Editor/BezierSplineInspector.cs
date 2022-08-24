using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    const float segmentSelectDistanceThreshold = 4f;

    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const int curveSteps = 10;
    private const float directionScale =0.5f;

    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private int selectedPointIndex = -1;
    private int selectedSegmentIndex = -1;

    private Transform cameraTransform;
    private Camera cam;
    private SceneView sV;

    [SerializeField]
    public HandleMode mode;

    //Used for visual feedback of the point mode
    private static Color[] modeColors = {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private void OnEnable()
    {
        spline = target as BezierSpline;
    }

    private void OnSceneGUI()
    {

        Input();
        //Draw();

        handleTransform = spline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        for (int i = 1; i < spline.PointCount; i += 3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(spline.LoopIndex(i+2));
            //sets the first point of the new curve to the last of the previous one
            p0 = p3;
        }


        for (int i = 0; i < spline.SegmentCount; i++)
        {
            Vector3[] points = spline.GetPointsInSegment(i);
            Handles.color = Color.black;
            Handles.DrawLine(points[1], points[0]);
            Handles.DrawLine(points[2], points[3]);
            Color segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? Color.red : Color.green;
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
        }

        Event e = Event.current;
        switch (e.type)
        {
            case EventType.KeyDown:
                {
                    if (Event.current.keyCode == (KeyCode.E))
                    {
                        mode = HandleMode.RotationMode;
                    }
                    else if (Event.current.keyCode == (KeyCode.W))
                    {
                        mode = HandleMode.MoveMode;
                    
                    }
                }
                break;
        }

        ShowDirections();
    }

    private void Input()
    {

       // Vector2 mousePos = new Vector2();
        Event guiEvent = Event.current;
        //Debug.Log(guiEvent.mousePosition);

        Vector3 mousePos = MouseUtility.GetMouseWorldPosition();

        //Keeps the focus on the GameObject is been edited
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            //Split
            if (selectedSegmentIndex != -1)
            {
                Undo.RecordObject(spline, "Split Segment");
                spline.SplitSegment(mousePos, selectedSegmentIndex);
                EditorUtility.SetDirty(spline);
            }
            else if(!spline.Loop)
            {
                Undo.RecordObject(spline, "Create Segment");
                spline.AddSegment(mousePos);
                EditorUtility.SetDirty(spline);
            }
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button ==1 && guiEvent.shift)
        {
            float minDstToAnchor = 8f;
            int closestAnchorIndex= -1;
            
            for (int i = 0; i < spline.PointCount; i+=3)
            {
                float dst = Vector3.Distance(mousePos, spline[i]);
                if (dst < minDstToAnchor)
                {
                    minDstToAnchor = dst;
                    closestAnchorIndex = i;
                }
            }


            if (closestAnchorIndex !=-1)
            {
                Undo.RecordObject(spline, "Delete Segment");
                spline.DeleteSegment(closestAnchorIndex);
            }
        }

        if (guiEvent.type == EventType.MouseMove)
        {
            float minDstToSegment = segmentSelectDistanceThreshold;
            int newSelectedSegmentIndex = -1;
            for (int i = 0; i < spline.SegmentCount; i++)
            {
                Vector3[] points = spline.GetPointsInSegment(i);
                float dst = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                //Debug.Log(i + " " + dst);
                if (dst < minDstToSegment)
                {
                    minDstToSegment = dst;
                    newSelectedSegmentIndex = i;
                }
            }

            Debug.Log("newSelectedSegmentIndex: " + newSelectedSegmentIndex);
            if (newSelectedSegmentIndex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newSelectedSegmentIndex;
                HandleUtility.Repaint();
                Debug.Log("selectedSegmentIndex: " + selectedSegmentIndex);
            }
        }
    }

    void Draw()
    {

        for (int i = 0; i < spline.SegmentCount; i++)
        {
            Vector3[] points = spline.GetPointsInSegment(i);
            Handles.color = Color.black;
            Handles.DrawLine(points[1], points[0]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2);
        }

        Handles.color = Color.red;
        for (int i = 0; i < spline.PointCount; i++)
        {
            Vector3 newPos = Handles.FreeMoveHandle(spline[i], Quaternion.identity, .1f, Vector3.zero, Handles.CylinderHandleCap);
            if (spline[i] != newPos)
            {
                Undo.RecordObject(spline, "Move point");
                spline.MovePoint(i, newPos);
            }
        }
    }

    private void ShowDirections()
    {
        Handles.color = Color.cyan;
        Vector3 point = spline.GetPoint(0f);

        Handles.DrawLine(point, point + spline.GetDirection(0f)*directionScale);

        int steps = curveSteps * spline.SegmentCount;
        for (int i = 1; i < steps; i++)
        {
            point = spline.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
        }
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = handleTransform.InverseTransformPoint(spline.GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);
        //making frist point twice as big, for visual clarity
        if (index == 0)
        {
            size *= 2f;
        }
        Handles.color = modeColors[(int)spline.GetControlPointMode(index)];
        if (Handles.Button(point,handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedPointIndex = index;
            //Requesting a repaint to refresh the editor when selecting a point
            Repaint();
        }
        if (selectedPointIndex == index && (selectedPointIndex % 3 == 0 || !spline.AutoSetControlPoints))
        {
            switch (mode)
            {
                case HandleMode.MoveMode:
                    EditorGUI.BeginChangeCheck();
                    point = Handles.DoPositionHandle(point, handleRotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // undo points drag operation is possible
                        Undo.RecordObject(spline, "Move Point p0");
                        //ask to save after moving a point
                        EditorUtility.SetDirty(spline);

                        //back to local coordiantes
                        spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
                        //spline.MovePoint(index, handleTransform.InverseTransformPoint(point));
                    }
                    break;
                case HandleMode.RotationMode:
                    EditorGUI.BeginChangeCheck();
                    Quaternion q = Handles.DoRotationHandle(handleRotation, point);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // undo points drag operation is possible
                        Undo.RecordObject(spline, "Move Point p0");
                        //ask to save after moving a point
                        EditorUtility.SetDirty(spline);

                        //back to local coordiantes
                        spline.SetControlPoint(index, handleTransform.InverseTransformPoint(q.eulerAngles));
                    }
                    break;
                default:
                    break;
            }
            
        }
        return point;
    }

    public override void OnInspectorGUI()
    {
        spline = target as BezierSpline;

        EditorGUI.BeginChangeCheck();
        bool loop = EditorGUILayout.Toggle("Loop", spline.Loop);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Toggle Loop");
            EditorUtility.SetDirty(spline);
            spline.Loop = loop;
        }

        if (selectedPointIndex >= 0 && selectedPointIndex < spline.PointCount)
        {
            DrawSelectedPointInspector();
        }
        if (GUILayout.Button("Add Curve"))
        {
            Undo.RecordObject(spline, "Add Curve");
            spline.AddSegment();
            EditorUtility.SetDirty(spline);
        }
        EditorGUI.BeginChangeCheck();
        bool autoSetControlPoints = EditorGUILayout.Toggle("autoSetControlPoints", spline.AutoSetControlPoints);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Toggle autoSetControlPoints");
            EditorUtility.SetDirty(spline);
            spline.AutoSetControlPoints = autoSetControlPoints;
        }
    }

    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("Selected Point");
        int index = EditorGUILayout.IntField("index", selectedPointIndex);
        EditorGUI.BeginChangeCheck();
        Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedPointIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Move Object");
            EditorUtility.SetDirty(spline);
            spline.SetControlPoint(selectedPointIndex, point);
        }

        EditorGUI.BeginChangeCheck();
        BezierControlPointMode mode =
            (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode",spline.GetControlPointMode(selectedPointIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Move Object");
            spline.SetControlPointMode(selectedPointIndex, mode);
            EditorUtility.SetDirty(spline);
        }
    }


}
