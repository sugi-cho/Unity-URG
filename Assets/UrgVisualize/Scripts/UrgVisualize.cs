using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class UrgVisualize : MonoBehaviour
{
    public Renderer sensingAreaRenderer;
    Transform sensingAreaTrs;
    MaterialPropertyBlock mpb { get { if (_mpb == null) _mpb = new MaterialPropertyBlock(); return _mpb; } }
    MaterialPropertyBlock _mpb;

    // Start is called before the first frame update
    [Header("this is for develop")]
    public List<long> distances;
    UrgControl urgControl;
    UrgDeviceEthernet urg;

    ComputeBuffer distanceDataBuffer;

    [Header("props for shader")]
    [SerializeField] Vector4 urgPos;
    [SerializeField] Vector4 urgProps;

    [Header("sensing area")]
    public Vector2 areaSize = new Vector2(1f, 1f);

    void Start()
    {
        sensingAreaTrs = sensingAreaRenderer.transform;
        urgControl = GetComponent<UrgControl>();
        urg = GetComponent<UrgDeviceEthernet>();

        distanceDataBuffer = new ComputeBuffer(urgControl.endStep - urgControl.startStep + 1, sizeof(float));
    }

    private void OnDestroy()
    {
        if (distanceDataBuffer != null)
            distanceDataBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {
        //distances = urg.distances;
        if (urg.distances != null && 0 < urg.distances.Count)
        {
            distances.Clear();
            distances.AddRange(urg.distances);
        }
        var ds = distances.Select(d => d * 0.001f).ToList();

        var pos = (Vector2)transform.localPosition;
        var dir = (Vector2)sensingAreaTrs.InverseTransformDirection(transform.forward);
        var sensorAngle = Mathf.Atan2(dir.y, dir.x);
        urgPos.Set(pos.x, pos.y, sensorAngle, 0);
        urgProps.Set(urgControl.endStep, urgControl.angleOffset * Mathf.Deg2Rad, 1f / (urgControl.angleDelta * Mathf.Deg2Rad), 0);

        sensingAreaRenderer.GetPropertyBlock(mpb);
        distanceDataBuffer.SetData(ds);
        mpb.SetBuffer("_UrgData", distanceDataBuffer);
        mpb.SetVector("_UrgPos", urgPos);
        mpb.SetVector("_UrgProps", urgProps);
        mpb.SetVector("_AreaProps", areaSize);
        sensingAreaRenderer.SetPropertyBlock(mpb);
    }
}
