using System.Collections.Generic;
using UnityEngine;

public interface ISpline
{
    Segment this[int i] { get; set; }

    /// <summary>
    /// Spline arc length
    /// </summary>
    float Length { get; }
    /// <summary>
    /// is the spline looping?
    /// </summary>
    bool Loop { get; set; }
    /// <summary>
    /// get a spline sample (with rotation)
    /// </summary>
    Sample GetSample(float distance);
    /// <summary>
    /// get the point for the given arc length
    /// </summary>
    Vector3 GetPoint(float distance);
    /// <summary>
    /// get the closest spline sample to the given point
    /// </summary>
    Sample GetClosestSample(Vector3 point);
    /// <summary>
    /// get the closest spline point to the given point
    /// </summary>
    Vector3 GetClosestPoint(Vector3 point);
    /// <summary>
    /// split the segment into two at the given arc length, 
    /// return the control point index
    /// </summary>
    int Split(float distance);

    int ControlPointsCount { get; }
    void SetControlPoint(int index, ControlPoint point);
    ControlPoint GetControlPoint(int index);
    void InsertControlPoint(int index, ControlPoint controlPoint);
    void AddControlPoint(ControlPoint controlPoint);
    void RemoveControlPoint(int index);

    int SegmentCount { get; }
    /// <summary>
    /// get the segment and segment arc length at the given arc length
    /// </summary>
    void GetSegmentAtDistance(float distance, out int segmentIndex, out float segmentDistance);
    void SetSegment(int index, Segment segment);
    Segment GetSegment(int index);
}

[System.Serializable]
public class Spline : ISpline
{
    [SerializeField]
    private bool loop;
    [SerializeField]
    private float length;
    [SerializeField]
    private List<ControlPoint> points = new List<ControlPoint>();
    [SerializeField]
    private List<Segment> segments = new List<Segment>();

    public Spline(Spline src) : this(src.points) { }

    public Spline() : this(
        new ControlPoint[]
        {
            new ControlPoint(Vector3.zero, Vector3.back * 0.25f, Vector3.forward * 0.25f),
            new ControlPoint(Vector3.forward, Vector3.forward * 0.75f, Vector3.forward * 1.25f)
        }) 
    { }

    public Spline(IEnumerable<ControlPoint> controlPoints)
    {
        points = new List<ControlPoint>(controlPoints);
        segments = new List<Segment>();
        for (int i = 0; i < points.Count; i++)
            segments.Add(new Segment(points[i], points[LoopIndex(i + 1)]));
        AverageUpVector();
        UpdateLength();
    }

    public Segment this[int i] { get => GetSegment(i); set => SetSegment(i, value); }
    /// <summary>
    /// is the spline looping?
    /// </summary>
    public bool Loop { get => loop; set { if (loop != value) UpdateLength(value); loop = value; } }
    /// <summary>
    /// Spline arc length
    /// </summary>
    public float Length => length;


