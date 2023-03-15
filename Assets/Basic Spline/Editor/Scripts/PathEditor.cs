using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using AlaslTools;

namespace BasicSpline
{
    using static HandleEx;

    [CustomEditor(typeof(Path))]
    public class PathEditor : Editor
    {
        public class SO : BaseSO<Path>
        {
            public SO(Object target) : base(target) { }
            public SO(SerializedObject serializedObject) : base(serializedObject) { }

            public Spline spline;
            public List<int> selected_points_editor_only;
            public Transform transform => target.transform;
        }

        // adding control points
        private class AddingTool
        {
            public bool IsActive => active;
            public int RecentPoint => lastIndex;

            private PathEditor editor;
            private ISpline spline => editor.spline;
            private SO so => editor.path;

            private static int controlID = "BasicSpline/AddingTool".GetHashCode();
            private bool active;

            bool appendLast;
            int lastIndex;

            public AddingTool(PathEditor editor)
            {
                this.editor = editor;
            }

            public void Start(bool appendLast = true)
            {
                active = true;
                this.appendLast = appendLast;

                if (this.appendLast)
                    lastIndex = so.spline.ControlPointsCount - 1;
                else
                    lastIndex = 0;
            }

            public void OnSceneGUI(SceneView sceneView)
            {
                if (!active || NavKeyDown())
                    return;

                lastIndex = Mathf.Clamp(lastIndex, 0, so.spline.ControlPointsCount - 1);

                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        if (Event.current.button == 0)
                        {
                            var pos = SceneRaycast.SmartRaycast(so.spline.GetControlPoint(lastIndex).point);

                            var cpoint = new ControlPoint(pos, pos, pos);

                            cpoint.editor_only_tangent_mode =
                                PathSettings.GetSettings().settings.lockTangentByDefault  ?
                                TangentMode.Lock : TangentMode.Free;

                            if (appendLast)
                            {
                                spline.AddControlPoint(cpoint);
                                editor.SetSelectedPoint(++lastIndex);
                            }
                            else
                                spline.InsertControlPoint(0, cpoint);

                            so.ApplyField(nameof(SO.spline));

                            Event.current.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        {
                            var cpoint = spline.GetControlPoint(lastIndex);

                            cpoint.SetTangent(SceneRaycast.SmartRaycast(cpoint.point), true, TangentMode.Lock);

                            spline.SetControlPoint(lastIndex, cpoint);
                            so.ApplyField(nameof(SO.spline));

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
            }
        }

        private const float sceneMenuOffset = 30f;
        private const float InspectorButtonSize = 25;

        private SO path;
        private ISpline spline;
        List<int> selectedPoints => path.selected_points_editor_only;
        bool singlePointSelected => selectedPoints.Count == 1;
        int selectedPoint => selectedPoints[0];

        private bool settingsOpened;
        private PathSettingsEditor SettingsEditor;
        private SettingsSO settingsSO => SettingsEditor.settingsSO;
        private PathSettings.Settings settings => SettingsEditor.settings;

        private static Tool tool;
        private Quaternion rotation;
        private Vector3 scale;

        private const double CancelClickThreshold = 0.15;
        private double lastCancelClickTime;
        private bool cancel;

        private static bool inSelection;
        private static Vector2 selectionStartPosition;
        private bool mouseInsideWindow = true;
        private List<int> pointsInSelection = new List<int>();

        private AddingTool addingTool;

        private PathEditorResource resource;
        private ArcHandle arcHandle;

        private void OnEnable()
        {
            path = new SO(target);
            spline = new TransformedSpline(path.spline, path.transform);

            SettingsEditor = (PathSettingsEditor)CreateEditor(PathSettings.GetSettings(), typeof(PathSettingsEditor));

            resource = new PathEditorResource();
            arcHandle = new ArcHandle();

            addingTool = new AddingTool(this);

            ValidateSpline();
            ValidateSelectedPoints();

            Undo.undoRedoPerformed += UndoPerformed;
            SceneView.beforeSceneGui += BeforeSceneGUI;
        }

        private void UndoPerformed()
        {
            ValidateSelectedPoints();
            spline = new TransformedSpline(path.spline, path.transform);
        }

        private void OnDisable()
        {
            if ((Selection.activeObject == null || settings.PrioritizeOnSelection)
                && !singlePointSelected && mouseInsideWindow)
                Selection.activeObject = target;

            path?.Dispose();

            DestroyImmediate(SettingsEditor);

            resource.Dispose();

            Undo.undoRedoPerformed -= UndoPerformed;
            SceneView.beforeSceneGui -= BeforeSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset();
            buttonStyle.fixedHeight = InspectorButtonSize;
            buttonStyle.fixedWidth = InspectorButtonSize;

            var points = settings.localEdit ?
            selectedPoints.Select((i) => path.spline.GetControlPoint(i)) :
            selectedPoints.Select((i) => spline.GetControlPoint(i));

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (singlePointSelected && GUILayout.Button(resource.delete_button_normal, buttonStyle))
                RemoveSelectedPoints();

            var tangentRes = EditorGUILayoutEX.MultiValueField(
                points.Select((point)=> point.editor_only_tangent_mode), 
                (tmode) =>
                {
                    if (GUILayout.Button(tmode == TangentMode.Lock ?
                          resource.lock_on_button_normal :
                          resource.lock_off_button_normal, buttonStyle))
                        return tmode.Next();
                    else
                        return tmode;
                });

            if (tangentRes.didChange)
                EditSelectedPoints((point) => {
                    point.editor_only_tangent_mode = tangentRes.value;
                    return point;
                });

            if (GUILayout.Button( spline.Loop ?
                resource.loop_on_button_normal : resource.loop_off_button_normal, buttonStyle))
            {
                spline.Loop = !spline.Loop;
                path.ApplyField(nameof(SO.spline));
            }

            if (GUILayout.Button( settings.showTangents ?
                resource.tangents_on_button_normal : resource.tangents_off_button_normal, buttonStyle))
            {
                settings.showTangents = !settings.showTangents;
                settingsSO.Apply();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(settings.localEdit ? resource.localspace_icon :resource.worldspace_icon, buttonStyle))
            {
                settings.localEdit = !settings.localEdit;
                settingsSO.Apply();
            }

            settingsOpened = GUILayout.Toggle(settingsOpened, new GUIContent(resource.settings_icon), buttonStyle);

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            var positionChangeRes = EditorGUILayoutEX.MultiVector3Field("Position", points.Select((point) => point.point));

            if(positionChangeRes.didChange)
            {
                EditSelectedPoints((point) =>
                {
                    point.Set(positionChangeRes.value.Apply(point.point));
                    return point;
                }, settings.localEdit);
            }

            GUILayout.Space(5f);

            //tangents 
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Tangents");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(settings.relativeTangentEdit ? "R" : "A", GUILayout.Width(InspectorButtonSize)))
            {
                settings.relativeTangentEdit = !settings.relativeTangentEdit;
                settingsSO.Apply();
            }

            EditorGUILayout.EndHorizontal();

            var inTanChangeRes = EditorGUILayoutEX.MultiVector3Field("In",
                points.Select((point) => point.inTangent - 
                (settings.relativeTangentEdit ? point.point : Vector3.zero )));

            if (inTanChangeRes.didChange)
            {
                EditSelectedPoints((point) =>
                {
                    inTanChangeRes.value.value += (settings.relativeTangentEdit ? point.point[inTanChangeRes.value.index] : 0);
                    point.SetTangent(inTanChangeRes.value.Apply(point.inTangent), true, point.editor_only_tangent_mode);
                    return point;
                }, settings.localEdit);
            }

            var outTanChangeRes = EditorGUILayoutEX.MultiVector3Field("Out",
                points.Select((point) => point.outTangent -
                (settings.relativeTangentEdit ? point.point : Vector3.zero)));

            if (outTanChangeRes.didChange)
            {
                EditSelectedPoints((point) =>
                {
                    outTanChangeRes.value.value += (settings.relativeTangentEdit ? point.point[outTanChangeRes.value.index] : 0);
                    point.SetTangent(outTanChangeRes.value.Apply(point.outTangent), false, point.editor_only_tangent_mode);
                    return point;
                }, settings.localEdit);
            }

            GUILayout.Space(5f);

            var angleChangeRes = EditorGUILayoutEX.MultiValueField(points.Select((point)=> point.angle), 
                (angle) => EditorGUILayout.FloatField("Angle", angle));
            if(angleChangeRes.didChange)
            {
                EditSelectedPoints((point) =>
                {
                    point.angle = angleChangeRes.value;
                    return point;
                }, true);
            }

            GUILayout.Space(15f);

            var selected = selectedPoint;
            EditorGUILayoutEX.AlignCenter(() =>
            {
                if (EditorGUILayoutEX.Button("<", !spline.Loop && selected == 0, GUILayout.Width(InspectorButtonSize)))
                {
                    selected--;
                    if (selected < 0)
                        selected = spline.ControlPointsCount - 1;
                }
                EditorGUI.showMixedValue = !singlePointSelected;
                selected = EditorGUILayout.IntField(selected + 1, GUILayout.Width(30)) - 1;
                EditorGUI.showMixedValue = false;
                if (EditorGUILayoutEX.Button(">", !spline.Loop && selected == spline.ControlPointsCount - 1, GUILayout.Width(InspectorButtonSize)))
                {
                    selected++;
                    selected %= spline.ControlPointsCount;
                }
            });
            if (selected != selectedPoint)
                SetSelectedPoint(selected);

            GUILayout.EndVertical();

            if (path.transform.localScale.x != path.transform.localScale.y || path.transform.localScale.x != path.transform.localScale.z)
                EditorGUILayout.HelpBox("non uniform scaling is not supported!", MessageType.Warning);

            if (settingsOpened)
            {
                GUILayout.Space(20);
                EditorGUILayoutEX.AlignCenter(() => GUILayout.Label("Settings"));
                SettingsEditor.OnInspectorGUI();
            }
        }

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

            ProcessSelection();
        }

