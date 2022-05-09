using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using static HandleEx;

[CustomEditor(typeof(Path))]
public class PathEditor : Editor
{
    // lazy way to handle serialize object and properties using reflection
    private class PathSO : BaseSO<Path>
    {
        public PathSO(Object target) : base(target) {}
        public PathSO(SerializedObject serializedObject) : base(serializedObject){}

        public Spline spline;
        public Transform transform => target.transform;

        public ControlPoint GetTransformPoint(int index)
        {
            var cpoint = spline.GetControlPoint(index);
            cpoint.point = transform.TransformPoint(cpoint.point);
            cpoint.inTangent = transform.TransformPoint(cpoint.inTangent);
            cpoint.outTangent = transform.TransformPoint(cpoint.outTangent);
            return cpoint;
        }

        public void SetTransformedPoint(int index ,ControlPoint cpoint)
        {
            cpoint.point = transform.InverseTransformPoint(cpoint.point);
            cpoint.inTangent = transform.InverseTransformPoint(cpoint.inTangent);
            cpoint.outTangent = transform.InverseTransformPoint(cpoint.outTangent);
            spline.SetControlPoint(index, cpoint);
        }
    }

    // tool for adding control points
    private class AddingTool
    {
        public bool IsActive => active;
        public int RecentPoint => lastIndex;

        private PathSO path;
        private SettingsSO settingsSO;

        private static int controlID = "Spline/AddingTool".GetHashCode();
        private bool active;

        bool appendLast;
        int lastIndex;

        public AddingTool(PathSO path,SettingsSO settingsSO)
        {
            this.path = path;
            this.settingsSO = settingsSO;
        }

