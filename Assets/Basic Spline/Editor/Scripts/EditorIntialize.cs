using UnityEngine;
using UnityEditor;

namespace BasicSpline
{

    public static class EditorInitializer
    {

        [MenuItem("GameObject/Path/Path", priority = 15)]
        public static void CreatePath()
        {
            GameObject go = new GameObject("Path");
            Selection.activeObject = go;
            var path = go.AddComponent<Path>();
        }

        [MenuItem("GameObject/Path/PathFollower", priority = 16)]
        public static void CreateMover()
        {
            GameObject go = new GameObject("PathFollower");
            Selection.activeObject = go;
            var path = go.AddComponent<PathFollower>();
        }
    }

}