        private void ProcessSelection()
        {
            if (NavKeyDown())
                return;

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    GetContextMenuPramas(out var rect, out var buttons);
                    if (Event.current.button == 0 && NearestDistance() >= 4.9f 
                        && !rect.Contains(Event.current.mousePosition))
                    {
                        selectionStartPosition = Event.current.mousePosition;
                        inSelection = true;
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && inSelection)
                    {
                        if (pointsInSelection.Count == 0)
                            SetSelectedPoint(selectedPoints[0]);
                        else
                            SetSelectedPoints(pointsInSelection);

                        pointsInSelection.Clear();
                        inSelection = false;
                    }
                    break;
                case EventType.MouseEnterWindow:
                    mouseInsideWindow = true;
                    break;
                case EventType.MouseLeaveWindow:
                    mouseInsideWindow = false;
                    break;
            }


            if (inSelection)
            {
                var rect = new Rect()
                {
                    min = Vector2.Min(Event.current.mousePosition, selectionStartPosition),
                    max = Vector2.Max(Event.current.mousePosition, selectionStartPosition)
                };

                pointsInSelection.Clear();
                for (int i = 0; i < spline.ControlPointsCount; i++)
                {
                    var cpoint = spline.GetControlPoint(i);
                    if (rect.Contains(HandleUtility.WorldToGUIPoint(cpoint.point)))
                        pointsInSelection.Add(i);
                }
            }
        }

