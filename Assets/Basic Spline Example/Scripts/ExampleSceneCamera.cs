using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleSceneCamera : MonoBehaviour
{
    public Transform[] points;
    public float duration = 1f;

    private int index = 0;

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            index = (index + 1) % points.Length;
            StartCoroutine(GoTo(points[index].position));
        }
    }

    IEnumerator GoTo(Vector3 position)
    {
        var startPos = transform.position;
        var startTime = Time.time;

        while(Time.time - startTime < duration)
        {
            yield return null;
            var t = Time.time - startTime / duration;
            t = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector3.Lerp(startPos, position, t);
        }
        transform.position = position;
    }
}