        public void Start(bool appendLast = true)
        {
            active = true;
            this.appendLast = appendLast;

            if (this.appendLast)
                lastIndex = path.spline.ControlPointsCount - 1;
            else
                lastIndex = 0;

            GUIUtility.hotControl = controlID;
            GUIUtility.keyboardControl = controlID;
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (!active || HandleEx.NavKeyDown())
                return;

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        Vector3 pos = path.transform.TransformPoint(path.spline.GetControlPoint(lastIndex).point);
                        pos = path.transform.InverseTransformPoint(SceneRaycast.SmartRaycast(pos));

                        var cpoint = new ControlPoint(pos, pos, pos);

                        cpoint.editor_only_tangent_mode = 
                            settingsSO.settings.lockTangentByDefault ? 
                            TangentMode.Lock : TangentMode.Free;

                        if (appendLast)
                        {
                            path.spline.AddControlPoint(cpoint);
                            lastIndex++;
                        }
                        else
                            path.spline.InsertControlPoint(0, cpoint);

                        path.Apply();
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        var cpoint = path.GetTransformPoint(lastIndex);

                        Vector3 pos = SceneRaycast.SmartRaycast(cpoint.point);

                        cpoint.SetTangent(pos, true, TangentMode.Lock);
                        path.SetTransformedPoint(lastIndex, cpoint);

                        path.Apply();
                        Event.current.Use();
                    }
                    break;
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, 0);
                    break;
            }
        }

        public void Stop()
        {
            active = false;

            if (GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
            }
        }
    }

    private Spline spline => path.spline;
    private PathSettings.Settings settings => settingsSO.settings;

    private PathSO path;
    private SettingsSO settingsSO;
    private AddingTool addingTool;

    private static Dictionary<string, Texture2D> texture_lookup = new Dictionary<string, Texture2D>();

    private Texture2D worldspace_icon;
    private Texture2D localspace_icon;
    private Texture2D settings_icon;

    private Texture2D controlPoint_normal_texture;
    private Texture2D controlPoint_hover_texture;
    private Texture2D controlPoint_active_texture;

    private GUIStyle delete_button_style;
    private GUIStyle lock_on_button_style;
    private GUIStyle lock_off_button_style;
    private GUIStyle loop_on_button_style;
    private GUIStyle loop_off_button_style;
    private GUIStyle tangents_on_button_style;
    private GUIStyle tangents_off_button_style;
    private GUIStyle add_button_style;

    private const float sceneMenuOffset = 30f;
    private const float InspectorButtonSize = 25;

    private const double CancelClickThreshold = 0.15;
    private double lastCancelClickTime;
    private bool cancel;

    private int selectedPoint;
    private static HashSet<int> selectedPoints = new HashSet<int>();
    private static bool inSelection;
    private static Vector2 selectionStartPosition;

    private static Tool tool;
    private Quaternion rotation;
    private Vector3 scale;

    private bool settingsOpened;
    private Editor SettingsEditor;

    private DragHandle dragHandle = new DragHandle();
    private ButtonHandle buttonHandle = new ButtonHandle();
    private ArcHandle arcHandle = new ArcHandle();

    private void OnEnable()
    {
        localspace_icon = GetTexture("localSpace_icon.png");
        worldspace_icon = GetTexture("worldSpace_icon.png");
        settings_icon = GetTexture("settings_icon.png");

        controlPoint_normal_texture = GetTexture("controlPoint_normal.png");
        controlPoint_hover_texture = GetTexture("controlPoint_hover.png");
        controlPoint_active_texture = GetTexture("controlPoint_active.png");

        delete_button_style = GetButtonStyle("delete_normal.png", "delete_hover.png");
        lock_on_button_style = GetButtonStyle("lock_on_normal.png", "lock_on_hover.png");
        lock_off_button_style = GetButtonStyle("lock_off_normal.png", "lock_off_hover.png");
        loop_on_button_style = GetButtonStyle("loop_on_normal.png", "loop_on_hover.png");
        loop_off_button_style = GetButtonStyle("loop_off_normal.png", "loop_off_hover.png");
        add_button_style = GetButtonStyle("add_normal.png", "add_hover.png");
        tangents_on_button_style = GetButtonStyle("tangents_on_normal.png", "tangents_on_hover.png");
        tangents_off_button_style = GetButtonStyle("tangents_off_normal.png", "tangents_off_hover.png");

        path = new PathSO(target);
        settingsSO = new SettingsSO(PathSettings.GetSettings());
        addingTool = new AddingTool(path,settingsSO);

        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.beforeSceneGui += BeforeSceneGUI;

        SettingsEditor = CreateEditor(PathSettings.GetSettings(), typeof(PathSettingsEditor));
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.beforeSceneGui -= BeforeSceneGUI;

        settingsSO.Dispose();
        path.Dispose();

        if ((Selection.activeObject == null || settings.PrioritizeOnSelection)
            && selectedPoints.Count > 0)
            Selection.activeObject = target;
        else
            selectedPoints.Clear();

        inSelection = false;
        DestroyImmediate(SettingsEditor);
    }

    //process input
    private void BeforeSceneGUI(SceneView sceneView)
    {
        if (Event.current == null)
            return;

        //send cancel signal on right click
        cancel = false;
        switch (Event.current.type)
        {
            case EventType.MouseDown:
                if (Event.current.button == 1)
                    lastCancelClickTime = EditorApplication.timeSinceStartup;
                break;
            case EventType.MouseUp:
                if (Event.current.button == 1 &&
                    (EditorApplication.timeSinceStartup - lastCancelClickTime) < CancelClickThreshold)
                    cancel = true;
                break;
        }

        // process the selection
        ProcessSelection();
    }

    public override void OnInspectorGUI()
    {
        List<int> indices = new List<int>();
        if (selectedPoints.Count > 0)
            foreach (var p in selectedPoints)
                indices.Add(p);
        else
            indices.Add(selectedPoint);

        bool setInTangent = false, setOutTangent = false;
        List<Vector3> points = new List<Vector3>();
        List<Vector3> inTangents = new List<Vector3>();
        List<Vector3> outTangents = new List<Vector3>();
        List<float> angles = new List<float>();
        List<TangentMode> tangentsMode = new List<TangentMode>();

        for (int i = 0; i < indices.Count; i++)
        {
            var cpoint = path.spline.GetControlPoint(indices[i]);
            points.Add(cpoint.point);
            inTangents.Add(cpoint.inTangent);
            outTangents.Add(cpoint.outTangent);
            angles.Add(cpoint.angle);
            tangentsMode.Add(cpoint.editor_only_tangent_mode);
        }

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        var buttonStyle = InspectorButtonStyle();

        var applyPath = false;
        var applySettings = false;

        if (spline.ControlPointsCount != 1 && GUILayout.Button(delete_button_style.normal.background, buttonStyle))
        {
            spline.RemoveControlPoint(selectedPoint);
            selectedPoint = Mathf.Clamp(selectedPoint, 0, spline.ControlPointsCount - 1);
            applyPath = true;
        }

        if (MultiValueField(tangentsMode, (tmode) =>
        {
            if (GUILayout.Button(tmode == TangentMode.Lock ?
                  lock_on_button_style.normal.background : 
                  lock_off_button_style.normal.background, buttonStyle))
                return tmode.Next();
            else
                return tmode;
        }))
        {
            setInTangent = true;
            applyPath = true;
        }

        if (GUILayout.Button(
            spline.Loop ?
            loop_on_button_style.normal.background : loop_off_button_style.normal.background, buttonStyle))
        {
            spline.Loop = !spline.Loop;
            applyPath = true;
        }

        if (GUILayout.Button(
            settings.showTangents ?
            tangents_on_button_style.normal.background : tangents_off_button_style.normal.background, buttonStyle))
        {
            settings.showTangents = !settings.showTangents;
            applySettings = true;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(settings.localEdit ? localspace_icon : worldspace_icon, buttonStyle))
        {
            settings.localEdit = !settings.localEdit;
            applySettings = true;
        }

        settingsOpened = GUILayout.Toggle(settingsOpened,new GUIContent(settings_icon),buttonStyle);

        GUILayout.EndHorizontal();

        if(!settings.localEdit)
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = path.transform.TransformPoint(points[i]);
                inTangents[i] = path.transform.TransformPoint(inTangents[i]);
                outTangents[i] = path.transform.TransformPoint(outTangents[i]);
            }

        EditorGUI.BeginChangeCheck();

        GUILayout.Space(5f);


        if (MultiVector3Field("Position", points, out var valueIndex,out var value))
        {
            for (int i = 0; i < points.Count; i++)
            {
                var oldvalue = points[i];
                var newValue = oldvalue;
                newValue[valueIndex] = value;
                var delta = newValue - oldvalue;
                points[i] = newValue;
                inTangents[i] += delta;
                outTangents[i] += delta;
            }
            applyPath = true;
        }

        GUILayout.Space(5f);

        //tangents 
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Tangents");

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(settings.relativeTangentEdit ? "R" : "A", GUILayout.Width(InspectorButtonSize)))
        {
            settings.relativeTangentEdit = !settings.relativeTangentEdit;
            applySettings = true;
        }

        EditorGUILayout.EndHorizontal();

        if (settings.relativeTangentEdit)
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                inTangents[i] = inTangents[i] - point;
                outTangents[i] = outTangents[i] - point;
            }

        if(MultiVector3Field("In",inTangents))
        {
            setInTangent = true;
            applyPath = true;
        }
        if (MultiVector3Field("Out",outTangents))
        {
            setOutTangent = true;
            applyPath = true;
        }

        if (settings.relativeTangentEdit)
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                inTangents[i] = inTangents[i] + point;
                outTangents[i] = outTangents[i] + point;
            }

        if (!settings.localEdit)
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = path.transform.InverseTransformPoint(points[i]);
                inTangents[i] = path.transform.InverseTransformPoint(inTangents[i]);
                outTangents[i] = path.transform.InverseTransformPoint(outTangents[i]);
            }

        GUILayout.Space(5f);

        applyPath |= MultiValueField(angles, (angle) => EditorGUILayout.FloatField("Angle", angle));

        GUILayout.Space(15f);


        if(setInTangent)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var cp = new ControlPoint(points[i], inTangents[i], outTangents[i]);
                ControlPoint.SetTangent(ref cp, cp.inTangent, true, tangentsMode[i]);
                points[i] = cp.point; inTangents[i] = cp.inTangent; outTangents[i] = cp.outTangent;
            }
        }
        if(setOutTangent)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var cp = new ControlPoint(points[i], inTangents[i], outTangents[i]);
                ControlPoint.SetTangent(ref cp, cp.outTangent, false, tangentsMode[i]);
                points[i] = cp.point; inTangents[i] = cp.inTangent; outTangents[i] = cp.outTangent;
            }
        }

        if (applyPath)
        {
            int c = 0;
            for (int i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                var cp = path.spline.GetControlPoint(index);

                cp.point = points[c];
                cp.inTangent = inTangents[c];
                cp.outTangent = outTangents[c];
                cp.angle = angles[c];
                cp.editor_only_tangent_mode = tangentsMode[c++];

                path.spline.SetControlPoint(index, cp);
            }
            path.Apply();
            SceneView.RepaintAll();
        }
        if (applySettings)
        {
            settingsSO.Apply();
            SceneView.RepaintAll();
        }

        AlignCenter(() =>
        {
            if (Button("<", !spline.Loop && selectedPoint == 0, InspectorButtonSize))
            {
                selectedPoint--;
                if (selectedPoint < 0)
                    selectedPoint = spline.ControlPointsCount - 1;
            }
            EditorGUI.showMixedValue = selectedPoints.Count > 0;
            selectedPoint = EditorGUILayout.IntField(selectedPoint + 1, GUILayout.Width(30)) - 1;
            EditorGUI.showMixedValue = false;
            if (Button(">", !spline.Loop && selectedPoint == spline.ControlPointsCount - 1, InspectorButtonSize))
            {
                selectedPoint++;
                selectedPoint %= spline.ControlPointsCount;
            }
        });

        GUILayout.EndVertical();

        if(path.transform.localScale.x != path.transform.localScale.y || path.transform.localScale.x != path.transform.localScale.z)
            EditorGUILayout.HelpBox("non uniform scaling is not supported!", MessageType.Warning);

        if (settingsOpened)
        {
            GUILayout.Space(20);
            AlignCenter(() => GUILayout.Label("Settings"));

            EditorGUI.BeginChangeCheck();
            SettingsEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                settingsSO.Update();
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) ||
            !path.transform.gameObject.activeSelf)
            return;

        //cancel multi selection and adding tool
        if (cancel)
        {
            selectedPoints.Clear();
            if(addingTool.IsActive)
                addingTool.Stop();
            Tools.current = tool;
            SceneView.RepaintAll();
            return;
        }

        if (addingTool.IsActive)
        {
            DrawControlPointsIcons((i) => controlPoint_normal_texture);

            addingTool.OnSceneGUI(view);
            selectedPoint = addingTool.RecentPoint;
        }
        else
        {
            //multi selection
            if (selectedPoints.Count > 1 || inSelection)
            {
                var normal = new Texture2D[]
                {
                controlPoint_normal_texture,
                controlPoint_hover_texture,
                controlPoint_active_texture
                };
                var selected = new Texture2D[]
                {
                controlPoint_hover_texture,
                controlPoint_hover_texture,
                controlPoint_active_texture
                };

                ControlPointsDragHandlers((i) => selectedPoints.Contains(i) ? selected : normal);

                MultiPointHandle();
            }
            else
            {
                DrawAngleHandle(selectedPoint);

                var textures = new Texture2D[]
                {
                controlPoint_normal_texture,
                controlPoint_hover_texture,
                controlPoint_active_texture
                };
                if (settings.smartHandleMode)
                    ControlPointsDragHandlers((i) => textures);
                else
                    ControlPointsMoveHandlers();
            }

            DrawContextMenu();
            SplitPathControl();
        }

    }

    private void ProcessSelection()
    {
        if (HandleEx.NavKeyDown())
            return;

        switch (Event.current.type)
        {
            case EventType.MouseDown:
                GetContextMenuPramas(out var rect, out var buttons);
                if (Event.current.button == 0 && HandleEx.NearestDistance() >= 4.9f && !rect.Contains(Event.current.mousePosition))
                {
                    selectionStartPosition = Event.current.mousePosition;
                    inSelection = true;
                }
                break;
            case EventType.MouseUp:
                if (Event.current.button == 0)
                    inSelection = false;
                break;
            case EventType.MouseLeaveWindow:
                inSelection = false;
                break;
        }

        if (inSelection)
        {
            var rect = new Rect()
            {
                min = Vector2.Min(Event.current.mousePosition, selectionStartPosition),
                max = Vector2.Max(Event.current.mousePosition, selectionStartPosition)
            };
            selectedPoints.Clear();
            for (int i = 0; i < spline.ControlPointsCount; i++)
            {
                var cpoint = path.GetTransformPoint(i);
                if (rect.Contains(HandleUtility.WorldToGUIPoint(cpoint.point)))
                    selectedPoints.Add(i);
            }
        }
    }

    #region Split Control
    private void SplitPathControl()
    {
        if (Event.current.control && spline.ControlPointsCount > 1)
        {
            if(Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();

            var closestPosToMouse = GetClosestLength();
            var pos = path.transform.TransformPoint(spline.GetPoint(closestPosToMouse));
            if (Vector2.Distance(HandleUtility.WorldToGUIPoint(pos),Event.current.mousePosition) <= 50f)
            {
                buttonHandle.normal.SetTexture(controlPoint_normal_texture);
                buttonHandle.hover.SetTexture(controlPoint_normal_texture);
                buttonHandle.active.SetTexture(controlPoint_normal_texture);

                buttonHandle.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, pos));

                if (buttonHandle.Draw<SphereD>())
                {
                    var index = spline.Split(closestPosToMouse);
                    var cpoint = spline.GetControlPoint(index);
                    cpoint.editor_only_tangent_mode = settings.lockTangentByDefault ?
                        TangentMode.Lock : TangentMode.Free;
                    spline.SetControlPoint(index, cpoint);
                }
                path.Apply();
            }
        }
    }

    private float GetClosestLength(int res = 20)
    {
        var mPos = Event.current.mousePosition;
        List<(int, float)> segmentDistance = new List<(int, float)>();

        for (int i = 0; i < spline.SegmentCount; i++)
        {
            var rect = BoundsToScreenRect(spline[i].Bounds);
            var d = DistanceToRect(mPos, rect);
            segmentDistance.Add((i, d));
        }
        segmentDistance.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        System.Func<Vector3, float> localPointToMPos = (vec) =>
        {
            vec = path.transform.TransformPoint(vec);
            return Vector2.Distance(HandleUtility.WorldToGUIPoint(vec), mPos);
        };

        var segt = 0f;
        var segmentIndex = 0;
        float step = 1f / res;
        var minDist = float.MaxValue;

        for (int i = 0; i < Mathf.Min(3, segmentDistance.Count); i++)
        {
            int ittc = 0;
            var seg = spline[segmentDistance[i].Item1];
            seg.Iterate(excute: (vec) =>
            {
                var d = localPointToMPos(vec);
                if (d < minDist)
                {
                    segt = step * ittc;
                    segmentIndex = segmentDistance[i].Item1;
                    minDist = d;
                }
                ittc++;
            }, res);
        }

        {
            float a, b;
            var seg = spline[segmentIndex];
            a = segt - step;
            b = segt + step;
            var da = localPointToMPos(seg.GetPoint(a));
            var db = localPointToMPos(seg.GetPoint(b));

            for (int i = 0; i < 20; i++)
            {
                var m = (a + b) * 0.5f;
                var dm = localPointToMPos(seg.GetPoint(m));
                if (Mathf.Abs(dm - da) < Mathf.Abs(dm - db))
                {
                    b = m;
                    db = dm;
                }
                else
                {
                    a = m;
                    da = dm;
                }
                segt = m;
            }
        }

        float lenght = 0;
        for (int i = 0; i < segmentIndex; i++)
            lenght += spline[i].Length;
        return lenght + spline[segmentIndex].GetArcLength(segt);
    }

    float DistanceToRect(Vector2 point, Rect rect)
    {
        Vector2 p = point - rect.center; Vector2 ext = rect.size * 0.5f;
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - ext;
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0);
    }

    Rect BoundsToScreenRect(Bounds bounds)
    {
        var c = bounds.center;
        var e = bounds.extents;
        Vector3[] corners = new Vector3[]
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, e.z),
            c + new Vector3(-e.x, -e.y, e.z),
            c + new Vector3(-e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, e.z),
            c + new Vector3(-e.x, e.y, e.z),
        };
        Rect rect = new Rect(HandleUtility.WorldToGUIPoint(c + e), Vector2.zero);
        for (int i = 1; i < corners.Length; i++)
            rect = Encapsulate(rect, HandleUtility.WorldToGUIPoint(corners[i]));
        return rect;
    }

    Rect Encapsulate(Rect rect, Vector2 point)
    {
        rect.min = Vector2.Min(point, rect.min);
        rect.max = Vector2.Max(point, rect.max);
        return rect;
    }

    #endregion

    #region Move Handle

    private void ControlPointsDragHandlers(System.Func<int, Texture2D[]> GetTextures)
    {
        for (int i = 0; i < spline.ControlPointsCount; i++)
        {
            var cpoint = path.GetTransformPoint(i);
            bool inculdInTangent = i != 0 || spline.Loop;
            bool inculdOutTangent = i != spline.ControlPointsCount - 1 || spline.Loop;
            bool select = false;

            var textures = GetTextures(i);
            dragHandle.normal.SetTexture(textures[0]);
            dragHandle.hover.SetTexture(textures[1]);
            dragHandle.active.SetTexture(textures[2]);

            dragHandle.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.point));
            if (dragHandle.Draw<SphereD>())
                cpoint.point = SceneRaycast.SmartRaycast(cpoint.point);

            select |= dragHandle.DidClick;

            if (settings.showTangents)
            {
                if (inculdInTangent)
                {
                    dragHandle.SetAllTransformation(IconDrawCmd(settings.tangentIconSize, cpoint.inTangent));
                    if (dragHandle.Draw<SphereD>())
                        cpoint.inTangent = SceneRaycast.SmartRaycast(cpoint.inTangent);

                    select |= dragHandle.DidClick;
                }
                if (inculdOutTangent)
                {
                    dragHandle.SetAllTransformation(IconDrawCmd(settings.tangentIconSize, cpoint.outTangent));
                    if (dragHandle.Draw<SphereD>())
                        cpoint.outTangent = SceneRaycast.SmartRaycast(cpoint.outTangent);

                    select |= dragHandle.DidClick;
                }
            }

            if (ApplyControlPoint(i, cpoint) || select)
            {
                selectedPoint = i;
                selectedPoints.Clear();
                Repaint();
            }
        }
    }

    private void ControlPointsMoveHandlers()
    {
        buttonHandle.normal.SetTexture(controlPoint_normal_texture);
        buttonHandle.hover.SetTexture(controlPoint_hover_texture);
        buttonHandle.active.SetTexture(controlPoint_active_texture);

        for (int i = 0; i < spline.ControlPointsCount; i++)
        {
            if (i != selectedPoint)
            {
                var cpoint = path.GetTransformPoint(i);
                bool inculdInTangent = i != 0 || spline.Loop;
                bool inculdOutTangent = i != spline.ControlPointsCount - 1 || spline.Loop;
                var select = false;

                buttonHandle.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.point));

                if (buttonHandle.Draw<SphereD>())
                    select = true;

                if (settings.showTangents)
                {
                    buttonHandle.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.inTangent));
                    if (inculdInTangent && buttonHandle.Draw<SphereD>())
                        select = true;

                    buttonHandle.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.outTangent));
                    if (inculdOutTangent && buttonHandle.Draw<SphereD>())
                        select = true;
                }

                if(select)
                {
                    selectedPoint = i;
                    Repaint();
                }
            }
        }

        {
            var cpoint = path.GetTransformPoint(selectedPoint);
            bool inculdInTangent = selectedPoint != 0 || spline.Loop;
            bool inculdOutTangent = selectedPoint != spline.ControlPointsCount - 1 || spline.Loop;

            cpoint.point = Handles.DoPositionHandle(cpoint.point, path.transform.rotation);
            if (inculdInTangent)
                cpoint.inTangent = Handles.DoPositionHandle(cpoint.inTangent, path.transform.rotation);
            if (inculdOutTangent)
                cpoint.outTangent = Handles.DoPositionHandle(cpoint.outTangent, path.transform.rotation);

            ApplyControlPoint(selectedPoint, cpoint);
        }
    }

    #endregion

    #region Multi Points Handle

    private void MultiPointHandle()
    {
        Vector3 center = GetSelectedPointsCenter();
        var centerws = path.transform.TransformPoint(center);

        if (Tools.current != Tool.None)
        {
            tool = (
                Tools.current != Tool.Move &&
                Tools.current != Tool.Rotate &&
                Tools.current != Tool.Scale) ?
                tool == Tool.None ? Tool.Move : tool : Tools.current;
            Tools.current = Tool.None;
            rotation = path.transform.rotation;
            scale = path.transform.localScale;
        }

        switch (tool)
        {
            case Tool.Move:
                {
                    var newCenter = Handles.DoPositionHandle(centerws, path.transform.rotation);
                    var delta = path.transform.InverseTransformPoint(newCenter) - center;

                    TransformSelectedPoints(center, Matrix4x4.Translate(delta));
                }
                break;
            case Tool.Rotate:
                {
                    if (Event.current.type == EventType.MouseUp)
                        rotation = path.transform.rotation;

                    var newRotation = Handles.DoRotationHandle(rotation, centerws);
                    var delta = newRotation * Quaternion.Inverse(rotation);
                    rotation = newRotation;

                    TransformSelectedPoints(center, Matrix4x4.Rotate(delta));
                }
                break;
            case Tool.Scale:
                {
                    if (Event.current.type == EventType.MouseUp)
                        scale = path.transform.localScale;

                    var newScale = Handles.DoScaleHandle(scale, centerws, path.transform.rotation, 1f);
                    Vector3 delta = new Vector3(newScale.x / scale.x, newScale.y / scale.y, newScale.z / scale.z);
                    scale = newScale;

                    TransformSelectedPoints(center, Matrix4x4.Scale(delta));
                }
                break;
        }
    }

    private Vector3 GetSelectedPointsCenter()
    {
        var center = Vector3.zero;
        foreach (var p in selectedPoints)
            center += spline.GetControlPoint(p).point;

        center.Scale(Vector3.one * (1f / selectedPoints.Count));
        return center;
    }

    private void TransformSelectedPoints(Vector3 center, Matrix4x4 m)
    {
        var trans = Matrix4x4.Translate(center) * m * Matrix4x4.Translate(-center);
        foreach (var p in selectedPoints)
        {
            var cpoint = spline.GetControlPoint(p);
            cpoint.point = trans.MultiplyPoint(cpoint.point);
            cpoint.inTangent = trans.MultiplyPoint(cpoint.inTangent);
            cpoint.outTangent = trans.MultiplyPoint(cpoint.outTangent);
            spline.SetControlPoint(p, cpoint);
        }
        path.Apply();
    }

    #endregion

    #region Context Menu

    private void DrawContextMenu()
    {
        GetContextMenuPramas(out var rect, out var buttons);
        DrawContextMenu(rect, buttons);
    }

    private void GetContextMenuPramas(out Rect rect, out List<(GUIStyle, System.Action)> buttons)
    {
        buttons = new List<(GUIStyle, System.Action)>();
        Vector3 position;

        if (selectedPoints.Count > 1)
        {
            position = path.transform.TransformPoint(GetSelectedPointsCenter());

            //remove points button
            buttons.Add((delete_button_style, () =>
            {
                var indices = new List<int>();
                foreach (var index in selectedPoints)
                    indices.Add(index);
                indices.Sort((a, b) => b.CompareTo(a));

                for (int i = 0; i < indices.Count; i++)
                    spline.RemoveControlPoint(indices[i]);

                selectedPoint = 0;
                selectedPoints.Clear();
                path.Apply();
            }
            ));

            //lock tangent button
            TangentMode tanMode = default;
            foreach (var index in selectedPoints)
            {
                tanMode = spline.GetControlPoint(index).editor_only_tangent_mode;
                break;
            }
            buttons.Add((tanMode == TangentMode.Lock ? lock_on_button_style : lock_off_button_style, () =>
            {
                foreach (var index in selectedPoints)
                {
                    var cpoint = spline.GetControlPoint(index);
                    cpoint.editor_only_tangent_mode = tanMode.Next();
                    cpoint.SetTangent(cpoint.inTangent, true, tanMode.Next());
                    spline.SetControlPoint(index, cpoint);
                }
                path.Apply();
            }
            ));
        }
        else
        {
            var cpoint = path.GetTransformPoint(selectedPoint);
            position = cpoint.point;

            bool isEndPoint = selectedPoint == 0 || selectedPoint == spline.ControlPointsCount - 1;
            bool only_point = spline.ControlPointsCount == 1;

            //remove point button
            if (!only_point)
                buttons.Add((delete_button_style, () =>
                {
                    spline.RemoveControlPoint(selectedPoint);
                    selectedPoint = Mathf.Clamp(selectedPoint, 0, spline.ControlPointsCount - 1);
                    path.Apply();
                }
                ));

            if (isEndPoint)
            {
                //loop button
                buttons.Add((spline.Loop ?
                loop_on_button_style : loop_off_button_style, () =>
                {
                    spline.Loop = !spline.Loop;
                    path.Apply();
                }
                ));

                //add points button
                buttons.Add((add_button_style, () => addingTool.Start(selectedPoint != 0)));
            }

            //lock tangent button
            if (!isEndPoint || spline.Loop)
                buttons.Add((cpoint.editor_only_tangent_mode == TangentMode.Lock ?
                    lock_on_button_style : lock_off_button_style, () =>
                    {
                        cpoint.editor_only_tangent_mode =
                        cpoint.editor_only_tangent_mode.Next();
                        cpoint.SetTangent(cpoint.inTangent, true, cpoint.editor_only_tangent_mode);
                        path.SetTransformedPoint(selectedPoint, cpoint);
                        path.Apply();
                    }
                ));
        }

        //show tangent button
        buttons.Add((settings.showTangents ? tangents_on_button_style : tangents_off_button_style, () =>
        {
            settings.showTangents = !settings.showTangents;
            settingsSO.Apply();
        }
        ));

        var size = new Vector2(settings.contextMenuButtonSize * buttons.Count + 5, settings.contextMenuButtonSize + 5);
        rect = new Rect(HandleUtility.WorldToGUIPoint(position) - Vector2.up * size.y
            + new Vector2(sceneMenuOffset, -sceneMenuOffset), size);
    }

    private void DrawContextMenu(Rect rect, List<(GUIStyle, System.Action)> buttons)
    {
        if (Event.current.type == EventType.Layout && rect.Contains(Event.current.mousePosition))
            SceneView.RepaintAll();

        Handles.BeginGUI();
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.BeginHorizontal();

        for (int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            if (GUILayout.Button("", button.Item1))
                button.Item2();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    #endregion

    private void DrawAngleHandle(int pointIndex)
    {
        if (!settings.showAngleHandle)
            return;

        var cpoint = path.GetTransformPoint(pointIndex);

        var position = cpoint.point;
        var forward = cpoint.outTangent - cpoint.point;
        var size = settings.drawSettings.width * 1.2f;

        Handles.color = Color.white;

        forward = forward == Vector3.zero ? Vector3.forward : forward;
        Handles.matrix = Matrix4x4.TRS(position,
            Quaternion.LookRotation(forward, Mathf.Abs(forward.normalized.y) > 0.5f ? Vector3.forward : Vector3.up) *
            Quaternion.Euler(90, 0, 0) *
            Quaternion.Euler(0, 90f, 0), Vector3.one * size);

        arcHandle.angle = cpoint.angle;
        arcHandle.radiusHandleSizeFunction = (var) => 0.5f;
        arcHandle.fillColor = settings.drawSettings.ColorB * new Color(1, 1, 1, 0.1f);
        arcHandle.angleHandleColor = settings.drawSettings.ColorA;
        arcHandle.wireframeColor = settings.tangentColor;
        arcHandle.DrawHandle();
        arcHandle.angle = Mathf.Clamp(arcHandle.angle, -180f, 180f);

        Handles.matrix = Matrix4x4.identity;

        var angle = arcHandle.angle;
        if (angle != cpoint.angle)
        {
            cpoint.angle = angle;
            path.SetTransformedPoint(pointIndex, cpoint);
            path.Apply();
        }
    }

    private void DrawControlPointsIcons(System.Func<int, Texture2D> GetTexture)
    {
        if (Event.current.type == EventType.Repaint)
        {
            for (int i = 0; i < spline.ControlPointsCount; i++)
            {
                var cpoint = path.GetTransformPoint(i);
                bool inculdInTangent = i != 0 || spline.Loop;
                bool inculdOutTangent = i != spline.ControlPointsCount - 1 || spline.Loop;

                var texture = GetTexture(i);

                IconDrawCmd(settings.controlPointIconSize, cpoint.point).SetTexture(texture).Draw();

                if (inculdInTangent)
                    IconDrawCmd(settings.tangentIconSize, cpoint.inTangent).SetTexture(texture).Draw();
                if (inculdOutTangent)
                    IconDrawCmd(settings.tangentIconSize, cpoint.outTangent).SetTexture(texture).Draw();
            }
        }
    }

    DrawCommand IconDrawCmd(float size, Vector3 position)
    {
        if(settings.screenSpaceIcon)
            return GetDrawCmd().ConstantScreenSize(position).
            Scale(size).LookAtCamera().Move(position);
        else
            return GetDrawCmd().Scale(size*0.01f).LookAtCamera().Move(position);
    }

    private bool ApplyControlPoint(int i, ControlPoint cpoint)
    {
        ControlPoint preCpoint = path.GetTransformPoint(i);
        bool didChange = false;

        if (preCpoint.point != cpoint.point)
        {
            preCpoint.Set(cpoint.point);
            didChange = true;
        }
        else if (preCpoint.inTangent != cpoint.inTangent)
        {
            preCpoint.SetTangent(cpoint.inTangent, true, preCpoint.editor_only_tangent_mode);
            didChange = true;
        }
        else if (preCpoint.outTangent != cpoint.outTangent)
        {
            preCpoint.SetTangent(cpoint.outTangent, false, preCpoint.editor_only_tangent_mode);
            didChange = true;
        }

        if (didChange)
        {
            path.SetTransformedPoint(i, preCpoint);
            path.Apply();
        }
        return didChange;
    }

    private GUIStyle InspectorButtonStyle()
    {
        var buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset();
        buttonStyle.fixedHeight = InspectorButtonSize;
        buttonStyle.fixedWidth = InspectorButtonSize;
        return buttonStyle;
    }

    private Texture2D GetTexture(string name)
    {
        if (texture_lookup.ContainsKey(name))
            return texture_lookup[name];
        var scriptDir = System.IO.Path.Combine(EditorHelper.GetScriptDirectory<PathEditor>(), "Resources");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>
            (System.IO.Path.Combine(scriptDir, name));
        texture_lookup.Add(name, texture);
        return texture;
    }

    private GUIStyle GetButtonStyle(string normalTexture, string hoverTexture)
    {
        GUIStyle style = new GUIStyle();
        style.stretchWidth = true;
        style.stretchHeight = true;
        style.imagePosition = ImagePosition.ImageOnly;
        style.normal.background = GetTexture(normalTexture);
        style.hover.background = GetTexture(hoverTexture);
        return style;
    }

    #region GUI HELPER
    bool MultiVector3Field(string name, List<Vector3> values)
    {
        bool didChange = MultiVector3Field(name, values, out var index,out var value);
        if(didChange)
            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                v[index] = value;
                values[i] = v;
            }
        return didChange;
    }
    
    bool MultiVector3Field(string name, List<Vector3> values , out int index,out float nv)
    {
        var value = values[0];
        bool sameX = true, sameY = true, sameZ = true;
        for (int i = 1; i < values.Count; i++)
        {
            var v = values[i];
            sameX &= value.x == v.x;
            sameY &= value.y == v.y;
            sameZ &= value.z == v.z;
        }

        Vector3 newValue = value;

        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(name, GUILayout.Width(45), GUILayout.ExpandWidth(true));

        EditorGUI.showMixedValue = !sameX;
        var oldWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 11;
        newValue.x = RoundedFloatField("X", newValue.x, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
        EditorGUI.showMixedValue = !sameY;
        newValue.y = RoundedFloatField("Y", newValue.y, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
        EditorGUI.showMixedValue = !sameZ;
        newValue.z = RoundedFloatField("Z", newValue.z, 2, GUILayout.Width(50), GUILayout.ExpandWidth(true));
        EditorGUI.showMixedValue = false;
        EditorGUIUtility.labelWidth = oldWidth;

        GUILayout.EndHorizontal();

        index = 0;
        if (newValue.x != value.x)
            index = 0;
        else if (newValue.y != value.y)
            index = 1;
        else if (newValue.z != value.z)
            index = 2;
        nv = newValue[index];
        return newValue.x != value.x || newValue.y != value.y || newValue.z != value.z;
    }

    bool MultiValueField<T>(List<T> values,
        System.Func<T, T> GUIFunc)
    {
        var value = values[0];
        bool sameValue = true;
        for (int i = 1; i < values.Count; i++)
            if (!EqualityComparer<T>.Default.Equals(values[i], value))
            {
                sameValue = false;
                break;
            }
        EditorGUI.BeginChangeCheck();

        EditorGUI.showMixedValue = !sameValue;
        value = GUIFunc(value);
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            for (int i = 0; i < values.Count; i++)
                values[i] = value;
            return true;
        }
        return false;
    }

    float RoundedFloatField(string name, float value, int pow = 2, params GUILayoutOption[] options)
    {
        float bnum = Mathf.Pow(10, pow);
        var input = Mathf.Round(value * bnum) / bnum;
        var output = EditorGUILayout.FloatField(name, input, options);
        return value + (output - input);
    }

    Vector3 RoundedVectorField(string name, Vector3 value, float labelWidth = 50f, int pow = 2)
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
            EditorGUILayout.LabelField(name, GUILayout.Width(labelWidth));
            output = EditorGUILayout.Vector3Field("", input);
            EditorGUILayout.EndHorizontal();
        }else
            output = EditorGUILayout.Vector3Field("", input);
        return value + (output - input);
    }

    bool Button(string name, bool disable, float width = 30)
    {
        if (disable)
        {
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button(name, GUILayout.Width(width));
            EditorGUI.EndDisabledGroup();
        }
        else
            return GUILayout.Button(name, GUILayout.Width(width));

        return false;
    }

    private void AlignCenter(System.Action Draw)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Draw();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    #endregion

    #region DrawGizmos

    private static Dictionary<Path, Mesh> meshPool = new Dictionary<Path, Mesh>();

    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
    private static void DrawGizmosNotSelected(Path path, GizmoType type)
    {
        var settings = PathSettings.GetSettings().settings;

        DrawSpline(path, path.transform.localToWorldMatrix,
            Gizmos.DrawLine, (color) => Gizmos.color = color, settings.drawSettings);

        if(inSelection)
            DrawTangents(path, settings);
    }

    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    private static void DrawGizmosSelected(Path path, GizmoType type)
    {
        var settings = PathSettings.GetSettings().settings;
        //drawing spline
        DrawSpline(path, path.transform.localToWorldMatrix,
        (a, b) => Handles.DrawAAPolyLine(a, b),
        (color) => Handles.color = color, settings.drawSettings);

        DrawTangents(path, settings);
    }

    private static void DrawTangents(Path path, PathSettings.Settings settings)
    {
        if (settings.showTangents)
            for (int i = 0; i < path.ControlPointsCount; i++)
            {
                var cpoint = path.GetControlPoint(i);
                cpoint.point = path.transform.TransformPoint(cpoint.point);
                cpoint.inTangent = path.transform.TransformPoint(cpoint.inTangent);
                cpoint.outTangent = path.transform.TransformPoint(cpoint.outTangent);

                bool inculdInTangent = i != 0 || path.Loop;
                bool inculdOutTangent = i != path.ControlPointsCount - 1 || path.Loop;
                Handles.color = settings.tangentColor;
                if (inculdInTangent)
                    Handles.DrawAAPolyLine(cpoint.point, cpoint.inTangent);
                if (inculdOutTangent)
                    Handles.DrawAAPolyLine(cpoint.point, cpoint.outTangent);
            }
    }

    private static void DrawSpline(ISpline spline, Matrix4x4 matrix, System.Action<Vector3, Vector3> DrawLine,
        System.Action<Color> setColor, SplineDrawSettings settings)
    {
        DrawDirection(spline,matrix,DrawLine, setColor, settings);

        float step = 1f / settings.resolution;
        for (int i = 0; i < spline.SegmentCount; i++)
        {
            int it = 0;
            var seg = spline[i];
            Sample preSample = default;
            System.Func<Sample, float, Vector3> GetSide = (sample, delta)
                   => sample.point + sample.right * delta;

            seg.Iterate(excuteSample: (sample) =>
            {
                it++;
                sample.Transform(matrix);

                if (it > 1)
                {
                    if (settings.shape == SplineDrawSettings.Shape.Line)
                    {
                        setColor(it % 2 == 0 ? settings.ColorA : settings.ColorB);
                        DrawLine(preSample.point, sample.point);
                    }
                    else if (settings.shape == SplineDrawSettings.Shape.Road)
                    {
                        float width = settings.width * 0.5f;
                        setColor(settings.ColorA);
                        DrawLine(GetSide(sample, -width), GetSide(sample, width));
                        setColor(settings.ColorB);
                        DrawLine(GetSide(sample, -width), GetSide(preSample, -width));
                        DrawLine(GetSide(sample, width), GetSide(preSample, width));
                    }
                }
                preSample = sample;

            }, settings.resolution);
        }
    }

    private static void DrawDirection(ISpline spline,Matrix4x4 matrix, System.Action<Vector3, Vector3> DrawLine,
        System.Action<Color> setColor, SplineDrawSettings settings)
    {
        float step = 1f / settings.resolution;

        var firstSegment = spline[0];
        var sa = firstSegment.GetSample(0).Transform(matrix);
        var sb = firstSegment.GetSample(step).Transform(matrix);

        var a0 = sa.point + sa.right * settings.width * 0.5f;
        var a1 = sa.point - sa.right * settings.width * 0.5f;
        var a2 = sb.point;

        setColor(settings.ColorA);
        DrawLine(a0, a1);
        DrawLine(a1, a2);
        DrawLine(a2, a0);
    }

    #endregion
}