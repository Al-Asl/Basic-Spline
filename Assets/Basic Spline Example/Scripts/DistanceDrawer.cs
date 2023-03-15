using UnityEngine;
using BasicSpline;

public class DistanceDrawer : MonoBehaviour
{
    [SerializeField]
    public Path path;
    [SerializeField]
    public Material material;

    [SerializeField,HideInInspector]
    private LineRenderer lineRenderer;
    private Transform sphere;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        var sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereGO.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphereGO.GetComponent<MeshRenderer>().sharedMaterial = material;
        sphere = sphereGO.transform;
    }

    void Update()
    {
        var point = path.GetClosestPoint(transform.position);
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);

        sphere.position = Vector3.Lerp(point,transform.position,0.5f);
        sphere.localScale = Vector3.one * Vector3.Distance(point, transform.position);
    }
}
