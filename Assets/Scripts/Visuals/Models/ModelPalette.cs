// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Colours of the 3D models. The main colours are the HUD palette (<see cref="HudTheme"/>), so
    /// models and HUD match; these are the extra material colours used only by the models.
    /// </summary>
    static class ModelPalette
    {
        public static readonly Color Metal = new Color(0.74f, 0.79f, 0.85f);
        public static readonly Color DarkMetal = new Color(0.3f, 0.35f, 0.42f);
        public static readonly Color Gold = new Color(0.96f, 0.74f, 0.27f);
        public static readonly Color PanelBacking = new Color(0.1f, 0.19f, 0.36f);
        public static readonly Color Flame = new Color(1f, 0.5f, 0.14f);
        public static readonly Color CraterDark = new Color(0.5f, 0.26f, 0.13f);
        public static readonly Color FlashWhite = new Color(1f, 0.97f, 0.85f);
    }
}
