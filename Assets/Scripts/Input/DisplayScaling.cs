// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// With Windows display scaling the Tobii runtime reports gaze positions inside the tracked window
    /// shrunk towards the window centre by the scaling factor (at 150 %, looking at the top-left corner
    /// lands about a third of the way to the centre). <see cref="Compensate"/> undoes that.
    /// </summary>
    public static class DisplayScaling
    {
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);

        /// <summary>Scaling of the game window: 1 = 100 %, 1.5 = 150 %.</summary>
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

        /// <summary>
        /// Converts a Game-view viewport point back to window-normalised coordinates (inverse of Tobii's
        /// GazePointDataProvider), stretches it around the window centre and converts it back.
        /// </summary>
        /// <param name="bounds">Game image inside the tracked window, as normalised by the Tobii SDK.</param>
        public static Vector2 Compensate(Vector2 viewport, Rect bounds, float scale)
        {
            if (scale <= 0f || bounds.width < 1e-4f || bounds.height < 1e-4f ||
                float.IsNaN(bounds.x) || float.IsNaN(bounds.y)) return viewport;
            float bottom = 1f - bounds.height - bounds.y; // window-normalised, bottom-up
            float wx = bounds.x + viewport.x * bounds.width;
            float wy = bottom + viewport.y * bounds.height;
            wx = 0.5f + (wx - 0.5f) * scale;
            wy = 0.5f + (wy - 0.5f) * scale;
            return new Vector2((wx - bounds.x) / bounds.width, (wy - bottom) / bounds.height);
        }
    }
}
