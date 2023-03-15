using UnityEngine;
using System.Collections.Generic;
using AlaslTools;

namespace BasicSpline
{

    [System.Serializable]
    public struct Poly3x3
    {
        [SerializeField]
        public Vector3 a, b, c, d;

        public Poly3x3(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Vector3 Evaluate(float t)
        {
            float tt = t * t;
            return t * tt * a + tt * b + t * c + d;
        }

        public Vector3 EvaluateD1(float t)
        {
            return 3 * t * t * a + 2 * t * b + c;
        }

        public Vector3 EvaluateD2(float t)
        {
            return 6 * t * a + 2 * b;
        }
    }

    public struct CurvePoints
    {
        public Vector3 a, aTan, bTan, b;

        public CurvePoints(Vector3 a, Vector3 aTan, Vector3 bTan, Vector3 b)
        {
            this.a = a;
            this.aTan = aTan;
            this.bTan = bTan;
            this.b = b;
        }
    }

    public static class CurveUtility
    {
        const float third = 1 / 3f;
        const float twothird = 2 / 3f;

        /// <summary>
        /// the partial polynomial p`.|p-a| = 0 where p is 
        /// the curve polynomial and a is the point,
        /// this is only the partial polynomial.
        /// </summary>
        public static float[] ClosestPointPoly(Poly3x3 curve)
        {
            return new float[]
            {
            3 * Vector3.Dot(curve.a, curve.a),
            5 * Vector3.Dot(curve.a, curve.b),
            4 * Vector3.Dot(curve.a, curve.c) + 2 * Vector3.Dot(curve.b, curve.b),
            3 * Vector3.Dot(curve.a, curve.d) + 3 * Vector3.Dot(curve.b, curve.c),
            2 * Vector3.Dot(curve.b, curve.d) + Vector3.Dot(curve.c, curve.c),
            Vector3.Dot(curve.c, curve.d)
            };
        }

        /// <summary>
        /// finding the parameter t for the closest point on the curve to the given point
        /// </summary>
        /// [ Xiao-Diao Chen, Yin Zhou, Zhenyu Shu, Hua Su, Jean-Claude Paul. 
        /// Improved Algebraic Algorithm On Point Projection For Bézier Curves. HAL, 2007 ]
        public static float GetClosestPoint(float[] closestPointPoly, Poly3x3 curve, Vector3 point)
        {
            //construct the polynomial and find its roots
            //p`.|p-a| = 0, where p is the curve polynomial, and a is the point
            float[] poly = new float[]
            {
            closestPointPoly[0],
            closestPointPoly[1],
            closestPointPoly[2],
            closestPointPoly[3] - 3 * Vector3.Dot(curve.a, point),
            closestPointPoly[4] - 2 * Vector3.Dot(curve.b, point),
            closestPointPoly[5] - Vector3.Dot(curve.c, point),
            };
            var roots = new List<float>() { 0, 1 };
            FindValidRoots(poly, roots);

            //get the closest root
            float minDist = float.MaxValue;
            float mint = default;
            for (int i = 0; i < roots.Count; i++)
            {
                var t = roots[i];
                var d = (curve.Evaluate(t) - point).sqrMagnitude;
                if (d < minDist)
                {
                    minDist = d;
                    mint = t;
                }
            }

            return mint;
        }

        /// <summary>
        /// finding roots using strum sequence, only the local minimum
        /// </summary>
        private static void FindValidRoots(float[] poly, List<float> roots)
        {
            var strum = PolyMath.ConstructStrumSeq(poly);
            FindRoots(strum, 0, 1f, PolyMath.SignChange(strum, 0), PolyMath.SignChange(strum, 1f), roots);
        }

        /// <summary>
        /// finding roots using strum sequence, only the local minimum
        /// </summary>
        private static void FindRoots(List<float[]> strum, float a, float b, int asc, int bsc, List<float> roots, float tol = 0.001f)
        {
            var v = asc - bsc;
            if (v == 0)
                return;
            else if (v == 1)
            {
                var fa = PolyMath.EvaluatePoly(strum[0], a);
                var fb = PolyMath.EvaluatePoly(strum[0], b);

                //only interested in the local minimum
                if (fa < fb)
                    roots.Add(PolyMath.HybridNewton(a, b,
                        (t) => PolyMath.EvaluatePoly(strum[0], t),
                        (t) => PolyMath.EvaluatePoly(strum[1], t), tol));
            }
            else
            {
                float m = (a + b) * 0.5f;
                if ((b - a) < tol)
                {
                    var fa = PolyMath.EvaluatePoly(strum[0], a);
                    var fb = PolyMath.EvaluatePoly(strum[0], b);
                    if (fa < fb)
                        roots.Add(m);
                    return;
                }
                int msc = PolyMath.SignChange(strum, m);
                FindRoots(strum, a, m, asc, msc, roots);
                FindRoots(strum, m, b, msc, bsc, roots);
            }
        }

