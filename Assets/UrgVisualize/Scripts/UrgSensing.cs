using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UrgSensing : MonoBehaviour
{
    [Header("sensing params")]
    [Range(0.1f, 2.0f)] public float objThreshold = 0.5f;
    public float minWidth = 0.01f;
    public Bounds sensingArea;

    [Header("sensing result")]
    public List<SensedObject> sensedObjs;
    public Material mat;

    object lockObj;

    UrgDeviceEthernet urg;
    UrgControl urgControl;

    Mesh sensedObjMesh
    {
        get
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.vertices = Enumerable.Repeat(Vector3.zero, 5).ToArray();
                _mesh.SetIndices(
                    new[] {
                        0, 1, 4,
                        4, 1, 3,
                        3, 1, 2
                    },MeshTopology.Triangles, 0);
                _mesh.MarkDynamic();
            }
            return _mesh;
        }
    }
    Mesh _mesh;
    ComputeBuffer verticesBuffer;
    List<Vector3> verticesData;

    private void Start()
    {
        urgControl = GetComponent<UrgControl>();
        urg = GetComponent<UrgDeviceEthernet>();
        urg.onReadMD += OnReadMD;
        urg.onReadME += OnReadME;

        sensedObjs = new List<SensedObject>();
        lockObj = new object();
        verticesBuffer = new ComputeBuffer(1080, sizeof(float) * 3);
        verticesData = new List<Vector3>();
    }

    private void Update()
    {
        DrawMesh();
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sensingArea.center, sensingArea.size);
        Gizmos.color = Color.green;
        if (lockObj != null)
            lock (lockObj)
                for (var i = 0; i < sensedObjs.Count; i++)
                {
                    var so = sensedObjs[i];
                    Gizmos.DrawLine(so.p0, so.center);
                    Gizmos.DrawLine(so.center, so.p1);
                }
    }

    private void OnDestroy()
    {
        urg.onReadMD -= OnReadMD;
        urg.onReadME -= OnReadME;
        if (verticesBuffer != null)
            verticesBuffer.Release();
    }

    void DrawMesh()
    {
        lock (lockObj)
        {
            verticesData.Clear();
            for (var i = 0; i < sensedObjs.Count; i++)
                verticesData.AddRange(sensedObjs[i].vertices);
            verticesBuffer.SetData(verticesData);
            mat.SetInt("_VCount", sensedObjMesh.vertexCount);
            mat.SetBuffer("_VBuffer", verticesBuffer);
            var matrices = Enumerable.Repeat(transform.localToWorldMatrix, sensedObjs.Count).ToList();
            Graphics.DrawMeshInstanced(sensedObjMesh, 0, mat, matrices);
        }

    }

    void GetPointFromDistance(int step, float distance, ref Vector3 pos)
    {
        var angle = step * urgControl.angleDelta - urgControl.angleOffset + 90f;
        pos.x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
        pos.z = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
    }
    void OnReadMD(List<long> distances)
    {
        if (distances == null || distances.Count < 1)
            return;

        Vector3 prevP = Vector3.zero;
        Vector3 checkP = Vector3.zero;
        Vector3 currentP = Vector3.zero;
        Vector3 accum = Vector3.zero;
        int accumCount = 0;
        bool isObj = false;

        sensedObjs.Clear();

        GetPointFromDistance(0, distances[0], ref prevP);
        for (var i = 0; i < distances.Count; i++)
        {
            var d = distances[i] * 0.001f;
            GetPointFromDistance(i, d, ref currentP);

            if (isObj)
            {
                if (objThreshold * objThreshold < (currentP - prevP).sqrMagnitude && sensingArea.Contains(currentP))//new obj
                {
                    if (minWidth * minWidth < (prevP - checkP).sqrMagnitude)
                        sensedObjs.Add(new SensedObject() { p0 = checkP, p1 = prevP, center = accum / accumCount });
                    checkP = currentP;
                    accum = currentP;
                    isObj = true;
                    accumCount = 1;
                }
                else if (!sensingArea.Contains(currentP))//lost obj
                {
                    if (minWidth * minWidth < (prevP - checkP).sqrMagnitude)
                        sensedObjs.Add(new SensedObject() { p0 = checkP, p1 = prevP, center = accum / accumCount });
                    isObj = false;
                    accumCount = 0;
                }
                else//continue obj
                {
                    accum += currentP;
                    accumCount++;
                }
            }
            else
            {
                if (objThreshold * objThreshold < (currentP - prevP).sqrMagnitude && sensingArea.Contains(currentP))//new obj
                {
                    checkP = currentP;
                    accum = currentP;
                    isObj = true;
                    accumCount = 1;
                }
            }
            prevP = currentP;
        }
    }
    void OnReadME(List<long> distances, List<long> strengths)
    {
        OnReadMD(distances);
    }

    [System.Serializable]
    public struct SensedObject
    {
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 center;

        public Vector3[] vertices
        {
            get
            {
                if (_vs == null)
                    _vs = new Vector3[5];
                var width = (p1 - p0).magnitude;
                _vs[0] = p0;
                _vs[1] = center;
                _vs[2] = p1;
                _vs[3] = p1 + center.normalized * width * 0.5f;
                _vs[4] = p0 + center.normalized * width * 0.5f;
                return _vs;
            }
        }
        Vector3[] _vs;
    }
}
