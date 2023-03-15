using System.Collections.Generic;
using UnityEngine;

namespace AlaslTools
{

    public static class PolyMath
    {
        const float cos120 = -0.5f;
        const float sin120 = 0.8660254037844f;
        const float third = 1 / 3f;

        public static void PrintArray(float[] poly)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");
            for (int i = 0; i < poly.Length - 1; i++)
            {
                builder.Append(poly[i]);
                builder.Append(",");
            }
            builder.Append(poly[poly.Length - 1]);
            builder.Append("}");
            Debug.Log(builder);
        }

        public static float EvaluatePoly(float[] coeff, float t)
        {
            float value = coeff[coeff.Length - 1];
            float m = t;
            for (int i = coeff.Length - 2; i >= 0; i--)
            {
                value += m * coeff[i];
                m *= t;
            }
            return value;
        }

        public static float[] PolyDerv(float[] coeff)
        {
            float[] derv = new float[coeff.Length - 1];
            float order = coeff.Length - 1;
            for (int i = 0; i < coeff.Length - 1; i++)
                derv[i] = coeff[i] * order--;
            return derv;
        }

        public static float HybridNewton(
            float l, float r,
            System.Func<float, float> function,
            System.Func<float, float> dfunction,
            float tol = 0.0001f)
        {
            float xl, xh;
            if (function(l) < function(r))
            {
                xl = l;
                xh = r;
            }
            else
            {
                xl = r;
                xh = l;
            }

            float lm = float.MinValue;
            float m = (xl + xh) * 0.5f;
            float f = function(m);
            float df = dfunction(m);

            for (int i = 0; i < 100; i++)
            {
                float dx = Mathf.Abs(xl - xh);

                if (dx < tol)
                    break;

                if (2 * Mathf.Abs(f) > Mathf.Abs(dx * df))
                    m = (xl + xh) * 0.5f;
                else
                {
                    m = m - f / df;
                    if ((xl - m) * (xh - m) > 0)
                        m = (xl + xh) * 0.5f;
                }

                if (m == lm)
                    break;
                lm = m;

                f = function(m);
                if (Mathf.Abs(f) < Mathf.Epsilon) break;
                df = dfunction(m);

                if (f < 0)
                    xl = m;
                else
                    xh = m;
            }
            return m;
        }

