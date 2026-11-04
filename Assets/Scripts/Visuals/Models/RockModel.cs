// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// A lumpy orange rock (radius about 1) with dented, dark craters and lighter rims. Every variant
    /// has its own seed, so the shapes differ but are the same in every run.
    /// </summary>
    static class RockModel
    {
        struct Crater { public Vector3 Direction; public float Radius, Depth; }

        public static Mesh Build(int variant)
        {
            var random = new SeededRandom(1009 + variant * 7919);
            SeededRandom.SineField lumps = random.Field(1.2f, 2.2f);
            SeededRandom.SineField bumps = random.Field(4f, 6.5f);

            var craters = new Crater[3 + variant % 3];
            for (int i = 0; i < craters.Length; i++)
                craters[i] = new Crater { Direction = random.Direction(), Radius = random.Range(0.3f, 0.5f), Depth = random.Range(0.1f, 0.16f) };

            List<Vector3> dirs;
            List<int> faces;
            ToonMeshBuilder.Icosphere(3, out dirs, out faces);
            var shape = new Vector3(1f, random.Range(0.74f, 0.86f), random.Range(0.85f, 1f));
            var points = new Vector3[dirs.Count];
            for (int i = 0; i < dirs.Count; i++)
            {
                Vector3 d = dirs[i];
                float radius = 1f + 0.2f * lumps.Sample(d) + 0.04f * bumps.Sample(d);
                Crater crater;
                float t = NearestCrater(craters, d, out crater);
                if (t < 1f) radius -= crater.Depth * (1f - t * t);                                // bowl
                else if (t < 1.35f) radius += crater.Depth * 0.35f * (1f - (t - 1f) / 0.35f);    // raised rim
                points[i] = Vector3.Scale(d * radius, shape);
            }

            var b = new ToonMeshBuilder { Gloss = 0.15f };
            Color rim = HudTheme.RockOrange * 1.12f;
            var unshape = new Vector3(1f / shape.x, 1f / shape.y, 1f / shape.z);
            b.Paint = centre =>
            {
                Vector3 d = Vector3.Scale(centre, unshape).normalized;
                Crater crater;
                float t = NearestCrater(craters, d, out crater);
                if (t < 0.75f) return ModelPalette.CraterDark;
                if (t > 1f && t < 1.3f) return rim;
                // Slight variation per facet so the surface reads as rock.
                return HudTheme.RockOrange * (0.93f + 0.09f * bumps.Sample(d * 1.7f));
            };
            for (int f = 0; f < faces.Count; f += 3)
                b.Triangle(points[faces[f]], points[faces[f + 1]], points[faces[f + 2]], Vector3.zero);
            return b.Build("GazeLab Rock " + variant);
        }

        /// <summary>Distance to the nearest crater centre in crater radii (below 1 = inside).</summary>
        static float NearestCrater(Crater[] craters, Vector3 direction, out Crater nearest)
        {
            nearest = craters[0];
            float best = float.PositiveInfinity;
            foreach (Crater crater in craters)
            {
                float t = Vector3.Angle(direction, crater.Direction) * Mathf.Deg2Rad / crater.Radius;
                if (t < best) { best = t; nearest = crater; }
            }
            return best;
        }
    }
}
