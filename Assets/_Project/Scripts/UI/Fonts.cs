using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>OS fonts with sensible fallbacks, cached as IMGUI styles per size.</summary>
    public static class Fonts
    {
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

        static readonly string[] UiNames = { "Tahoma", "Verdana", "Segoe UI", "Helvetica", "Arial", "Liberation Sans", "DejaVu Sans" };
        static readonly string[] MonoNames = { "Courier New", "Lucida Console", "Consolas", "Menlo", "Liberation Mono", "DejaVu Sans Mono" };
        static readonly string[] HandNames = { "Segoe Print", "Bradley Hand", "Ink Free", "Comic Sans MS", "Georgia" };
        static readonly string[] SerifNames = { "Georgia", "Times New Roman", "Liberation Serif", "DejaVu Serif" };

        static Font Get(string key, string[] names)
        {
            if (fonts.TryGetValue(key, out var f) && f != null) return f;
            var over = Resources.Load<Font>("ThirdLamp/Fonts/" + key);
            f = over != null ? over : Font.CreateDynamicFontFromOSFont(names, 16);
            fonts[key] = f;
            return f;
        }

        static GUIStyle Style(string key, string[] names, int size, FontStyle fs = FontStyle.Normal)
        {
            string k = key + size + fs;
            if (styles.TryGetValue(k, out var s)) return s;
            s = new GUIStyle { font = Get(key, names), fontSize = Mathf.Max(8, size), fontStyle = fs, richText = false, clipping = TextClipping.Clip };
            styles[k] = s;
            return s;
        }

        public static GUIStyle UI(int size) => Style("ui", UiNames, size);
        public static GUIStyle UIBold(int size) => Style("ui", UiNames, size, FontStyle.Bold);
        public static GUIStyle Mono(int size) => Style("mono", MonoNames, size);
        public static GUIStyle Hand(int size) => Style("hand", HandNames, size);
        public static GUIStyle Serif(int size) => Style("serif", SerifNames, size);
    }
}
