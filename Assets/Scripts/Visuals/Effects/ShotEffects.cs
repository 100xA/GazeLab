// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Cartoon effects of a shot: muzzle flash at the cannon and an explosion of fire puffs and
    /// tumbling fragments where a target was destroyed. Purely visual (no colliders).
    /// </summary>
    public static class ShotEffects
    {
        public static void MuzzleFlash(Vector2 muzzle)
        {
            var at = new Vector3(muzzle.x, muzzle.y, -0.6f);
            Puff(at, PuffColor.Orange, 0.45f, 0.22f, 0f, Vector3.zero);
            Puff(at + new Vector3(0f, 0f, -0.05f), PuffColor.White, 0.28f, 0.14f, 0f, Vector3.zero);
        }

        /// <summary>Explosion of a destroyed target: fire puffs and rock chunks or satellite parts.</summary>
        public static void Explode(Vector3 centre, float radius, TargetKind kind)
        {
            centre.z -= 0.3f; // in front of the other targets in this depth layer
            Puff(centre, PuffColor.White, radius * 0.8f, 0.2f, 0f, Vector3.zero);
            Puff(centre + new Vector3(0f, 0f, 0.05f), PuffColor.Orange, radius * 1.05f, 0.5f, 0f, Vector3.zero);
            PuffColor[] colors = { PuffColor.Yellow, PuffColor.Orange, PuffColor.Red };
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius * 0.6f;
                var at = centre + new Vector3(offset.x, offset.y, -0.02f * (i + 1));
                Puff(at, colors[i % colors.Length], radius * Random.Range(0.35f, 0.6f), Random.Range(0.35f, 0.55f),
                     Random.Range(0f, 0.12f), (Vector3)offset.normalized * Random.Range(1f, 2.5f));
            }

            int pieces = kind == TargetKind.Rock ? 10 : 8;
            for (int i = 0; i < pieces; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                var at = centre + (Vector3)(direction * radius * 0.3f) + new Vector3(0f, 0f, 0.1f + 0.02f * i);
                EffectPiece piece = Spawn(ToonModels.Debris(kind, Random.Range(0, ToonModels.DebrisVariants)), at);
                piece.Size = radius * Random.Range(0.14f, 0.26f);
                piece.Lifetime = Random.Range(0.7f, 1.1f);
                piece.Velocity = (Vector3)direction * Random.Range(3f, 7f);
                piece.Drag = 1.5f;
                piece.SpinAxis = Random.onUnitSphere;
                piece.SpinSpeed = Random.Range(240f, 640f);
            }
        }

        static void Puff(Vector3 at, PuffColor color, float size, float lifetime, float delay, Vector3 velocity)
        {
            EffectPiece puff = Spawn(ToonModels.Puff(color), at);
            puff.Pops = true;
            puff.Size = size;
            puff.Lifetime = lifetime;
            puff.Delay = delay;
            puff.Velocity = velocity;
            puff.Drag = 4f;
        }

        static EffectPiece Spawn(Mesh mesh, Vector3 position)
        {
            var root = new GameObject("Shot effect");
            root.transform.SetPositionAndRotation(position, Random.rotation);
            root.transform.localScale = Vector3.zero; // EffectPiece sizes it from the next frame on
            ToonModels.Attach(root.transform, mesh);
            return root.AddComponent<EffectPiece>();
        }
    }
}
