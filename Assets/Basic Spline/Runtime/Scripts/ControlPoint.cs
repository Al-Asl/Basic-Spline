using UnityEngine;

namespace BasicSpline
{

    public static class TangentModeExtenstion
    {
        public static TangentMode Next(this TangentMode mode)
        {
            return (TangentMode)(((int)mode + 1) % 2);
        }
    }

    public enum TangentMode
    {
        Free = 0,
        Lock = 1
    }

    [System.Serializable]
    public struct ControlPoint
    {
        public Vector3 point;
        public Vector3 inTangent, outTangent;
        public float angle;
#if UNITY_EDITOR
        public TangentMode editor_only_tangent_mode;
#endif

        public ControlPoint(Vector3 point, Vector3 inTangent, Vector3 outTangent, float angle = 0)
        {
            this.point = point;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
            this.angle = angle;
#if UNITY_EDITOR
            editor_only_tangent_mode = TangentMode.Free;
#endif
        }

        public void SetTangent(Vector3 tangent, bool inTangent, TangentMode tangentMode)
        {
            SetTangent(ref this, tangent, inTangent, tangentMode);
        }

        public static void SetTangent(ref ControlPoint cp, Vector3 tangent, bool inTangent, TangentMode tangentMode)
        {
            if (tangentMode == TangentMode.Lock)
            {
                if (inTangent)
                    SetLockTangent(tangent, cp.point, out cp.inTangent, out cp.outTangent);
                else
                    SetLockTangent(tangent, cp.point, out cp.outTangent, out cp.inTangent);
            }
            else if (tangentMode == TangentMode.Free)
            {
                if (inTangent)
                    cp.inTangent = tangent;
                else
                    cp.outTangent = tangent;
            }
        }

        private static void SetLockTangent(Vector3 value, Vector3 center, out Vector3 a, out Vector3 b)
        {
            a = value; b = 2 * center - a;
        }

        /// <summary>
        /// move the point with tangents
        /// </summary>
        public void Set(Vector3 point)
        {
            var delta = point - this.point;
            Move(delta);
        }

        /// <summary>
        /// move the point with tangents
        /// </summary>
        public void Move(Vector3 delta)
        {
            point += delta;
            inTangent += delta;
            outTangent += delta;
        }

        public static bool operator ==(ControlPoint a, ControlPoint b)
        {
            return (a.point == b.point) && (a.inTangent == b.inTangent) && (a.outTangent == b.outTangent);
        }

        public static bool operator !=(ControlPoint a, ControlPoint b)
        {
            return !(a == b);
        }
    }
}