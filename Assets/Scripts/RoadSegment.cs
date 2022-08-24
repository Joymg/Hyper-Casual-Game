using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class RoadSegment : MonoBehaviour
{
    [SerializeField]
    public BezierSpline spline;

    [SerializeField,Range(2,32)]
    int edgeRingCount = 8;

    public Mesh2D shape2D;
    public MeshCollider meshCollider;

    [Range(0f, 1f), SerializeField] float t = 0;
    Mesh mesh;

    private void Awake()
    {
        spline = GetComponent<BezierSpline>();
        mesh = new Mesh();
        meshCollider = GetComponent<MeshCollider>();
        mesh.name = "RoadSegment";

        GetComponent<MeshFilter>().sharedMesh = mesh;

    }

    private void Update()
    {
        GenerateMesh();
    }


    void GenerateMesh()
    {
        
        mesh.Clear();
        float uSpan = shape2D.CalcUspan();

        //total of rings the road will have
        int steps = edgeRingCount * spline.SegmentCount;
        //Vertex

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        for (int ring = 0; ring < steps; ring++)
        {
            float t = ring / (steps - 1f);
            OrientedPoint op = new OrientedPoint(spline.GetPoint(t), Quaternion.LookRotation(spline.GetVelocity(t)));
            
            for (int i = 0; i < shape2D.VertexCount; i++)
            {
                verts.Add(op.LocalToWorldPos(shape2D.vertices[i].point));
                normals.Add(op.LocalToWorldVec(shape2D.vertices[i].normal));
                uvs.Add(new Vector2(shape2D.vertices[i].u , t * spline.GetApproxLenght() / uSpan));
            }
        }



        //Triangles
        List<int> triIndices = new List<int>();
        for (int ring = 0; ring < steps-1; ring++)
        {
            int rootIndex = ring * shape2D.VertexCount;
            int rootIndexNext = (ring+1) * shape2D.VertexCount;

            for (int line = 0; line < shape2D.LineCount; line+=2)
            {
                int lineIndexA = shape2D.lineIndices[line];
                int lineIndexB = shape2D.lineIndices[line + 1];

                int currentA = rootIndex + lineIndexA;
                int currentB = rootIndex + lineIndexB;

                int nextA = rootIndexNext + lineIndexA;
                int nextB = rootIndexNext + lineIndexB;

                triIndices.Add(currentA);
                triIndices.Add(nextA);
                triIndices.Add(nextB);
                
                triIndices.Add(currentA);
                triIndices.Add(nextB);
                triIndices.Add(currentB);
            }

        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(triIndices,0);
        mesh.SetUVs(0,uvs);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();


        meshCollider.sharedMesh = mesh;

    }
   public void OnDrawGizmos()
    {

        Gizmos.DrawSphere(spline.GetControlPoint(0), 0.1f);
        for (int i = 1; i < spline.PointCount; i += 3)
        {
            Gizmos.DrawSphere(spline.GetControlPoint(i), 0.1f);
            Gizmos.DrawSphere(spline.GetControlPoint(i + 1), 0.1f);
            Gizmos.DrawSphere(spline.GetControlPoint(spline.LoopIndex(i+2)), 0.1f);
            Handles.DrawBezier(spline.GetControlPoint(i - 1), spline.GetControlPoint(spline.LoopIndex(i + 2)), spline.GetControlPoint(i), spline.GetControlPoint(i + 1), Color.white, EditorGUIUtility.whiteTexture, 1f);


        }

        Vector3 testPoint = spline.GetPoint(t);
        Quaternion testOrientation = Quaternion.LookRotation(spline.GetVelocity(t));

        //Draw Shape2d outilne
        Vector3[] verts = shape2D.vertices.Select(v => testPoint + (testOrientation * v.point)).ToArray();

        for (int i = 0; i < shape2D.lineIndices.Length; i += 2)
        {
            Vector3 a = verts[shape2D.lineIndices[i]];
            Vector3 b = verts[shape2D.lineIndices[i + 1]];
            Gizmos.DrawLine(a, b);

        }

    }
}
