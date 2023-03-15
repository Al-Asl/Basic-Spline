using System.Collections.Generic;
using UnityEngine;
using AlaslTools;
using System.Collections;

namespace BasicSpline
{
    public struct Sample
    {
        public Matrix4x4 localToWorld;

        public Vector3 right => localToWorld.GetColumn(0);
        public Vector3 up => localToWorld.GetColumn(1);
        public Vector3 forward => localToWorld.GetColumn(2);
        public Vector3 point => localToWorld.GetColumn(3);

        public Sample(Vector3 point, Vector3 forward, Vector3 right, Vector3 up) : this()
        {
            localToWorld = new Matrix4x4(right, up, forward, point);
            localToWorld.m33 = 1f;
        }

        public Sample Transform(Transform transform) =>
            Transform(transform.localToWorldMatrix);

        public Sample Transform(Matrix4x4 matrix)
        {
            localToWorld = matrix * localToWorld;
            return this;
        }

        public void Apply(Transform transform)
        {
            transform.rotation = Quaternion.LookRotation(forward, up);
            transform.position = point;
        }
    }

    /// <summary>
    /// spline segment based on Bézier curve
    /// </summary>
    [System.Serializable]
    public class Segment
    {
        public Bounds Bounds => bounds;
        public float Length => lenght;

        [SerializeField, HideInInspector]
        private Poly3x3 curve;

        [SerializeField, HideInInspector]
        private float lenght;

        [SerializeField, HideInInspector]
        private float InflexionPoint, InflexionLength;
        [SerializeField, HideInInspector]
        private float[] t2l;

        [SerializeField, HideInInspector]
        private float[] closestPointPoly;

        [SerializeField, HideInInspector]
        private Bounds bounds;

        [SerializeField, HideInInspector]
        private Vector3 aUp, bUp;

        public static void AverageUpVectors(Segment a, Segment b)
        {
            var m = Vector3.Slerp(a.bUp, b.aUp, 0.5f);
            a.bUp = m; b.aUp = m;
        }

        public Segment(ControlPoint a, ControlPoint b) :
        this(new CurvePoints(a.point, a.outTangent, b.inTangent, b.point)
            , a.angle, b.angle)
        { }

        public Segment(CurvePoints points, float aAngle = 0, float bAngle = 0)
        {
            curve = CurveUtility.GetPoly(points);

            aUp = CurveUtility.GetUpVector(curve, aAngle, bAngle, 0);
            bUp = CurveUtility.GetUpVector(curve, aAngle, bAngle, 1f);

            bounds = CurveUtility.EvaluateBounds(curve);
            closestPointPoly = CurveUtility.ClosestPointPoly(curve);

            if (CurveUtility.InflexionPoint(curve, out InflexionPoint))
            {
                InflexionLength = CurveUtility.GetArcLengthLegendre5(curve, InflexionPoint);
                lenght = CurveUtility.GetArcLengthLegendre5(curve, 1f);

                this.t2l = new float[6];
                CurveUtility.Split(points, InflexionPoint, out var ca, out var cb);
                var t2l = CurveUtility.ArcLengthPoly(CurveUtility.GetPoly(ca));
                for (int i = 0; i < 3; i++)
                    this.t2l[i] = t2l[i];
                t2l = CurveUtility.ArcLengthPoly(CurveUtility.GetPoly(cb));
                for (int i = 0; i < 3; i++)
                    this.t2l[i + 3] = t2l[i];

            }
            else
            {
                t2l = CurveUtility.ArcLengthPoly(curve);
                lenght = PolyMath.EvaluatePoly(t2l, 1f);
            }
        }

        /// <summary>
        /// get a curve sample (position and rotation)
        /// </summary>
        Sample GetSample(float t, Vector3 point, Vector3 d1)
        {
            var up = Vector3.Slerp(aUp, bUp, t);
            var forward = d1.normalized;
            var right = Vector3.Cross(up, forward).normalized;
            return new Sample(point, forward, right, Vector3.Cross(forward, right).normalized);
        }
        /// <summary>
        /// evaluate point on the curve
        /// </summary>
        public Vector3 GetPoint(float t) => curve.Evaluate(t);

