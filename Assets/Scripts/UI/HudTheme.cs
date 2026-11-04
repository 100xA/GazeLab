// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Colours, fonts and drawing helpers of the HUD. Layout values are given for a 1080p screen
    /// ("design units") and scaled with the Game view height, so the HUD keeps its size on 4K displays.
    /// </summary>
    public sealed class HudTheme
    {
        public static readonly Color Panel = new Color(0.035f, 0.06f, 0.12f, 0.88f);
        public static readonly Color PanelSolid = new Color(0.035f, 0.06f, 0.12f, 0.97f);
        public static readonly Color Edge = new Color(0.35f, 0.85f, 1f, 0.28f);
        public static readonly Color Accent = new Color(0.35f, 0.88f, 1f);
        public static readonly Color Good = new Color(0.32f, 1f, 0.58f);
        public static readonly Color Warn = new Color(1f, 0.74f, 0.25f);
        public static readonly Color Bad = new Color(1f, 0.38f, 0.38f);
        public static readonly Color Muted = new Color(0.62f, 0.7f, 0.82f);
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.1f);
        public static readonly Color OnAccent = new Color(0.03f, 0.06f, 0.12f);

        /// <summary>HUD scale: 1 at 1080p, larger on 4K Game views.</summary>
        public static float Scale { get { return Mathf.Clamp(Screen.height / 1080f, 0.75f, 3f); } }
        public static float Width { get { return Screen.width / Scale; } }
        public static float Height { get { return Screen.height / Scale; } }

        /// <summary>Design-unit rectangle to GUI pixels.</summary>
        public static Rect R(float x, float y, float w, float h)
        {
            float s = Scale;
            return new Rect(x * s, y * s, w * s, h * s);
        }

        /// <summary>Unity screen point (bottom-left origin) to GUI point (top-left origin).</summary>
        public static Vector2 ToGui(Vector2 screenPoint) { return new Vector2(screenPoint.x, Screen.height - screenPoint.y); }

        public GUIStyle Caption, Value, Body, BodyMuted, Small, Chip, Segment, Center, Huge, TileTitle, TileBody, Badge;
        public Texture2D Dot { get; private set; }

        Texture2D fill, ring;
        GUIStyle fillStyle, ringStyle;
        float builtForScale = -1f;

        /// <summary>Call at the start of OnGUI; rebuilds fonts and textures when the scale changes.</summary>
        public void Prepare()
        {
            if (Dot == null) Dot = MakeRound(32, 15.5f, 0f);
            if (fillStyle != null && Mathf.Approximately(builtForScale, Scale)) return;
            float s = builtForScale = Scale;

            int radius = Mathf.Max(4, Mathf.RoundToInt(10 * s));
            Release(fill);
            Release(ring);
            fill = MakeRound(radius * 2 + 2, radius, 0f);
            ring = MakeRound(radius * 2 + 2, radius, Mathf.Max(1f, Mathf.Round(1.2f * s)));
            fillStyle = NineSlice(fill, radius);
            ringStyle = NineSlice(ring, radius);

            Caption = Font(11, FontStyle.Bold, TextAnchor.UpperLeft);
            Value = Font(34, FontStyle.Bold, TextAnchor.UpperLeft);
            Body = Font(14, FontStyle.Normal, TextAnchor.MiddleLeft);
            BodyMuted = Font(13, FontStyle.Normal, TextAnchor.MiddleLeft);
            Small = Font(12, FontStyle.Normal, TextAnchor.MiddleLeft);
            Chip = Font(12, FontStyle.Bold, TextAnchor.MiddleCenter);
            Segment = Font(13, FontStyle.Bold, TextAnchor.MiddleCenter);
            Center = Font(20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Huge = Font(34, FontStyle.Bold, TextAnchor.MiddleCenter);
            TileTitle = Font(24, FontStyle.Bold, TextAnchor.UpperLeft);
            TileBody = Font(15, FontStyle.Normal, TextAnchor.UpperLeft);
            Badge = Font(18, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        public void Release()
        {
            Release(Dot);
            Release(fill);
            Release(ring);
        }

        // ---------- drawing ----------

        /// <summary>Filled rounded rectangle.</summary>
        public void Box(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = color;
            fillStyle.Draw(rect, false, false, false, false);
        }

        /// <summary>Rounded outline.</summary>
        public void Outline(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = color;
            ringStyle.Draw(rect, false, false, false, false);
        }

        public void Circle(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Dot);
        }

        public static void Text(Rect rect, string text, GUIStyle style, Color color)
        {
            GUI.color = Color.white;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        /// <summary>Invisible button over an already drawn control.</summary>
        public static bool Clicked(Rect rect) { return GUI.Button(rect, GUIContent.none, GUIStyle.none); }

        public static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        public static void Line(Vector2 start, Vector2 end, float width)
        {
            Matrix4x4 previous = GUI.matrix;
            Vector2 delta = end - start;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width / 2, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = previous;
        }

        /// <summary>Ring from 12 o'clock clockwise, filled to <paramref name="progress"/> (0..1).</summary>
        public static void Ring(Vector2 center, float radius, float progress, float width)
        {
            const int Segments = 48;
            int count = Mathf.CeilToInt(Mathf.Clamp01(progress) * Segments);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / Segments - Mathf.PI / 2;
                float b = (i + 1) * Mathf.PI * 2 / Segments - Mathf.PI / 2;
                Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                     center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width);
            }
        }

        /// <summary>Corner brackets around a rectangle (target highlight).</summary>
        public static void Corners(Rect r, float width, float length)
        {
            Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + length, r.yMin), width);
            Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + length), width);
            Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax - length, r.yMin), width);
            Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + length), width);
            Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + length, r.yMax), width);
            Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - length), width);
            Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - length, r.yMax), width);
            Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - length), width);
        }

        // ---------- construction ----------

        static GUIStyle Font(int size, FontStyle fontStyle, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(size * Scale), fontStyle = fontStyle, alignment = anchor, wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0)
            };
            style.normal.textColor = Color.white;
            return style;
        }

        static GUIStyle NineSlice(Texture2D texture, int border)
        {
            var style = new GUIStyle { border = new RectOffset(border, border, border, border) };
            style.normal.background = texture;
            return style;
        }

        /// <summary>Anti-aliased disc, or a ring of the given thickness.</summary>
        static Texture2D MakeRound(int size, float radius, float thickness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float alpha = Mathf.Clamp01(radius - d + 0.5f);
                    if (thickness > 0f) alpha *= Mathf.Clamp01(d - (radius - thickness) + 0.5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            texture.Apply();
            return texture;
        }

        static void Release(Texture2D texture) { if (texture != null) Object.Destroy(texture); }
    }
}
