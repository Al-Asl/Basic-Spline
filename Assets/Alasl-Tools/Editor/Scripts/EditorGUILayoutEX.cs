using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace AlaslTools
{
    public static class EditorGUILayoutEX
    {
        public struct MultiValueFieldResult<T>
        {
            public bool didChange;
            public T value;

            public MultiValueFieldResult(T value)
            {
                didChange = true;
                this.value = value;
            }
        }

        public static MultiValueFieldResult<T> MultiValueField<T>(IEnumerable<T> values, Func<T, T> GUIFunc)
        {
            bool sameValue = true;
            var firstValue = values.First();
            foreach (var value in values)
                if (!EqualityComparer<T>.Default.Equals(value, firstValue))
                {
                    sameValue = false;
                    break;
                }
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = !sameValue;
            firstValue = GUIFunc(firstValue);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
                return new MultiValueFieldResult<T>(firstValue);
            else
                return new MultiValueFieldResult<T>();
        }

        public struct VectorChange
        {
            public int index;
            public float value;

            public VectorChange(int index, float value)
            {
                this.index = index;
                this.value = value;
            }

            public Vector3 Apply(Vector3 vec)
            {
                vec[index] = value;
                return vec;
            }
        }

        public static MultiValueFieldResult<VectorChange> MultiVector3Field(string name, IEnumerable<Vector3> values)
        {
            var firstValue = values.First();
            bool sameX = true, sameY = true, sameZ = true;
            foreach (var v in values)
            {
                sameX &= firstValue.x == v.x;
                sameY &= firstValue.y == v.y;
                sameZ &= firstValue.z == v.z;
            }

            Vector3 value = firstValue;

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(name, GUILayout.Width(45), GUILayout.ExpandWidth(true));

            EditorGUI.showMixedValue = !sameX;
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 11;
            value.x = FloatField("X", value.x, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
            EditorGUI.showMixedValue = !sameY;
            value.y = FloatField("Y", value.y, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
            EditorGUI.showMixedValue = !sameZ;
            value.z = FloatField("Z", value.z, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
            EditorGUI.showMixedValue = false;
            EditorGUIUtility.labelWidth = oldWidth;

            GUILayout.EndHorizontal();

            if (value.x != firstValue.x)
                return new MultiValueFieldResult<VectorChange>(new VectorChange(0, value[0]));
            else if (value.y != firstValue.y)
                return new MultiValueFieldResult<VectorChange>(new VectorChange(1, value[1]));
            else if (value.z != firstValue.z)
                return new MultiValueFieldResult<VectorChange>(new VectorChange(2, value[2]));

            return new MultiValueFieldResult<VectorChange>();
        }

        public static void AlignCenter(Action Draw)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Draw();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static bool Button(string name, bool disable, params GUILayoutOption[] options)
        {
            if (disable)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button(name, options);
                EditorGUI.EndDisabledGroup();
                return false;
            }
            else
                return GUILayout.Button(name, options);
        }

        public static float FloatField(string name, float value, int pow = 2, params GUILayoutOption[] options)
        {
            float bnum = Mathf.Pow(10, pow);
            var input = Mathf.Round(value * bnum) / bnum;
            var output = EditorGUILayout.FloatField(name, input, options);
            return value + (output - input);
        }

        public static Vector3 VectorField(string name, Vector3 value, int pow = 2, params GUILayoutOption[] options)
        {
            float bnum = Mathf.Pow(10, pow);
            var input = new Vector3(
                Mathf.Round(value.x * bnum) / bnum,
                Mathf.Round(value.y * bnum) / bnum,
                Mathf.Round(value.z * bnum) / bnum);
            Vector3 output = default;
            if (!string.IsNullOrEmpty(name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, options);
                output = EditorGUILayout.Vector3Field("", input);
                EditorGUILayout.EndHorizontal();
            }
            else
                output = EditorGUILayout.Vector3Field("", input);
            return value + (output - input);
        }
    }
}