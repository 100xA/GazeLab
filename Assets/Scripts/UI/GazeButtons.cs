// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Gaze-only buttons: looking at one for <see cref="DwellSeconds"/> activates it. Gaze slightly
    /// outside a button still counts, and short gaze loss (blink, jitter) freezes the progress.
    /// Pure logic; drawing happens in <see cref="MenuView"/>.
    /// </summary>
    public sealed class GazeButtons
    {
        public struct Button
        {
            public Rect Rect;          // GUI pixels
            public string Number, Title, Body;
            public Color Accent;
            public Action Action;
        }

        public const float DwellSeconds = 1f;
        /// <summary>New buttons ignore the gaze briefly, so a menu appearing under the eyes is not triggered by accident.</summary>
        const float ArmDelaySeconds = 0.6f;
        const float ToleranceDesignUnits = 40f;
        const float LossToleranceSeconds = 0.3f;

        readonly List<Button> buttons = new List<Button>();
        float lastHitTime = float.NegativeInfinity;
        float armedAt = float.NaN;

        public IList<Button> Items { get { return buttons; } }
        public int Hovered { get; private set; } = -1;
        public float Progress { get; private set; }

        public void Set(IEnumerable<Button> items)
        {
            buttons.Clear();
            buttons.AddRange(items);
            Release();
            armedAt = float.NaN; // armed relative to the next update
        }

        public bool Contains(Vector2 guiPoint)
        {
            foreach (Button button in buttons) if (button.Rect.Contains(guiPoint)) return true;
            return false;
        }

        /// <summary>Advances the dwell of the button under the gaze; activates it when complete.</summary>
        public void Update(bool gazeValid, Vector2 guiPoint, float now, float deltaTime)
        {
            if (float.IsNaN(armedAt)) armedAt = now + ArmDelaySeconds;
            if (now < armedAt) return;
            int hit = gazeValid ? ButtonAt(guiPoint) : -1;
            if (hit < 0)
            {
                if (Hovered >= 0 && now - lastHitTime <= LossToleranceSeconds) return;
                Release();
                return;
            }
            lastHitTime = now;
            if (hit != Hovered) { Hovered = hit; Progress = 0f; }
            Progress += deltaTime / DwellSeconds;
            if (Progress < 1f) return;
            Action action = buttons[hit].Action;
            Release();
            if (action != null) action();
        }

        void Release()
        {
            Hovered = -1;
            Progress = 0f;
        }

        int ButtonAt(Vector2 guiPoint)
        {
            int best = -1;
            float bestDistance = ToleranceDesignUnits * HudTheme.Scale;
            for (int i = 0; i < buttons.Count; i++)
            {
                Rect r = buttons[i].Rect;
                float dx = Mathf.Max(r.xMin - guiPoint.x, 0f, guiPoint.x - r.xMax);
                float dy = Mathf.Max(r.yMin - guiPoint.y, 0f, guiPoint.y - r.yMax);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance <= bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }
    }
}
