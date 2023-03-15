using AlaslTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BasicSpline
{
    public class PathEditorResource : System.IDisposable
    {
        public Texture2D worldspace_icon { get; private set; }
        public Texture2D localspace_icon { get; private set; }
        public Texture2D settings_icon { get; private set; }

        public Texture2D controlPoint_normal_texture { get; private set; }
        public Texture2D controlPoint_hover_texture { get; private set; }
        public Texture2D controlPoint_active_texture { get; private set; }

        public Texture2D delete_button_normal { get; private set; }
        public Texture2D delete_button_hover { get; private set; }
        public Texture2D lock_on_button_normal { get; private set; }
        public Texture2D lock_on_button_hover { get; private set; }
        public Texture2D lock_off_button_normal { get; private set; }
        public Texture2D lock_off_button_hover { get; private set; }
        public Texture2D loop_on_button_normal { get; private set; }
        public Texture2D loop_on_button_hover { get; private set; }
        public Texture2D loop_off_button_normal { get; private set; }
        public Texture2D loop_off_button_hover { get; private set; }
        public Texture2D tangents_on_button_normal { get; private set; }
        public Texture2D tangents_on_button_hover { get; private set; }
        public Texture2D tangents_off_button_normal { get; private set; }
        public Texture2D tangents_off_button_hover { get; private set; }
        public Texture2D add_button_normal { get; private set; }
        public Texture2D add_button_hover { get; private set; }

        public GUIStyle delete_button_style { get; private set; }
        public GUIStyle lock_on_button_style { get; private set; }
        public GUIStyle lock_off_button_style { get; private set; }
        public GUIStyle loop_on_button_style { get; private set; }
        public GUIStyle loop_off_button_style { get; private set; }
        public GUIStyle tangents_on_button_style { get; private set; }
        public GUIStyle tangents_off_button_style { get; private set; }
        public GUIStyle add_button_style { get; private set; }

        private string basePath;

        public PathEditorResource()
        {
            basePath = System.IO.Path.Combine(EditorHelper.GetAssemblyDirectory<PathEditor>(), "Resources");

            localspace_icon = GetTexture("localSpace_icon.psd");
            worldspace_icon = GetTexture("worldSpace_icon.psd");
            settings_icon = GetTexture("settings_icon.psd");

            controlPoint_normal_texture = GetTexture("controlPoint_normal.psd");
            controlPoint_hover_texture = GetTexture("controlPoint_hover.psd");
            controlPoint_active_texture = GetTexture("controlPoint_active.psd");

            delete_button_normal = GetTexture("delete_normal.psd");
            delete_button_hover = GetTexture("delete_hover.psd");
            delete_button_style = GetButtonStyle(delete_button_normal,delete_button_hover);

            lock_on_button_normal = GetTexture("lock_on_normal.psd");
            lock_on_button_hover = GetTexture("lock_on_hover.psd");
            lock_on_button_style = GetButtonStyle(lock_on_button_normal,lock_on_button_hover);

            lock_off_button_normal = GetTexture("lock_off_normal.psd");
            lock_off_button_hover = GetTexture("lock_off_hover.psd");
            lock_off_button_style = GetButtonStyle(lock_off_button_normal,lock_off_button_hover);

            loop_on_button_normal = GetTexture("loop_on_normal.psd");
            loop_on_button_hover = GetTexture("loop_on_hover.psd");
            loop_on_button_style = GetButtonStyle(loop_on_button_normal, loop_on_button_hover);

            loop_off_button_normal = GetTexture("loop_off_normal.psd");
            loop_off_button_hover = GetTexture("loop_off_hover.psd");
            loop_off_button_style = GetButtonStyle(loop_off_button_normal, loop_off_button_hover);

            add_button_normal = GetTexture("add_normal.psd");
            add_button_hover = GetTexture("add_hover.psd");
            add_button_style = GetButtonStyle(add_button_normal,add_button_hover);

            tangents_on_button_normal = GetTexture("tangents_on_normal.psd");
            tangents_on_button_hover = GetTexture("tangents_on_hover.psd");
            tangents_on_button_style = GetButtonStyle(tangents_on_button_normal,tangents_on_button_hover);

            tangents_off_button_normal = GetTexture("tangents_off_normal.psd");
            tangents_off_button_hover = GetTexture("tangents_off_hover.psd");
            tangents_off_button_style = GetButtonStyle(tangents_off_button_normal, tangents_off_button_hover);
        }

        private Texture2D GetTexture(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>
                (System.IO.Path.Combine(basePath, name));
        }

        private GUIStyle GetButtonStyle(Texture2D normal, Texture2D hover)
        {
            GUIStyle style = new GUIStyle();
            style.stretchWidth = true;
            style.stretchHeight = true;
            style.imagePosition = ImagePosition.ImageOnly;
            style.normal.background = normal;
            style.hover.background = hover;
            return style;
        }

        public void Dispose()
        {

        }
    }
}