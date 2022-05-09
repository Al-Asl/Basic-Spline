using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorInitializer
{
    [MenuItem("GameObject/Path/Path", priority = 15)]
    public static void CreatePath()
    {
        GameObject go = new GameObject("Path");
        Selection.activeObject = go;
        var path = go.AddComponent<Path>();
    }

    [MenuItem("GameObject/Path/Mover", priority = 16)]
    public static void CreateMover()
    {
        GameObject go = new GameObject("Mover");
        Selection.activeObject = go;
        var path = go.AddComponent<PathMover>();
    }
}