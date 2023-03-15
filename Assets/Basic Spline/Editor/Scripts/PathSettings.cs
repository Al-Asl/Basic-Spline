using UnityEditor;
using UnityEngine;
using AlaslTools;

namespace BasicSpline
{
    using static PathSettings;

    public class SettingsSO : BaseSO<PathSettings>
    {
        public SettingsSO(Object target) : base(target) { }
        public SettingsSO(SerializedObject serializedObject) : base(serializedObject) { }

        public Settings settings;
    }

    [CustomEditor(typeof(PathSettings))]
    public class PathSettingsEditor : Editor
    {
        public Settings settings => settingsSO.settings;
        public SettingsSO settingsSO { get; private set; }

        private void OnEnable()
        {
            settingsSO = new SettingsSO(target);
        }

        private void OnDisable()
        {
            settingsSO.Dispose();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            var labelStyle = GUI.skin.label;
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("Draw", labelStyle);

            settings.drawSettings.shape = (SplineDrawSettings.Shape)EditorGUILayout.EnumPopup("Draw Shape", settings.drawSettings.shape);
            settings.drawSettings.resolution = EditorGUILayout.IntField("Draw Resolution", settings.drawSettings.resolution);
            if (settings.drawSettings.shape == SplineDrawSettings.Shape.Road)
                settings.drawSettings.width = EditorGUILayout.FloatField("Road Width", settings.drawSettings.width);
            settings.drawSettings.ColorA = EditorGUILayout.ColorField("Prime Color", settings.drawSettings.ColorA);
            settings.drawSettings.ColorB = EditorGUILayout.ColorField("Secondary Color", settings.drawSettings.ColorB);
            settings.tangentColor = EditorGUILayout.ColorField("Tangent Color", settings.tangentColor);

            EditorGUILayout.LabelField("Controls", labelStyle);

            settings.smartHandleMode = EditorGUILayout.Toggle(new GUIContent("Smart handle", "the smart handle will project it self to scene geometry," +
                " if the projection failed then it will project to the best aligned world plane to camera direction"), settings.smartHandleMode);
            if (settings.smartHandleMode)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(12);
                GUILayout.BeginVertical();

                settings.fallBack = (SceneRaycast.RayCastFallBack)EditorGUILayout.EnumPopup("FallBack", settings.fallBack);
                settings.RayCastToSceneGeomatry = EditorGUILayout.Toggle("Scene Geo Proj", settings.RayCastToSceneGeomatry);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            settings.PrioritizeOnSelection = EditorGUILayout.Toggle("Prioritize Selection", settings.PrioritizeOnSelection);
            settings.lockTangentByDefault = EditorGUILayout.Toggle(new GUIContent("Lock Tangent", "lock the tangent for the newly created points"), settings.lockTangentByDefault);
            settings.showAngleHandle = EditorGUILayout.Toggle("Show Angle Handle", settings.showAngleHandle);

            EditorGUILayout.LabelField("Icons", labelStyle);

            settings.screenSpaceIcon = EditorGUILayout.Toggle("Screen Space icon", settings.screenSpaceIcon);
            settings.contextMenuButtonSize = EditorGUILayout.IntField("Context Button Size", settings.contextMenuButtonSize);
            settings.controlPointIconSize = EditorGUILayout.FloatField("Control Point Size", settings.controlPointIconSize);

            if (EditorGUI.EndChangeCheck())
            {
                SceneRaycast.SmartRaycastSettings.SceneGeoRayCast = settings.RayCastToSceneGeomatry;
                SceneRaycast.SmartRaycastSettings.fallBack = settings.fallBack;
                SceneView.RepaintAll();
                settingsSO.Apply();
            }
        }
    }

    public class PathSettings : ScriptableObject
    {
        private static PathSettings instance;

        public static PathSettings GetSettings()
        {
            if (instance == null)
            {
                var scriptDir = System.IO.Path.Combine(EditorHelper.GetAssemblyDirectory<PathEditor>(), "Resources");
                var path = System.IO.Path.Combine(scriptDir, "Settings.asset");
                instance = AssetDatabase.LoadAssetAtPath<PathSettings>(path);
                if (instance == null)
                {
                    instance = CreateInstance<PathSettings>();
                    AssetDatabase.CreateAsset(instance, path);
                }
            }
            return instance;
        }

        [System.Serializable]
        public class Settings
        {
            public SplineDrawSettings drawSettings;
            public Color tangentColor;
            [Space]
            public bool localEdit;
            public bool relativeTangentEdit;
            [Space]
            public bool smartHandleMode;
            public SceneRaycast.RayCastFallBack fallBack;
            public bool RayCastToSceneGeomatry;
            public bool PrioritizeOnSelection;
            public bool lockTangentByDefault;
            public bool showAngleHandle;
            public bool showTangents;
            [Space]
            public bool screenSpaceIcon;
            public int contextMenuButtonSize;
            public float controlPointIconSize;
            public float tangentIconSize => controlPointIconSize * 0.75f;

        }

        [SerializeField]
        public Settings settings =
            new Settings()
            {
                drawSettings = new SplineDrawSettings(30, 0.2f,
                    SplineDrawSettings.Shape.Road, Color.green, Color.yellow),
                tangentColor = Color.cyan,
                showAngleHandle = true,
                lockTangentByDefault = true,
                smartHandleMode = true,
                fallBack = SceneRaycast.RayCastFallBack.CameraAlignPlane,
                RayCastToSceneGeomatry = true,
                PrioritizeOnSelection = true,
                screenSpaceIcon = true,
                contextMenuButtonSize = 35,
                localEdit = true,
                relativeTangentEdit = true,
                controlPointIconSize = 20f,
            };
    }

}