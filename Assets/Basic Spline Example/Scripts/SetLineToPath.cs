using UnityEngine;
using BasicSpline;

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

        int i = 0;
        foreach(var seg in path.IterateSegments())
            foreach(var point in seg.IteratePoints(resolution - 1))
                renderer.SetPosition(i++, path.transform.TransformPoint(point));
    }
}
