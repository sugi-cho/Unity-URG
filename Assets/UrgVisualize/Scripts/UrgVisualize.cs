using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrgVisualize : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("this is for develop")]
    public List<long> distances;
    UrgDeviceEthernet urg;

    void Start()
    {
        urg = GetComponent<UrgDeviceEthernet>();
    }

    // Update is called once per frame
    void Update()
    {
        //distances = urg.distances;
    }
}
