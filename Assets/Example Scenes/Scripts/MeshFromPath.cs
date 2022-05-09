using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshFromPath : MonoBehaviour
{
    [SerializeField]
    public Path path;
    [Space]
    [SerializeField]
    public int resolution = 30;
    [SerializeField]
    public float width = 0.2f;

    [SerializeField,HideInInspector]
    private Mesh mesh;


    private void Start()
    {
        mesh = SplineUtility.CreateMesh(path, Matrix4x4.identity, width, resolution);
        var filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;
    }

    private void LateUpdate()
    {
        SetToPath();
        SplineUtility.UpdateMesh(path, mesh, Matrix4x4.identity, width, resolution);
    }

    void SetToPath()
    {
        transform.position = path.transform.position;
        transform.rotation = path.transform.rotation;
        transform.localScale = path.transform.localScale;
    }
}
