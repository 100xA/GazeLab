// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GazeLab
{
    public static class DisplayScaling
    {
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);

        public static float Current()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorGUIUtility.pixelsPerPoint;
#else
            try
            {
                uint dpi = GetDpiForWindow(GetActiveWindow());
                if (dpi > 0) return dpi / 96f;
            }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
            return Screen.dpi > 0 ? Screen.dpi / 96f : 1f;
#endif
        }

        public static Vector2 Compensate(Vector2 viewport, Rect bounds, float scale)
        {
            if (scale <= 0f || bounds.width < 1e-4f || bounds.height < 1e-4f ||
                float.IsNaN(bounds.x) || float.IsNaN(bounds.y)) return viewport;
            float bottom = 1f - bounds.height - bounds.y;
            float wx = bounds.x + viewport.x * bounds.width;
            float wy = bottom + viewport.y * bounds.height;
            wx = 0.5f + (wx - 0.5f) * scale;
            wy = 0.5f + (wy - 0.5f) * scale;
            return new Vector2((wx - bounds.x) / bounds.width, (wy - bottom) / bounds.height);
        }
    }
}