    /// <summary>
    /// get a spline sample (with rotation)
    /// </summary>
    public Sample GetSample(float distance)
    {
        GetSegmentAtDistance(distance, out var segmentIndex, out var segmentLength);
        var segment = GetSegment(segmentIndex);
        return segment.GetSample(segment.GetParameter(segmentLength));
    }
    /// <summary>
    ///  get the point for the given arc length
    /// </summary>
    public Vector3 GetPoint(float distance)
    {
        GetSegmentAtDistance(distance, out var segmentIndex, out var segmentLength);
        var segment = GetSegment(segmentIndex);
        return segment.GetPoint(segment.GetParameter(segmentLength));
    }
    /// <summary>
    /// get the closest spline sample to the given point
    /// </summary>
    public Sample GetClosestSample(Vector3 point)
    {
        GetClosestLength(point, out var index, out var t);
        return GetSegment(index).GetSample(t);
    }
    /// <summary>
    /// get the closest spline point to the given point
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 point) 
    {
        GetClosestLength(point, out var index, out var t);
        return GetSegment(index).GetPoint(t); 
    }
    /// <summary>
    /// split the segment into two at the given arc length, 
    /// return the control point index
    /// </summary>
    public int Split(float distance)
    {
        GetSegmentAtDistance(distance, out var segmentIndex, out var segmentLength);
        var segment = GetSegment(segmentIndex);
        segment.Split(segment.GetParameter(segmentLength),out var pa,out var pb , out var mangle);

        var cpoint = GetControlPoint(segmentIndex);
        cpoint.outTangent = pa.aTan;
        SetControlPoint(segmentIndex, cpoint);

        var nextIndex = LoopIndex(segmentIndex + 1);
        cpoint = GetControlPoint(nextIndex);
        cpoint.inTangent = pb.bTan;
        SetControlPoint(nextIndex, cpoint);

        InsertControlPoint(nextIndex, new ControlPoint(pa.b, pa.bTan, pb.aTan, mangle));

        return nextIndex;
    }


    public int ControlPointsCount => points.Count;
    public void SetControlPoint(int index, ControlPoint cpoint) 
    { 
        points[index] = cpoint;
        var preIndex = LoopIndex(index - 1);
        var nextIndex = LoopIndex(index + 1);
        segments[index] = new Segment(cpoint, points[nextIndex]);
        segments[preIndex] = new Segment(points[preIndex],cpoint);
        AverageUpVector(index);
        UpdateLength();
    }
    public ControlPoint GetControlPoint(int index) => points[index];
    public void InsertControlPoint(int index, ControlPoint cpoint) 
    { 
        points.Insert(index, cpoint);
        var preIndex = LoopIndex(index - 1);
        var nextIndex = LoopIndex(index + 1);
        segments.Insert(index, new Segment(cpoint, points[nextIndex]));
        segments[preIndex] = new Segment(points[preIndex], cpoint);
        AverageUpVector(index);
        UpdateLength();
    }
    public void AddControlPoint(ControlPoint cpoint) { 
        points.Add(cpoint);
        segments.Add(new Segment(cpoint, points[0]));
        segments[points.Count - 2] = new Segment(points[points.Count - 2], cpoint);
        AverageUpVector(points.Count - 1);
        UpdateLength();
    }
    public void RemoveControlPoint(int index) {
        points.RemoveAt(index);
        segments.RemoveAt(index);
        index = LoopIndex(index);
        var preIndex = LoopIndex(index-1);
        segments[preIndex] = new Segment(points[preIndex], points[index]);
        AverageUpVectorForSegment(preIndex);
        UpdateLength();
    }


    public int SegmentCount => loop ? segments.Count : segments.Count - 1;
    public void SetSegment(int index, Segment segment) => segments[index] = segment;
    public Segment GetSegment(int index) => segments[index];
    /// <summary>
    /// get the segment and segment arc length at the given arc length
    /// </summary>
    public void GetSegmentAtDistance(float distance, out int segmentIndex, out float segmentDistance)
    {
        distance = loop ? (distance % length + length) % length : Mathf.Clamp(distance, 0, length);

        float l = 0;
        for (int i = 0; i < SegmentCount; i++)
        {
            var seg = GetSegment(i);
            l += seg.Length;
            if (l >= distance)
            {
                segmentIndex = i;
                segmentDistance = distance - (l - seg.Length);
                return;
            }
        }
        segmentIndex = SegmentCount - 1;
        segmentDistance = GetSegment(segmentIndex).Length;
    }

    /// average the up vector around segment
    private void AverageUpVectorForSegment(int index)
    {
        AverageUpVector(LoopIndex(index - 1), index);
        AverageUpVector(index, LoopIndex(index + 1));
    }
    /// average the up vector around control point
    private void AverageUpVector(int controlIndex)
    {
        for (int i = -1; i < 2; i++)
        {
            var index = LoopIndex(controlIndex + i);
            AverageUpVector(LoopIndex(index - 1), index);
        }
    }
    /// average the up vector for the whole spline
    private void AverageUpVector()
    {
        for (int i = 1; i < segments.Count; i++)
            AverageUpVector(i - 1, i);
        AverageUpVector(segments.Count - 1, 0);
    }
    private void AverageUpVector(int a, int b) => Segment.AverageUpVectors(segments[a], segments[b]);
    /// get the closest spline segment to the given point
    private void GetClosestLength(Vector3 point, out int segmentIndex, out float t)
    {
        //get the closest three segments (I think three is enough)
        List<(int, float)> segmentsLookup = new List<(int, float)>(SegmentCount);
        for (int i = 0; i < SegmentCount; i++)
        {
            var seg = this[i];
            var pointOnBox = seg.Bounds.ClosestPoint(point);
            segmentsLookup.Add((i, Vector3.Distance(pointOnBox, point)));
        }
        segmentsLookup.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        //iterate the closest three segments to find the closest point between them
        var dist = float.MaxValue;
        t = 0f;
        segmentIndex = 0;
        for (int i = 0; i < Mathf.Min(3, SegmentCount); i++)
        {
            var si = segmentsLookup[i].Item1;
            var s = this[si];
            var st = s.GetClosestPoint(point);
            var d = Vector3.Distance(s.GetPoint(st), point);
            if (d < dist)
            {
                segmentIndex = si;
                t = st;
                dist = d;
            }
        }
    }
    private int LoopIndex(int index)
    {
        return (index + points.Count) % points.Count;
    }
    /// update spline arc length
    private void UpdateLength() => UpdateLength(loop);
    /// update spline arc length
    /// <param name="loop">count for loop?</param>
    private void UpdateLength(bool loop)
    {
        length = 0;
        for (int i = 0; i < (loop ? segments.Count : segments.Count - 1); i++)
            length += GetSegment(i).Length;
    }
}