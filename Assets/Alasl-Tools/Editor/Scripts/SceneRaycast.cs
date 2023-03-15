using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace AlaslTools
{

    public static class SceneRaycast
    {
        public enum RayCastFallBack
        {
            OrthoPlanes,
            CameraAlignPlane
        }

        public static class SmartRaycastSettings
        {
            public static bool SceneGeoRayCast;
            public static RayCastFallBack fallBack;
        }

        private static MethodInfo IntersectMeshMethod;
        private static Plane[] gridPlanes = new Plane[]
        {
        new Plane(Vector3.left,0),
        new Plane(Vector3.down,0),
        new Plane(Vector3.back,0),
        };

        static SceneRaycast()
        {
            IntersectMeshMethod =
                typeof(HandleUtility).GetMethod("IntersectRayMesh",
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        /// <summary>
        /// casting a ray from mouse position to world, if it fail to hit
        /// then it cast a ray against the best align orthogonal plane, and 
        /// the pos is only used shift these planes around.
        /// </summary>
        public static Vector3 SmartRaycast(Vector3 pos)
        {
            if (SmartRaycastSettings.SceneGeoRayCast)
            {
                if (MouseGeoRaycast(out var hit))
                    return hit.point;
                else
                    return DoRayCastFallBack(pos);
            }
            else
                return DoRayCastFallBack(pos);
        }

        private static Vector3 DoRayCastFallBack(Vector3 pos)
        {
            if (SmartRaycastSettings.fallBack == RayCastFallBack.CameraAlignPlane)
                return CameraAlignPlaneRaycast(pos);
            else
                return OrthoPlanesRaycast(pos);
        }

        public static Vector3 CameraAlignPlaneRaycast(Vector3 position)
        {
            var camera = SceneView.lastActiveSceneView.camera;
            var plane = new Plane(-camera.transform.forward,
                Vector3.Dot(camera.transform.forward, position - camera.transform.position));
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            plane.Raycast(ray, out var t);
            return ray.GetPoint(t);
        }

        public static Vector3 OrthoPlanesRaycast(Vector3 position)
        {
            var camera = SceneView.lastActiveSceneView.camera;

            float bestAlignValue = 0f;
            int index = 0;
            for (int i = 0; i < gridPlanes.Length; i++)
            {
                var value = Mathf.Abs(Vector3.Dot(gridPlanes[i].normal,
                    camera.transform.forward));
                if (value >= bestAlignValue)
                {
                    bestAlignValue = value;
                    index = i;
                }
            }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var plane = gridPlanes[index];
            plane.distance = position[index];
            plane.Raycast(ray, out var t);

            return ray.GetPoint(t);
        }

        public static bool MouseGeoRaycast(out RaycastHit hit)
        {
            hit = default;
            var mpos = Event.current.mousePosition;

            var go = HandleUtility.PickGameObject(mpos, false);
            if (go != null)
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null)
                    return IntersectRayMesh(HandleUtility.GUIPointToWorldRay(mpos), filter, out hit);
            }

            return false;
        }

        public static bool IntersectRayMesh(Ray ray, MeshFilter meshFilter, out RaycastHit hit)
        {
            return IntersectRayMesh(ray, meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix, out hit);
        }

        public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
        {
            var parameters = new object[] { ray, mesh, matrix, null };
            bool didHit = (bool)IntersectMeshMethod.Invoke(null, parameters);
            hit = (RaycastHit)parameters[3];
            return didHit;
        }
    }

}