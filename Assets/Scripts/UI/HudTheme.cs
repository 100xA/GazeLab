// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Look of the HUD, matched to the flat cartoon sprites: colours taken from the ship (red, yellow,
    /// teal), the rock (orange) and the satellite (blue); chunky rounded "plates" with a darker lip;
    /// Lilita One for headings and Varela Round for text (both SIL Open Font License, in Resources/Fonts).
    /// Layout values are design units for a 1080p screen, scaled with the Game view height.
    /// </summary>
    public sealed class HudTheme
    {
        // Palette from the game sprites
        public static readonly Color ShipRed = new Color(0.93f, 0.25f, 0.22f);
        public static readonly Color ShipYellow = new Color(0.98f, 0.84f, 0.29f);
        public static readonly Color CockpitTeal = new Color(0.36f, 0.66f, 0.73f);
        public static readonly Color RockOrange = new Color(0.86f, 0.5f, 0.24f);
        public static readonly Color SatelliteBlue = new Color(0.2f, 0.55f, 0.92f);
        public static readonly Color WingGreen = new Color(0.47f, 0.8f, 0.45f);

        // UI roles
        public static readonly Color Panel = new Color(0.09f, 0.14f, 0.2f, 0.96f);
        public static readonly Color PanelLight = new Color(0.14f, 0.21f, 0.29f, 0.98f);
        public static readonly Color Ink = new Color(0.04f, 0.07f, 0.1f);
        public static readonly Color Foreground = new Color(0.95f, 0.97f, 1f);
        public static readonly Color Muted = new Color(0.66f, 0.76f, 0.82f);
        public static readonly Color Good = WingGreen;
        public static readonly Color Warn = new Color(0.98f, 0.64f, 0.16f);
        public static readonly Color Bad = ShipRed;
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.12f);

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

        /// <summary>Darker shade of a colour, used for the lip under plates and text shadows.</summary>
        public static Color Shade(Color color, float factor = 0.55f)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        public GUIStyle Label, LabelStrong, Value, Body, BodyMuted, Small, Chip, Button, Callout, Title, Subtitle, Banner, TileTitle, TileBody, Badge, Hit;
        public Texture2D Dot { get; private set; }

        Texture2D fill, fillSmall, ring;
        GUIStyle fillStyle, fillSmallStyle, ringStyle;
        Font headingFont, bodyFont;
        float builtForScale = -1f;

        /// <summary>Call at the start of OnGUI; rebuilds fonts and textures when the scale changes.</summary>
        public void Prepare()
        {
            if (Dot == null) Dot = MakeRound(32, 15.5f, 0f);
            if (headingFont == null) headingFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (bodyFont == null) bodyFont = Resources.Load<Font>("Fonts/VarelaRound-Regular");
            if (fillStyle != null && Mathf.Approximately(builtForScale, Scale)) return;
            float s = builtForScale = Scale;

            int radius = Mathf.Max(5, Mathf.RoundToInt(14 * s));
            int smallRadius = Mathf.Max(3, Mathf.RoundToInt(5 * s));
            Release(fill);
            Release(fillSmall);
            Release(ring);
            fill = MakeRound(radius * 2 + 2, radius, 0f);
            fillSmall = MakeRound(smallRadius * 2 + 2, smallRadius, 0f);
            fillSmallStyle = NineSlice(fillSmall, smallRadius);
            ring = MakeRound(radius * 2 + 2, radius, Mathf.Max(2f, Mathf.Round(3f * s)));
            fillStyle = NineSlice(fill, radius);
            ringStyle = NineSlice(ring, radius);

            Label = Font(bodyFont, 12, TextAnchor.UpperLeft);
            LabelStrong = Font(headingFont, 16, TextAnchor.UpperLeft);
            Value = Font(headingFont, 40, TextAnchor.UpperLeft);
            Body = Font(bodyFont, 15, TextAnchor.MiddleLeft);
            BodyMuted = Font(bodyFont, 13, TextAnchor.MiddleLeft);
            Small = Font(bodyFont, 12, TextAnchor.MiddleLeft);
            Chip = Font(headingFont, 14, TextAnchor.MiddleCenter);
            Button = Font(headingFont, 16, TextAnchor.MiddleCenter);
            Callout = Font(headingFont, 24, TextAnchor.MiddleCenter);
            Title = Font(headingFont, 52, TextAnchor.MiddleCenter);
            Subtitle = Font(bodyFont, 20, TextAnchor.MiddleCenter);
            Banner = Font(headingFont, 46, TextAnchor.MiddleCenter);
            TileTitle = Font(headingFont, 28, TextAnchor.UpperLeft);
            TileBody = Font(bodyFont, 16, TextAnchor.UpperLeft);
            Badge = Font(headingFont, 24, TextAnchor.MiddleCenter);
            Hit = Font(headingFont, 64, TextAnchor.MiddleCenter);
        }

        public void Release()
        {
            Release(Dot);
            Release(fill);
            Release(fillSmall);
            Release(ring);
        }

        // ---------- drawing ----------

        /// <summary>Flat rounded rectangle.</summary>
        public void Box(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = color;
            // Small elements (bars, segments) get a smaller corner radius so the 9-slice fits.
            GUIStyle style = Mathf.Min(rect.width, rect.height) < fillStyle.border.left * 2 ? fillSmallStyle : fillStyle;
            style.Draw(rect, false, false, false, false);
        }

        /// <summary>Chunky rounded plate with a darker lip below, like the game's flat sprites.</summary>
        public void Plate(Rect rect, Color color, float lip = 5f)
        {
            float l = lip * Scale;
            Box(new Rect(rect.x, rect.y + l, rect.width, rect.height), Shade(color));
            Box(rect, color);
        }

        /// <summary>Thick rounded outline.</summary>
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

        public static void Write(Rect rect, string text, GUIStyle style, Color color)
        {
            GUI.color = Color.white;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        /// <summary>Heading text with a hard drop shadow (cartoon style).</summary>
        public static void Shadowed(Rect rect, string text, GUIStyle style, Color color, Color shadow, float offset = 3f)
        {
            float o = offset * Scale;
            Write(new Rect(rect.x, rect.y + o, rect.width, rect.height), text, style, shadow);
            Write(rect, text, style, color);
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
            const int Segments = 72;
            int count = Mathf.CeilToInt(Mathf.Clamp01(progress) * Segments);
            for (int i = 0; i < count; i++)
            {
                // Segments overlap slightly so thick rings have no gaps.
                float a = (i - 0.3f) * Mathf.PI * 2 / Segments - Mathf.PI / 2;
                float b = (i + 1.3f) * Mathf.PI * 2 / Segments - Mathf.PI / 2;
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

        static GUIStyle Font(Font font, int size, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = Mathf.RoundToInt(size * Scale), alignment = anchor, wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0)
            };
            if (font == null) style.fontStyle = size >= 16 ? FontStyle.Bold : FontStyle.Normal; // fallback: Arial
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