        /// <summary>
        /// the arc length for a given parameter,
        /// using legendre-gauss with n = 5
        /// </summary>
        //https://pomax.github.io/bezierinfo/legendre-gauss.html
        public static float GetArcLengthLegendre5(Poly3x3 curve, float t)
        {
            float ht = t * 0.5f;
            Vector3 a = 3 * curve.a;
            Vector3 b = 2 * curve.b;
            Vector3 c = curve.c;
            System.Func<float, float> Evaluate = (t0) => (t0 * t0 * a + t0 * b + c).magnitude;
            return ht * (
                0.5688888888888889f * Evaluate(ht) +
                0.4786286704993665f * Evaluate(0.4615306898943169f * ht) +
                0.4786286704993665f * Evaluate(1.5384693101056831f * ht) +
                0.2369268850561891f * Evaluate(0.093820154061336f * ht) +
                0.2369268850561891f * Evaluate(1.9061798459386640f * ht));
        }

        /// <summary>
        /// the arc length for a given parameter,
        /// using legendre-gauss with n = 8
        /// </summary>
        //https://pomax.github.io/bezierinfo/legendre-gauss.html
        public static float GetArcLengthLegendre8(Poly3x3 curve, float t)
        {
            float ht = t * 0.5f;
            Vector3 a = 3 * curve.a;
            Vector3 b = 2 * curve.b;
            Vector3 c = curve.c;
            System.Func<float, float> Evaluate = (t0) => (t0 * t0 * a + t0 * b + c).magnitude;
            return ht * (
                0.3626837833783620f * Evaluate(0.8165653575043502f * ht) +
                0.3626837833783620f * Evaluate(1.1834346424956498f * ht) +
                0.3137066458778873f * Evaluate(0.474467590083671f * ht) +
                0.3137066458778873f * Evaluate(1.5255324099163290f * ht) +
                0.2223810344533745f * Evaluate(0.2033335225863733f * ht) +
                0.2223810344533745f * Evaluate(1.7966664774136267f * ht) +
                0.1012285362903763f * Evaluate(0.0397101435024637f * ht) +
                0.1012285362903763f * Evaluate(1.9602898564975363f * ht));
        }

        /// <summary>
        /// the parameter for a given arc length
        /// </summary>
        public static float GetParameter(Poly3x3 curve, float arcLenght)
        {
            Vector3 a = 3 * curve.a;
            Vector3 b = 2 * curve.b;
            Vector3 c = curve.c;
            System.Func<float, float> d = (t) => (t * t * a + t * b + c).magnitude;
            return PolyMath.HybridNewton(0, 1f, (t) => GetArcLengthLegendre5(curve, t) - arcLenght, d);
        }

        //this is an inverse polynomial for
        //[Approximate Arc Length Parametrization MARCELO WALTER ,AND ALAIN FOURNIER]
        public static float[] ParamerterPoly(Poly3x3 curve)
        {
            float l = GetArcLengthLegendre5(curve, 1f);
            float invl = 1f / l;
            float invll = invl * invl;
            float t1 = GetParameter(curve, l * third);
            float t2 = GetParameter(curve, l * twothird);
            return new float[]
            {
            (13.5f*t1 - 13.5f*t2 + 4.5f)*invll*invl,
            (-22.5f*t1 + 18f*t2 - 4.5f)*invll,
            (9*t1 - 4.5f*t2 + 1)*invl
            };
        }

        /// <summary>
        /// return a polynomial that's used for calculating arc length for a given parameter t
        /// </summary>
        /// [Approximate Arc Length Parametrization MARCELO WALTER ,AND ALAIN FOURNIER]
        public static float[] ArcLengthPoly(Poly3x3 curve)
        {
            float l3 = GetArcLengthLegendre5(curve, 1f);
            float l1 = GetArcLengthLegendre5(curve, third);
            float l2 = GetArcLengthLegendre5(curve, twothird);
            return new float[] 
            {
                13.5f * l1 - 13.5f * l2 + 4.5f * l3,
                -22.5f * l1 + 18 * l2 - 4.5f * l3,
                9 * l1 - 4.5f * l2 + l3
            };
        }

        /// <summary>
        /// convert from polynomial form to control points
        /// </summary>
        public static CurvePoints GetPoints(Poly3x3 poly)
        {
            CurvePoints res = new CurvePoints();
            res.a = poly.d;
            res.aTan = third * poly.c + poly.d;
            res.bTan = third * (poly.b + poly.c) + res.aTan;
            res.b = poly.a + poly.b + poly.c + poly.d;
            return res;
        }

        /// <summary>
        /// convert from control points to polynomial form
        /// </summary>
        public static Poly3x3 GetPoly(CurvePoints points)
        {
            return GetPoly(points.a, points.aTan, points.bTan, points.b);
        }