        private void OnSceneGUI()
        {
            //if prefab is selected
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) ||
                !path.transform.gameObject.activeSelf)
                return;

            //cancel multi selection and adding tool
            if (cancel)
            {
                SetSelectedPoint(selectedPoints[0]);

                if (addingTool.IsActive)
                    addingTool.Stop();

                Tools.current = tool;
                SceneView.RepaintAll();

                return;
            }

            DrawPath();

            if (addingTool.IsActive)
            {
                DrawControlPointsIcons();

                addingTool.OnSceneGUI(null);
            }
            else if (inSelection)
            {
                DrawControlPointsIcons();
            }
            else 
            {

                if (singlePointSelected)
                {
                    if (settings.smartHandleMode)
                        ControlPointsDragHandlers();
                    else
                        ControlPointsMoveHandlers();

                    DrawAngleHandle(selectedPoint);
                }
                else
                {
                    ControlPointsDragHandlers();

                    MultiPointHandle();
                }

                DrawContextMenu();

                SplitPathControl();
            }

        }

        private void ValidateSpline()
        {
            if(spline.ControlPointsCount == 0)
            {
                var points = new ControlPoint[]
                {
                    new ControlPoint(Vector3.zero, Vector3.back * 0.25f, Vector3.forward * 0.25f),
                    new ControlPoint(Vector3.forward, Vector3.forward * 0.75f, Vector3.forward * 1.25f)
                };
                if (settings.lockTangentByDefault)
                {
                    points[0].editor_only_tangent_mode = TangentMode.Lock;
                    points[1].editor_only_tangent_mode = TangentMode.Lock;
                }
                path.spline = new Spline(points);
                spline = new TransformedSpline(path.spline, path.transform);
                path.ApplyField(nameof(SO.spline));
            }
        }

        private void ValidateSelectedPoints()
        {
            var points = new List<int>(selectedPoints.Where((i)=> i > -1 && i < spline.ControlPointsCount).Distinct());
            if (points.Count == 0)
                points.Add(0);
            if (points.Count != selectedPoints.Count)
                SetSelectedPoints(points);
        }

        #region SinglePointHandle

        private void ControlPointsDragHandlers()
        {
            for (int i = 0; i < spline.ControlPointsCount; i++)
            {
                var cpoint = spline.GetControlPoint(i);

                Drag.normal.SetTexture(resource.controlPoint_normal_texture);
                Drag.hover.SetTexture(resource.controlPoint_hover_texture);
                Drag.active.SetTexture(resource.controlPoint_active_texture);
                if (!singlePointSelected && selectedPoints.Contains(i))
                    Drag.normal.SetTexture(resource.controlPoint_hover_texture);

                Drag.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.point));

                if (Drag.Draw<SphereD>())
                    cpoint.point = SceneRaycast.SmartRaycast(cpoint.point);

                bool dragStart = Drag.Start;

                if (ShouldDrawInTangent(spline,i))
                {
                    Drag.SetAllTransformation(IconDrawCmd(settings.tangentIconSize, cpoint.inTangent));

                    if (Drag.Draw<SphereD>())
                        cpoint.inTangent = SceneRaycast.SmartRaycast(cpoint.inTangent);

                    dragStart |= Drag.Start;
                }

                if (ShouldDrawOutTangent(spline, i))
                {
                    Drag.SetAllTransformation(IconDrawCmd(settings.tangentIconSize, cpoint.outTangent));

                    if (Drag.Draw<SphereD>())
                        cpoint.outTangent = SceneRaycast.SmartRaycast(cpoint.outTangent);

                    dragStart |= Drag.Start;
                }

                if (ApplyControlPoint(i, cpoint) || dragStart)
                {
                    SetSelectedPoint(i);
                    Repaint();
                }
            }
        }

        private void ControlPointsMoveHandlers()
        {
            Button.normal.SetTexture(resource.controlPoint_normal_texture);
            Button.hover.SetTexture(resource.controlPoint_hover_texture);
            Button.active.SetTexture(resource.controlPoint_active_texture);

            for (int i = 0; i < spline.ControlPointsCount; i++)
            {
                var cpoint = spline.GetControlPoint(i);

                if (i != selectedPoint)
                {
                    var select = false;

                    Button.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.point));
                    if (Button.Draw<SphereD>())
                        select = true;

                    if (ShouldDrawInTangent(spline, i))
                    {
                        Button.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.inTangent));
                        if (Button.Draw<SphereD>())
                            select = true;
                    }

                    if (ShouldDrawOutTangent(spline, i))
                    {
                        Button.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, cpoint.outTangent));
                        if (Button.Draw<SphereD>())
                            select = true;
                    }

                    if (select)
                    {
                        SetSelectedPoint(i);
                        Repaint();
                    }
                }
                else
                {
                    cpoint.point = Handles.DoPositionHandle(cpoint.point, path.transform.rotation);
                    if (ShouldDrawInTangent(spline, i))
                        cpoint.inTangent = Handles.DoPositionHandle(cpoint.inTangent, path.transform.rotation);
                    if (ShouldDrawOutTangent(spline, i))
                        cpoint.outTangent = Handles.DoPositionHandle(cpoint.outTangent, path.transform.rotation);

                    ApplyControlPoint(i, cpoint);
                }
            }
        }

        private void DrawAngleHandle(int pointIndex)
        {
            if (!settings.showAngleHandle)
                return;

            var cpoint = spline.GetControlPoint(pointIndex);

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
                spline.SetControlPoint(pointIndex, cpoint);
                path.ApplyField(nameof(SO.spline));
            }
        }

        #endregion

        #region MultiPointHandle

        private void MultiPointHandle()
        {
            var center = GetSelectedPointsCenter();

            if (Tools.current != Tool.None)
            {
                tool = ( Tools.current != Tool.Move && Tools.current != Tool.Rotate && Tools.current != Tool.Scale) ?
                    tool == Tool.None ? Tool.Move : tool : Tools.current;
                Tools.current = Tool.None;
                rotation = path.transform.rotation;
                scale = path.transform.localScale;
            }

            switch (tool)
            {
                case Tool.Move:
                    {
                        if (Event.current.type == EventType.MouseUp)
                            path.ApplyField(nameof(SO.spline));

                        var newCenter = Handles.DoPositionHandle(center, path.transform.rotation);

                        if(newCenter != center)
                            TransformSelectedPoints(center, Matrix4x4.Translate(newCenter - center));
                    }
                    break;
                case Tool.Rotate:
                    {
                        if (Event.current.type == EventType.MouseUp)
                        {
                            rotation = path.transform.rotation;
                            path.ApplyField(nameof(SO.spline));
                        }

                        var newRotation = Handles.DoRotationHandle(rotation, center);
                        var delta = newRotation * Quaternion.Inverse(rotation);

                        if (rotation != newRotation)
                            TransformSelectedPoints(center, Matrix4x4.Rotate(delta));
                        rotation = newRotation;
                    }
                    break;
                case Tool.Scale:
                    {
                        if (Event.current.type == EventType.MouseUp)
                        {
                            scale = path.transform.localScale;
                            path.ApplyField(nameof(SO.spline));
                        }

                        var newScale = Handles.DoScaleHandle(scale, center, path.transform.rotation, 2f);
                        Vector3 delta = new Vector3(newScale.x / scale.x, newScale.y / scale.y, newScale.z / scale.z);

                        if(newScale != scale)
                            TransformSelectedPoints(center, Matrix4x4.Scale(delta));
                        scale = newScale;
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

            if (!singlePointSelected)
            {
                position = GetSelectedPointsCenter();

                //remove points button
                buttons.Add(((GUIStyle, System.Action))(resource.delete_button_style, RemoveSelectedPoints));

                //lock tangent button
                TangentMode tanMode = spline.GetControlPoint(selectedPoints[0]).editor_only_tangent_mode;
                buttons.Add((tanMode == TangentMode.Lock ? resource.lock_on_button_style : resource.lock_off_button_style, ToggleSelectedPointsTan));
            }
            else
            {
                var cpoint = spline.GetControlPoint(selectedPoint);
                position = cpoint.point;

                bool isEndPoint = selectedPoints[0] == 0 || selectedPoints[0] == spline.ControlPointsCount - 1;
                bool only_point = spline.ControlPointsCount == 1;

                //remove point button
                if (!only_point)
                    buttons.Add((resource.delete_button_style, RemoveSelectedPoints));

                if (isEndPoint)
                {
                    //loop button
                    buttons.Add((spline.Loop ?
                    resource.loop_on_button_style : resource.loop_off_button_style, () =>
                    {
                        spline.Loop = !spline.Loop;
                        path.ApplyField(nameof(SO.spline));
                    }
                    ));

                    //add points button
                    buttons.Add((resource.add_button_style, () => addingTool.Start(selectedPoints[0] != 0)));
                }

                //lock tangent button
                if (!isEndPoint || spline.Loop)
                    buttons.Add((cpoint.editor_only_tangent_mode == TangentMode.Lock ?
                        resource.lock_on_button_style : resource.lock_off_button_style, ToggleSelectedPointsTan));
            }

            //show tangent button
            buttons.Add((settings.showTangents ? resource.tangents_on_button_style : resource.tangents_off_button_style, () =>
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

        #region Split Control

        private void SplitPathControl()
        {
            if (Event.current.control && spline.ControlPointsCount > 1)
            {
                if (Event.current.type == EventType.MouseMove)
                    SceneView.RepaintAll();

                var closestPosToMouse = GetClosestLength();
                var pos = spline.GetPoint(closestPosToMouse);
                if (Vector2.Distance(HandleUtility.WorldToGUIPoint(pos), Event.current.mousePosition) <= 50f)
                {
                    Button.normal.SetTexture(resource.controlPoint_normal_texture);
                    Button.hover.SetTexture(resource.controlPoint_normal_texture);
                    Button.active.SetTexture(resource.controlPoint_normal_texture);

                    Button.SetAllTransformation(IconDrawCmd(settings.controlPointIconSize, pos));

                    if (Button.Draw<SphereD>())
                    {
                        var index = spline.Split(closestPosToMouse);
                        var cpoint = spline.GetControlPoint(index);
                        cpoint.editor_only_tangent_mode = settings.lockTangentByDefault ?
                            TangentMode.Lock : TangentMode.Free;
                        spline.SetControlPoint(index, cpoint);
                        path.ApplyField(nameof(SO.spline));
                    }
                }
            }
        }

        private float GetClosestLength()
        {
            int res = 20;

            var mPos = Event.current.mousePosition;

            Segment segment = null;
            var segt = 0f;
            var segDist = 0f;
            var minDist = float.MaxValue;

            float dist = 0;
            foreach (var seg in spline.IterateSegments())
            {
                int i = 0;
                foreach(var point in seg.IteratePoints(res))
                {
                    var d = Vector2.Distance(HandleUtility.WorldToGUIPoint(point), mPos);
                    if (d < minDist)
                    {
                        segment = seg;
                        segt = i/(float)res;
                        segDist = dist;
                        minDist = d;
                    }
                    i++;
                }
                dist += seg.Length;
            }

            {
                float a, b;
                a = segt - 1f / res;
                b = segt + 1f / res;
                var da = Vector2.Distance(HandleUtility.WorldToGUIPoint(segment.GetPoint(a)), mPos);
                var db = Vector2.Distance(HandleUtility.WorldToGUIPoint(segment.GetPoint(b)), mPos);

                for (int i = 0; i < res; i++)
                {
                    var m = (a + b) * 0.5f;
                    var dm = Vector2.Distance(HandleUtility.WorldToGUIPoint(segment.GetPoint(m)), mPos);
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

            return segDist + segment.GetArcLength(segt);
        }

        #endregion

        #region PointEditing

        void SetSelectedPoint(int i)
        {
            selectedPoints.Clear();
            selectedPoints.Add(i);
            path.ApplyField(nameof(SO.selected_points_editor_only));
        }

        void SetSelectedPoints(IEnumerable<int> points)
        {
            if (points.Count() == 0)
                Debug.LogError("selecting zero points");

            selectedPoints.Clear();
            selectedPoints.AddRange(points);
            path.ApplyField(nameof(SO.selected_points_editor_only));
        }

        private bool ApplyControlPoint(int i, ControlPoint cpoint)
        {
            ControlPoint preCpoint = spline.GetControlPoint(i);
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
                spline.SetControlPoint(i, preCpoint);
                path.ApplyField(nameof(SO.spline));
            }

            return didChange;
        }

        private void RemoveSelectedPoints()
        {
            selectedPoints.Sort((a, b) => b.CompareTo(a));
            int count = Mathf.Min(spline.ControlPointsCount - 1, selectedPoints.Count);

            for (int i = 0; i < count; i++)
                spline.RemoveControlPoint(selectedPoints[i]);

            SetSelectedPoint(0);
            path.ApplyField(nameof(SO.spline));
        }

        private void ToggleSelectedPointsTan()
        {
            TangentMode tanMode = spline.GetControlPoint(selectedPoints[0]).editor_only_tangent_mode;

            foreach (var index in selectedPoints)
            {
                var cpoint = spline.GetControlPoint(index);
                cpoint.editor_only_tangent_mode = tanMode.Next();
                cpoint.SetTangent(cpoint.inTangent, true, tanMode.Next());
                spline.SetControlPoint(index, cpoint);
            }

            path.ApplyField(nameof(SO.spline));
        }

        private void EditSelectedPoints(System.Func<ControlPoint,ControlPoint> func , bool local = false)
        {
            if(local)
            {
                foreach (var i in selectedPoints)
                {
                    var point = path.spline.GetControlPoint(i);
                    point = func(point);
                    path.spline.SetControlPoint(i, point);
                }
            }
            else
            {
                foreach (var i in selectedPoints)
                {
                    var point = spline.GetControlPoint(i);
                    point = func(point);
                    spline.SetControlPoint(i, point);
                }
            }
            path.ApplyField(nameof(SO.spline));
        }

        #endregion

        private void DrawControlPointsIcons()
        {
            if (Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < spline.ControlPointsCount; i++)
                {
                    var texture = pointsInSelection.Contains(i) ?
                        resource.controlPoint_hover_texture :
                        resource.controlPoint_normal_texture;
                    var cpoint = spline.GetControlPoint(i);

                    IconDrawCmd(settings.controlPointIconSize, cpoint.point).SetTexture(texture).Draw();

                    if (ShouldDrawInTangent(spline,i))
                        IconDrawCmd(settings.tangentIconSize, cpoint.inTangent).SetTexture(texture).Draw();
                    if (ShouldDrawOutTangent(spline,i))
                        IconDrawCmd(settings.tangentIconSize, cpoint.outTangent).SetTexture(texture).Draw();
                }
            }
        }

        DrawCommand IconDrawCmd(float size, Vector3 position)
        {
            if (settings.screenSpaceIcon)
                return GetDrawCmd().ConstantScreenSize(position).
                Scale(size).LookAtCamera().Move(position);
            else
                return GetDrawCmd().Scale(size * 0.01f).LookAtCamera().Move(position);
        }

        private static bool ShouldDrawInTangent(ISpline spline,int pointIndex)
        {
            return (pointIndex != 0 || spline.Loop) && PathSettings.GetSettings().settings.showTangents;
        }

        private static bool ShouldDrawOutTangent(ISpline spline, int pointIndex)
        {
            return (pointIndex != spline.ControlPointsCount - 1 || spline.Loop ) && PathSettings.GetSettings().settings.showTangents;
        }

        #region DrawGizmos

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        private static void DrawGizmosNotSelected(Path path, GizmoType type)
        {
            using(SO so = new SO(path))
            {
                DrawSpline(so.spline, so.transform.localToWorldMatrix,
                    Gizmos.DrawLine, (color) => Gizmos.color = color);
            }
        }

        private void DrawPath()
        {
            DrawSpline(path.spline, path.transform.localToWorldMatrix,
            (a, b) => Handles.DrawAAPolyLine(a, b),
            (color) => Handles.color = color);

            DrawTangents(path.spline, path.transform.localToWorldMatrix);
        }

        private static void DrawTangents(ISpline spline, Matrix4x4 matrix)
        {
            var settings = PathSettings.GetSettings().settings;

            for (int i = 0; i < spline.ControlPointsCount; i++)
            {
                var cpoint = spline.GetControlPoint(i);
                cpoint.point = matrix.MultiplyPoint(cpoint.point);
                cpoint.inTangent = matrix.MultiplyPoint(cpoint.inTangent);
                cpoint.outTangent = matrix.MultiplyPoint(cpoint.outTangent);

                Handles.color = settings.tangentColor;
                if (ShouldDrawInTangent(spline,i))
                    Handles.DrawAAPolyLine(cpoint.point, cpoint.inTangent);
                if (ShouldDrawOutTangent(spline,i))
                    Handles.DrawAAPolyLine(cpoint.point, cpoint.outTangent);
            }
        }

        private static void DrawSpline(ISpline spline, Matrix4x4 matrix, System.Action<Vector3, Vector3> DrawLine,
            System.Action<Color> setColor)
        {
            var settings = PathSettings.GetSettings().settings.drawSettings;

            DrawDirection(spline, matrix, DrawLine, setColor, settings);

            float step = 1f / settings.resolution;

            foreach (var seg in spline.IterateSegments())
            {
                int it = 0;
                Sample preSample = default;
                System.Func<Sample, float, Vector3> GetSide = (sample, delta)
                       => sample.point + sample.right * delta;
                foreach (var sample in seg.IterateSamples(settings.resolution))
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
                }
            }
        }

        private static void DrawDirection(ISpline spline, Matrix4x4 matrix, System.Action<Vector3, Vector3> DrawLine,
            System.Action<Color> setColor, SplineDrawSettings settings)
        {
            float step = 1f / settings.resolution;

            var firstSegment = spline.GetSegment(0);
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
}