        private static void SolveQuad(float[] coeff, float a, float b, List<float> roots)
        {
            float d = coeff[1] * coeff[1] - 4 * coeff[0] * coeff[2];
            if (d < 0)
                return;
            else
            {
                float dsqrt = Mathf.Sqrt(d);
                float inv2a = 0.5f / coeff[0];
                var r = (-coeff[1] + dsqrt) * inv2a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);
                if (d != 0)
                {
                    r = (-coeff[1] - dsqrt) * inv2a;
                    if ((r - a) * (r - b) <= 0)
                        roots.Add(r);
                }
            }
        }

        private static void SolveCubic(float[] coeff, float a, float b, List<float> roots)
        {
            float inva = 1 / coeff[0];
            float invaa = inva * inva;
            float bb = coeff[1] * coeff[1];
            float ac3 = coeff[0] * coeff[2] * 3;

            float p = (ac3 - bb) * invaa * third;
            float halfq = (2 * coeff[1] * bb - 3 * ac3 * coeff[1] +
                27 * coeff[0] * coeff[0] * coeff[3]) * third * third * third * invaa * inva * 0.5f;

            float pover3 = p * third;
            float d = halfq * halfq + pover3 * pover3 * pover3;

            float bover3a = coeff[1] * inva * third;

            if (d > 0)
            {
                var dsqrt = Mathf.Sqrt(d);
                var r = Mathf.Pow(-halfq - dsqrt, third) + Mathf.Pow(-halfq + dsqrt, third) - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);
            }
            else if (d < 0)
            {
                //converting to polar
                float y = Mathf.Sqrt(Mathf.Abs(d));
                var angle = halfq > 0 ? Mathf.Atan(-y / halfq) + Mathf.PI : Mathf.Atan(-y / halfq);
                var rad = Mathf.Sqrt(-d + halfq * halfq);

                angle *= third;
                rad = Mathf.Pow(rad, third);

                float nx = Mathf.Cos(angle) * rad;
                float ny = Mathf.Sin(angle) * rad;

                var r = nx * 2 - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);

                r = 2 * (nx * cos120 - ny * sin120) - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);

                r = 2 * (nx * cos120 + ny * sin120) - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);
            }
            else
            {
                var r = Mathf.Pow(-halfq, third) * 2 - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);

                r = Mathf.Pow(-halfq, third) * 2 * cos120 - bover3a;
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);
            }
        }

        public static List<float> FindRoots(float[] coeff, float a, float b)
        {
            List<float> roots = new List<float>();
            FindRoots(coeff, a, b, roots);
            return roots;
        }

        public static float[] RemoveLeadingZero(float[] coeff)
        {
            int o = LeadingCoeff(0, coeff);
            if (o != 0)
            {
                float[] ncoeff = new float[coeff.Length - o];
                for (int i = 0; i < ncoeff.Length; i++)
                    ncoeff[i] = coeff[i + o];
                return ncoeff;
            }
            else
                return coeff;
        }

        public static void FindRoots(float[] coeff, float a, float b, List<float> roots)
        {
            coeff = RemoveLeadingZero(coeff);
            if (coeff.Length == 2)
            {
                var r = -coeff[1] / coeff[0];
                if ((r - a) * (r - b) <= 0)
                    roots.Add(r);
            }
            else if (coeff.Length == 3)
            {
                SolveQuad(coeff, a, b, roots);
            }
            else if (coeff.Length == 4)
            {
                SolveCubic(coeff, a, b, roots);
            }
            //there is also close forum solution for 4th degree but it's not needed in this project.
            else if (coeff.Length > 1)
            {
                List<float[]> strum = ConstructStrumSeq(coeff);
                FindRoots(strum, a, b, SignChange(strum, a), SignChange(strum, b), roots);
            }
        }

        public static List<float[]> ConstructStrumSeq(float[] coeff)
        {
            List<float[]> strum = new List<float[]>();
            strum.Add(coeff);
            strum.Add(PolyDerv(coeff));

            while (true)
            {
                float[] newPoly = ModPoly(strum[strum.Count - 2], strum[strum.Count - 1]);
                Negate(newPoly);
                strum.Add(newPoly);
                if (newPoly.Length <= 1)
                    break;
            }

            return strum;
        }

        private static void Negate(float[] coeff)
        {
            for (int i = 0; i < coeff.Length; i++)
                coeff[i] = -coeff[i];
        }

        /// <summary>
        /// the reminder for a/b.
        /// </summary>
        private static float[] ModPoly(float[] a, float[] b)
        {
            int offset = 0;
            int op = a.Length - b.Length + 1;

            float[] mod = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
                mod[i] = a[i];

            float binv = -1 / b[0];
            for (int i = 0; i < op; i++)
            {
                var coeff = mod[offset] * binv;
                mod[offset++] = 0;
                for (int j = 0; j < b.Length - 1; j++)
                    mod[j + offset] += coeff * b[j + 1];
            }

            return RemoveLeadingZero(mod);
        }

        private static int LeadingCoeff(int start, float[] coeff)
        {
            for (int i = start; i < coeff.Length; i++)
                if (coeff[i] != 0)
                    return i;
            return coeff.Length - 1;
        }

        public static int SignChange(List<float[]> strum, float x)
        {
            int c = 0;
            float lv = EvaluatePoly(strum[0], x);
            for (int i = 1; i < strum.Count; i++)
            {
                float v = EvaluatePoly(strum[i], x);
                if (lv == 0 || v * lv < 0)
                    c++;
                lv = v;
            }
            return c;
        }

        /// <summary>
        /// finding roots using strum method for isolation and 
        /// Hybrid Newton to converge for the exact root.
        /// </summary>
        private static void FindRoots(List<float[]> strum, float a, float b, int asc, int bsc, List<float> roots, float tol = 0.001f)
        {
            var v = asc - bsc;
            if (v == 0)
                return;
            else if (v == 1)
            {
                var fa = EvaluatePoly(strum[0], a);
                var fb = EvaluatePoly(strum[0], b);

                if (fa * fb < 0)
                    roots.Add(HybridNewton(a, b, (t) => EvaluatePoly(strum[0], t), (t) => EvaluatePoly(strum[1], t), tol));
                else
                {
                    float m = (a + b) * 0.5f;
                    float fm = EvaluatePoly(strum[0], m);
                    if (Mathf.Abs(b - a) < tol || fm == 0)
                    {
                        roots.Add(m);
                        return;
                    }
                    int msc = SignChange(strum, m);
                    if (msc == asc)
                        FindRoots(strum, m, b, msc, bsc, roots);
                    else
                        FindRoots(strum, a, m, asc, msc, roots);
                }
            }
            else
            {
                float m = (a + b) * 0.5f;
                if (Mathf.Abs(b - a) < tol)
                {
                    roots.Add(m);
                    return;
                }
                //TODO : handle when f(m) == 0
                int msc = SignChange(strum, m);
                FindRoots(strum, a, m, asc, msc, roots);
                FindRoots(strum, m, b, msc, bsc, roots);
            }
        }

        public static float Bisection(float a, float b, System.Func<float, float> function, float prec = 0.0001f)
        {
            var m = (a + b) * 0.5f;
            if (Mathf.Abs(b - a) <= prec)
                return m;
            var pm = function(m);
            if (pm == 0)
                return m;

            var pa = function(a);
            if (pa * pm < 0)
                return Bisection(a, m, function, prec);
            else
                return Bisection(m, b, function, prec);
        }
    }

}