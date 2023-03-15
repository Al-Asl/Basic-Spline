using UnityEngine;

namespace BasicSpline
{

    [System.Serializable]
    public class SplineDrawSettings
    {
        public static SplineDrawSettings current;

        public enum Shape
        {
            Line,
            Road
        }

        public int resolution;
        public Shape shape;
        public Color ColorA, ColorB;
        public float width;

        public SplineDrawSettings(int resolution, float width, Shape shape, Color colorA, Color colorB)
        {
            this.resolution = resolution;
            this.shape = shape;
            this.width = width;
            ColorA = colorA;
            ColorB = colorB;
        }
    }

}