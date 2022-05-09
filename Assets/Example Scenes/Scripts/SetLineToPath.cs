using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetLineToPath : MonoBehaviour
{
    [SerializeField]
    public Path path;
    [SerializeField]
    public int resolution;

    private void Update()
    {
        var renderer = GetComponent<LineRenderer>();
        if (renderer == null || path == null)
            return;

        renderer.useWorldSpace = true;
        renderer.positionCount = path.SegmentCount * resolution;
        for (int i = 0 ,c = 0; i < path.SegmentCount; i++)
            path[i].Iterate(excute:(point) =>
            {
                renderer.SetPosition(c++, path.transform.TransformPoint(point));
            }, resolution - 1);
    }
}
