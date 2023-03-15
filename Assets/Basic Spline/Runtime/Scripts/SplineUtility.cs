using System.Collections.Generic;
using UnityEngine;

namespace BasicSpline
{

    public static class SplineUtility
    {
        public static Mesh CreateMesh(ISpline spline, Matrix4x4 transform, float width, int subdivsion = 30)
        {
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            UpdateMesh(spline, mesh, transform, width, subdivsion);
            return mesh;
        }

        public static void UpdateMesh(ISpline spline, Mesh mesh, Matrix4x4 transform, float width, int subdivsion = 30)
        {
            mesh.Clear();
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<int> indices = new List<int>();

            var hw = width * 0.5f;

            foreach(var seg in spline.IterateSegments())
            {
                foreach(var sample in seg.IterateSamples(subdivsion))
                {
                    sample.Transform(transform);
                    verts.Add(sample.point - hw * sample.right);
                    verts.Add(sample.point + hw * sample.right);
                    norms.Add(sample.up);
                    norms.Add(sample.up);
                }
            }

            var segmets = verts.Count / 2 - 1;
            for (int i = 0; i < segmets; i++)
            {
                var index = i * 2;
                indices.Add(index);
                indices.Add(index + 2);
                indices.Add(index + 1);

                indices.Add(index + 2);
                indices.Add(index + 3);
                indices.Add(index + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
        }
    }

}