        /// <summary>
        /// evaluate curve first derivative
        /// </summary>
        public Vector3 GetD1(float t) => curve.EvaluateD1(t);

        /// <summary>
        /// evaluate curve second derivative
        /// </summary>
        public Vector3 GetD2(float t) => curve.EvaluateD2(t);

        /// <summary>
        /// get curve sample (with rotation)
        /// </summary>
        public Sample GetSample(float t) => GetSample(t, GetPoint(t), GetD1(t));

        /// <summary>
        /// split the segment at a given parameter t
        /// </summary>
        public void Split(float t, out CurvePoints a, out CurvePoints b, out float angle)
        {
            CurvePoints points = CurveUtility.GetPoints(curve);
            CurveUtility.Split(points, t, out a, out b);

            //TODO : count for the new up vector
            angle = Mathf.LerpAngle(CurveUtility.GetAngle(curve, aUp, 0),
                CurveUtility.GetAngle(curve, bUp, 1f), t);
        }

        /// <summary>
        /// evaluate the arc length from curve start to the given parameter
        /// </summary>
        public float GetArcLength(float t)
        {
            int offset = 0;
            float ol = 0;
            if (InflexionPoint != 0)
            {
                if (t > InflexionPoint)
                {
                    offset = 3;
                    ol = InflexionLength;
                    t = Mathf.InverseLerp(InflexionPoint, 1f, t);
                }
                else
                    t = Mathf.InverseLerp(0, InflexionPoint, t);
            }
            var tt = t * t;
            return ol + t2l[offset] * tt * t + t2l[offset + 1] * tt + t2l[offset + 2] * t;
        }
        /// <summary>
        /// evaluate the curve parameter for the given arc length
        /// </summary>
        public float GetParameter(float length) =>
            PolyMath.HybridNewton(0, 1f, (t) => GetArcLength(t) - length, GetArcLengthD1);

        /// <summary>
        /// finding the curve parameter of the closest point on the curve to the given point
        /// </summary>
        public float GetClosestPoint(Vector3 point)
            => CurveUtility.GetClosestPoint(closestPointPoly, curve, point);

        /// <summary>
        /// evaluate the arc length derivative at a given parameter
        /// </summary>
        private float GetArcLengthD1(float t)
        {
            int offset = 0;
            if (InflexionPoint != 0)
            {
                if (t > InflexionPoint)
                {
                    offset = 3;
                    t = Mathf.InverseLerp(InflexionPoint, 1f, t);
                }
                else
                    t = Mathf.InverseLerp(0, InflexionPoint, t);
            }
            return t2l[offset] * 3 * t * t + t2l[offset + 1] * 2 * t + t2l[offset + 2];
        }

        /// <summary>
        /// Iterate over the segment points in fixed parametric steps.
        /// </summary>
        //using forward differencing https://en.wikipedia.org/wiki/Finite_difference
        public IEnumerable<Vector3> IteratePoints(int res)
        {
            float s = 1f / res;
            float ss = s * s;
            var a = curve.a * ss * s;
            var b = curve.b * ss;

            var d1 = a + b + curve.c * s;
            var d2 = 6 * a + 2 * b;
            var d3 = 6 * a;

            Vector3 point = curve.d;

            for (int i = 0; i < res + 1; i++)
            {
                yield return point;
                point += d1;
                d1 += d2;
                d2 += d3;
            }
        }

        /// <summary>
        /// Iterate over the segment points in fixed parametric steps.
        /// </summary>
        //using forward differencing https://en.wikipedia.org/wiki/Finite_difference
        public IEnumerable<Sample> IterateSamples(int res)
        {
            float s = 1f / res;
            float ss = s * s;
            var a = curve.a * ss * s;
            var b = curve.b * ss;

            var d1 = a + b + curve.c * s;
            var d2 = 6 * a + 2 * b;
            var d3 = 6 * a;

            Vector3 point = curve.d;

            for (int i = 0; i < res + 1; i++)
            {
                float t = i * s;

                yield return GetSample(t, point, d1);
                point += d1;
                d1 += d2;
                d2 += d3;
            }
        }
    }

}