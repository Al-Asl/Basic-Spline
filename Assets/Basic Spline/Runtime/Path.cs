using UnityEngine;

[AddComponentMenu("Path/Path")]
public class Path : MonoBehaviour , ISpline
{
    [SerializeField]
    private Spline spline = new Spline();

    public Segment this[int i] { get => spline[i]; set => spline[i] = value; }

    /// <summary>
    /// set the path to loop
    /// </summary>
    public bool Loop { get => spline.Loop; set => spline.Loop = value; }
    /// <summary>
    /// path arc length in world space
    /// </summary>
    public float Length => spline.Length*transform.localScale.x;

    /// <summary>
    /// get a path sample (with rotation)
    /// </summary>
    public Sample GetSample(float distance) => spline.GetSample(distance).Transform(transform);
    /// <summary>
    /// get the point on the path for the given arc length
    /// </summary>
    public Vector3 GetPoint(float distance) => transform.TransformPoint(spline.GetPoint(distance));
    /// <summary>
    /// get the closest path sample to the given world space point
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 point) => transform.TransformPoint(spline.GetClosestPoint(transform.InverseTransformPoint(point)));
    /// <summary>
    /// get the closest path point to the given world space point
    /// </summary>
    public Sample GetClosestSample(Vector3 point) => spline.GetClosestSample(transform.InverseTransformPoint(point));
    /// <summary>
    /// Split the segment into two at a given arc length, return the control point index
    /// </summary>
    public int Split(float distance) => spline.Split(distance);

    public int ControlPointsCount => spline.ControlPointsCount;
    public void SetControlPoint(int index, ControlPoint cpoint) => spline.SetControlPoint(index, cpoint);
    public ControlPoint GetControlPoint(int index) => spline.GetControlPoint(index);
    public void InsertControlPoint(int index, ControlPoint cpoint) => spline.InsertControlPoint(index, cpoint);
    public void AddControlPoint(ControlPoint cpoint) => spline.AddControlPoint(cpoint);
    public void RemoveControlPoint(int index) => spline.RemoveControlPoint(index);

    public int SegmentCount => spline.SegmentCount;
    public void GetSegmentAtDistance(float distance, out int segmentIndex, out float segmentDistance) => spline.GetSegmentAtDistance(distance, out segmentIndex, out segmentDistance);
    public void SetSegment(int index, Segment segment) => spline.SetSegment(index, segment);
    /// <summary>
    /// get the segment and segment arc length at a given arc length
    /// </summary>
    public Segment GetSegment(int index) => spline.GetSegment(index);
}
