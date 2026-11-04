// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>Meshes of the shot effects: fire puffs and the fragments of destroyed targets (radius about 1).</summary>
    static class EffectModels
    {
        /// <summary>Half glowing ball: bright, but the light bands still show a round ball of fire.</summary>
        public static Mesh Puff(PuffColor color)
        {
            var b = new ToonMeshBuilder().Style(Tint(color), 0.45f);
            b.Gloss = 0.6f;
            b.Sphere(1f, 2);
            return b.Build("GazeLab Puff " + color);
        }

        /// <summary>Lumpy rock chunk: an icosahedron with random corner distances.</summary>
        public static Mesh RockChunk(int variant)
        {
            var random = new SeededRandom(31 + variant * 101);
            List<Vector3> dirs;
            List<int> faces;
            ToonMeshBuilder.Icosphere(0, out dirs, out faces);
            for (int i = 0; i < dirs.Count; i++) dirs[i] *= random.Range(0.6f, 1f);
            var b = new ToonMeshBuilder { Gloss = 0.15f }.Style(variant % 2 == 0 ? HudTheme.RockOrange : ModelPalette.CraterDark);
            for (int f = 0; f < faces.Count; f += 3) b.Triangle(dirs[faces[f]], dirs[faces[f + 1]], dirs[faces[f + 2]], Vector3.zero);
            return b.Build("GazeLab Rock Chunk " + variant);
        }

        /// <summary>Satellite fragment: a broken solar panel (variants 0, 1) or a metal or gold shard (2, 3).</summary>
        public static Mesh SatellitePart(int variant)
        {
            var b = new ToonMeshBuilder();
            if (variant < 2)
            {
                b.Style(ModelPalette.PanelBacking);
                b.Box(new Vector3(1.6f, 1.1f, 0.12f));
                b.Style(HudTheme.SatelliteBlue);
                foreach (float x in new[] { -0.4f, 0.4f })
                {
                    b.Part(new Vector3(x, 0f, 0f));
                    b.Box(new Vector3(0.7f, 0.95f, 0.2f));
                }
            }
            else
            {
                b.Part(Vector3.zero, new Vector3(20f, 35f, 10f), Vector3.one).Style(variant == 2 ? ModelPalette.Metal : ModelPalette.Gold);
                b.Box(new Vector3(1.2f, 0.7f, 0.5f));
            }
            return b.Build("GazeLab Satellite Part " + variant);
        }

        static Color Tint(PuffColor color)
        {
            switch (color)
            {
                case PuffColor.White: return ModelPalette.FlashWhite;
                case PuffColor.Yellow: return HudTheme.ShipYellow;
                case PuffColor.Orange: return ModelPalette.Flame;
                default: return HudTheme.ShipRed;
            }
        }
    }
}
