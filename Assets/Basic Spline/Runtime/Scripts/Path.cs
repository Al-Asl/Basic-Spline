using System.Collections.Generic;
using UnityEngine;

namespace BasicSpline
{

    [ExecuteInEditMode, AddComponentMenu("BasicSpline/Path")]
    public class Path : MonoBehaviour, ISpline
    {
        [SerializeField]
        private Spline spline;

        private TransformedSpline transformedSpline;

#if UNITY_EDITOR
        [SerializeField]
        private List<int> selected_points_editor_only = new List<int>();
#endif

        void OnEnable()
        {
            transformedSpline = new TransformedSpline(spline, transform);
        }

        private void OnValidate()
        {
            OnEnable();
        }

        /// <summary>
        /// set the path to loop
        /// </summary>
        public bool Loop { get => spline.Loop; set => spline.Loop = value; }
        /// <summary>
        /// path arc length in world space
        /// </summary>
        public float Length => transformedSpline.Length;

        /// <summary>
        /// get a path sample (with rotation)
        /// </summary>
        public Sample GetSample(float distance) => transformedSpline.GetSample(distance);
        /// <summary>
        /// get the point on the path for the given arc length
        /// </summary>
        public Vector3 GetPoint(float distance) => transformedSpline.GetPoint(distance);
        /// <summary>
        /// get the closest path sample to the given world space point
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 point) => transformedSpline.GetClosestPoint(point);
        /// <summary>
        /// get the closest path point to the given world space point
        /// </summary>
        public Sample GetClosestSample(Vector3 point) => transformedSpline.GetClosestSample(point);
        /// <summary>
        /// Split the segment into two at a given arc length, return the control point index
        /// </summary>
        public int Split(float distance) => spline.Split(distance);

        public int ControlPointsCount => spline.ControlPointsCount;
        /// <summary>
        /// set the control point in world space
        /// </summary>
        public void SetControlPoint(int index, ControlPoint cpoint) => transformedSpline.SetControlPoint(index,cpoint);
        /// <summary>
        /// get the control point in world space
        /// </summary>
        public ControlPoint GetControlPoint(int index) => transformedSpline.GetControlPoint(index);
        /// <summary>
        /// insert a control point in world space
        /// </summary>
        public void InsertControlPoint(int index, ControlPoint cpoint) => transformedSpline.InsertControlPoint(index,cpoint);
        /// <summary>
        /// add a control point in world space
        /// </summary>
        public void AddControlPoint(ControlPoint cpoint) => transformedSpline.AddControlPoint(cpoint);
        public void RemoveControlPoint(int index) => transformedSpline.RemoveControlPoint(index);
        public IEnumerable<ControlPoint> IterateControlPoints() => transformedSpline.IterateControlPoints();


        //////////  segment control are in local space //////////

        public int SegmentCount => spline.SegmentCount;
        /// <summary>
        /// set segment in local space
        /// </summary>
        public void SetSegment(int index, Segment segment) => spline.SetSegment(index, segment);
        /// <summary>
        /// get segment in local space
        /// </summary>
        public Segment GetSegment(int index) => spline.GetSegment(index);
        /// <summary>
        /// enumerate segments, segments are in local space
        /// </summary>
        public IEnumerable<Segment> IterateSegments() => spline.IterateSegments();

        /// <summary>
        /// get the segment and segment distance at a given arc length
        /// </summary>
        public void GetSegmentAtDistance(float distance, out int segmentIndex, out float segmentDistance) => spline.GetSegmentAtDistance(distance, out segmentIndex, out segmentDistance);
        public void GetClosestDistance(Vector3 point, out int segmentIndex, out float t) => spline.GetClosestDistance(point, out segmentIndex, out t);
    }
}