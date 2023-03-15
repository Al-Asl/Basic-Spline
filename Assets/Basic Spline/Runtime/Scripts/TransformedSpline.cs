using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BasicSpline
{
    public class TransformedSpline : ISpline
    {
        private ISpline spline;
        private Transform transform;

        public TransformedSpline(ISpline spline, Transform transform)
        {
            this.spline = spline;
            this.transform = transform;
        }

        private float scale => transform.lossyScale.x;

        /// <summary>
        /// set the path to loop
        /// </summary>
        public bool Loop { get => spline.Loop; set => spline.Loop = value; }
        /// <summary>
        /// path arc length
        /// </summary>
        public float Length => spline.Length * scale;

        /// <summary>
        /// get a path sample (with rotation)
        /// </summary>
        public Sample GetSample(float distance) => spline.GetSample(distance).Transform(transform);
        /// <summary>
        /// get the point on the path for the given arc length
        /// </summary>
        public Vector3 GetPoint(float distance) => transform.TransformPoint(spline.GetPoint(distance));
        /// <summary>
        /// get the closest path sample to the given point
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 point) => transform.TransformPoint(spline.GetClosestPoint(transform.InverseTransformPoint(point)));
        /// <summary>
        /// get the closest path point to the given point
        /// </summary>
        public Sample GetClosestSample(Vector3 point) => spline.GetClosestSample(transform.InverseTransformPoint(point)).Transform(transform);
        /// <summary>
        /// Split the segment into two at a given arc length, return the control point index
        /// </summary>
        public int Split(float distance) => spline.Split(distance / scale);

        public int ControlPointsCount => spline.ControlPointsCount;
        public void SetControlPoint(int index, ControlPoint cpoint) => spline.SetControlPoint(index, InverseTransformPoint(cpoint));
        public ControlPoint GetControlPoint(int index) => TransformPoint(spline.GetControlPoint(index));
        public void InsertControlPoint(int index, ControlPoint cpoint) => spline.InsertControlPoint(index, InverseTransformPoint(cpoint));
        public void AddControlPoint(ControlPoint cpoint) => spline.AddControlPoint(InverseTransformPoint(cpoint));
        public void RemoveControlPoint(int index) => spline.RemoveControlPoint(index);
        public IEnumerable<ControlPoint> IterateControlPoints() => spline.IterateControlPoints().Select((point) => TransformPoint(point));

        public int SegmentCount => spline.SegmentCount;
        public void SetSegment(int index, Segment segment) => throw new System.NotImplementedException();
        public Segment GetSegment(int index) 
        {
            return new Segment(GetControlPoint(index), GetControlPoint((index + 1) % ControlPointsCount));
        }
        public IEnumerable<Segment> IterateSegments()
        {
            for (int i = 0; i < SegmentCount; i++)
                yield return GetSegment(i);
        }

        /// <summary>
        /// get the segment and segment distance at a given arc length
        /// </summary>
        public void GetSegmentAtDistance(float distance, out int segmentIndex, out float segmentDistance) => spline.GetSegmentAtDistance(distance, out segmentIndex, out segmentDistance);
        public void GetClosestDistance(Vector3 point, out int segmentIndex, out float t) => spline.GetClosestDistance(point, out segmentIndex, out t);

        private ControlPoint TransformPoint(ControlPoint cpoint)
        {
            cpoint.point = transform.TransformPoint(cpoint.point);
            cpoint.inTangent = transform.TransformPoint(cpoint.inTangent);
            cpoint.outTangent = transform.TransformPoint(cpoint.outTangent);
            return cpoint;
        }

        private ControlPoint InverseTransformPoint(ControlPoint cpoint)
        {
            cpoint.point = transform.InverseTransformPoint(cpoint.point);
            cpoint.inTangent = transform.InverseTransformPoint(cpoint.inTangent);
            cpoint.outTangent = transform.InverseTransformPoint(cpoint.outTangent);
            return cpoint;
        }
    }
}