        /// <summary>
        /// convert from control points to polynomial form
        /// </summary>
        public static Poly3x3 GetPoly(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var c = 3 * (p1 - p0);
            var b = 3 * (p2 - p1) - c;
            return new Poly3x3(p3 - p0 - c - b, b, c, p0);
        }

        /// <summary>
        /// evaluate the angle of the up vector
        /// </summary>
        public static float GetAngle(Poly3x3 curve, Vector3 up, float t)
        {
            return Vector3.SignedAngle(GetUpVector(curve, t), up, curve.EvaluateD1(t));
        }

        /// <summary>
        /// evaluate the up vector for the curve
        /// </summary>
        public static Vector3 GetUpVector(Poly3x3 curve, float aa, float ab, float t)
        {
            return Quaternion.AngleAxis(Mathf.Lerp(aa, ab, t), curve.EvaluateD1(t)) * GetUpVector(curve, t);
        }

        //blend between the d2 and the world up based on the d1.(world up)
        private static Vector3 GetUpVector(Poly3x3 curve, float t)
        {
            return Vector3.Lerp(
                Vector3.up,
                curve.EvaluateD2(t).normalized,
                Mathf.Abs(Mathf.Asin(curve.EvaluateD1(t).normalized.y) / Mathf.PI * 2));
        }

        /// <summary>
        /// evaluate curve boundary.
        /// </summary>
        public static Bounds EvaluateBounds(Poly3x3 curve)
        {
            //Encapsulate start and end points
            Bounds res = new Bounds(curve.a + curve.b + curve.c + curve.d, Vector3.zero);
            res.Encapsulate(curve.d);

            //Encapsulate points where d1 equal zero
            var roots = new List<float>();
            PolyMath.FindRoots(new float[] { 3 * curve.a.x, 2 * curve.b.x, curve.c.x }, 0, 1f, roots);
            PolyMath.FindRoots(new float[] { 3 * curve.a.y, 2 * curve.b.y, curve.c.y }, 0, 1f, roots);
            PolyMath.FindRoots(new float[] { 3 * curve.a.z, 2 * curve.b.z, curve.c.z }, 0, 1f, roots);
            for (int i = 0; i < roots.Count; i++)
                res.Encapsulate(curve.Evaluate(roots[i]));

            return res;
        }

        /// <summary>
        /// return true if Inflexion point exist
        /// </summary>
        /// [Approximate Arc Length Parametrization MARCELO WALTER ,AND ALAIN FOURNIER]
        public static bool InflexionPoint(Poly3x3 curve, out float t)
        {
            t = 0;
            float[] poly = new float[]
            {
                18 * Vector3.Dot(curve.a, curve.a),
                18 * Vector3.Dot(curve.a, curve.b),
                6  * Vector3.Dot(curve.a, curve.c) + 4 * Vector3.Dot(curve.b, curve.b),
                2  * Vector3.Dot(curve.b, curve.c)
            };
            var roots = PolyMath.FindRoots(poly, 0, 1f);
            if (roots.Count > 1)
            {
                roots.Sort();
                if (roots.Count > 2)
                    t = roots[1];
                else
                    t = (roots[0] + roots[1]) * 0.5f;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// split the curve into two segment based on a given parameter.
        /// </summary>
        //https://en.wikipedia.org/wiki/De_Casteljau%27s_algorithm
        public static void Split(CurvePoints target, float t, out CurvePoints a, out CurvePoints b)
        {
            var m = Vector3.Lerp(target.aTan, target.bTan, t);
            var aa = Vector3.Lerp(target.a, target.aTan, t);
            var bb = Vector3.Lerp(target.bTan, target.b, t);
            var ab = Vector3.Lerp(aa, m, t);
            var ba = Vector3.Lerp(m, bb, t);
            var mm = Vector3.Lerp(ab, ba, t);
            a = new CurvePoints(target.a, aa, ab, mm);
            b = new CurvePoints(mm, ba, bb, target.b);
        }

        /// <summary>
        /// return the arc length to a given parameter, using adaptive subdivision.
        /// </summary>
        public static float ArcLengthSubdivision(CurvePoints target, float t, int depth = 3)
        {
            Split(target, t, out var a, out var b);
            return ArcLength(a, depth);
        }

        private static float ArcLength(CurvePoints target, int depth = 3)
        {
            depth--;
            if (depth == 0)
                return (Vector3.Distance(target.a, target.b) +
                    Vector3.Distance(target.a, target.aTan) +
                    Vector3.Distance(target.aTan, target.bTan) +
                    Vector3.Distance(target.aTan, target.b)) * 0.5f;
            Split(target, 0.5f, out var segA, out var segB);
            return ArcLength(segA, depth) + ArcLength(segB, depth);
        }
    }
}