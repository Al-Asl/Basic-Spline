using UnityEngine;

namespace BasicSpline
{

    [ExecuteInEditMode, AddComponentMenu("BasicSpline/PathFollower")]
    public class PathFollower : MonoBehaviour
    {
        [SerializeField]
        public Path path;

        [Space]
        [SerializeField]
        public float speed;
        [SerializeField]
        public float distance;
        [SerializeField]
        public bool applyRotation;

        [Space]
        [SerializeField]
        [Tooltip("check this for better performance. It will do fine at a low speed value")]
        public bool fastMode;

        [SerializeField, HideInInspector]
        private float lastDistance;
        [SerializeField, HideInInspector]
        private int segmentIndex;
        [SerializeField, HideInInspector]
        private float t;

        void Update()
        {
            if (path == null)
                return;

            if (fastMode)
            {
                if (lastDistance != distance)
                {
                    path.GetSegmentAtDistance(distance, out segmentIndex, out var dist);
                    t = path.GetSegment(segmentIndex).GetParameter(dist);

                    SetAtSegment();
                    lastDistance = distance;
                    return;
                }

                var d1 = path.GetSegment(segmentIndex).GetD1(Mathf.Clamp01(t)).magnitude;

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
#endif
                    t += (Time.deltaTime * speed) / d1;
                UpdateSegment();

                var lastPos = transform.position;
                SetAtSegment();
                distance += Vector3.Distance(lastPos, transform.position) * Mathf.Sign(speed);

                ClampDistance();
                lastDistance = distance;
            }
            else
            {

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
#endif
                    distance += Time.deltaTime * speed;
                ClampDistance();

                if (applyRotation)
                    path.GetSample(distance).Apply(transform);
                else
                    transform.position = path.GetPoint(distance);

            }
        }

        private void UpdateSegment()
        {
            if (t > 1f || t < 0)
            {
                var sd = Mathf.FloorToInt(t);
                var nextIndex = segmentIndex + sd;

                if (path.Loop)
                {
                    segmentIndex = (nextIndex + path.SegmentCount) % path.SegmentCount;
                    t = (t % 1f + 1f) % 1f;
                }
                else
                {
                    if (nextIndex < 0)
                    {
                        segmentIndex = 0;
                        t = 0;
                    }
                    else if (nextIndex > path.SegmentCount - 1)
                    {
                        segmentIndex = path.SegmentCount - 1;
                        t = 1f;
                    }
                    else
                    {
                        segmentIndex = nextIndex;
                        t = (t % 1f + 1f) % 1f;
                    }
                }
            }
        }

        void SetAtSegment()
        {
            if (applyRotation)
                path.GetSegment(segmentIndex).GetSample(t).Transform(path.transform).Apply(transform);
            else
                transform.position = path.transform.TransformPoint(path.GetSegment(segmentIndex).GetPoint(t));
        }

        void ClampDistance()
        {
            distance = !path.Loop ? Mathf.Clamp(distance, 0, path.Length) : distance;
        }
    }

}