// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>One moving, spinning, scaling part of an effect; removes itself when done.</summary>
    public sealed class EffectPiece : MonoBehaviour
    {
        public Vector3 Velocity, SpinAxis = Vector3.forward;
        public float Size = 1f, Lifetime = 0.5f, Delay, Drag, SpinSpeed;
        /// <summary>Puffs pop up fast and shrink away; fragments keep their size and vanish at the end.</summary>
        public bool Pops;
        float age;

        void Update()
        {
            float dt = Time.deltaTime;
            if (Delay > 0f) { Delay -= dt; return; }
            age += dt;
            float t = age / Mathf.Max(0.01f, Lifetime);
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.position += Velocity * dt;
            Velocity *= Mathf.Exp(-Drag * dt);
            if (SpinSpeed != 0f) transform.Rotate(SpinAxis, SpinSpeed * dt, Space.World);
            transform.localScale = Vector3.one * (Size * (Pops ? PopScale(t) : FragmentScale(t)));
        }

        static float PopScale(float t)
        {
            if (t < 0.22f)
            {
                // Overshooting ease-out: grows quickly and bulges a little.
                float u = t / 0.22f - 1f;
                return 1f + 2.2f * u * u * u + 1.2f * u * u;
            }
            float v = (t - 0.22f) / 0.78f;
            return 1f - v * v;
        }

        static float FragmentScale(float t) { return t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f; }
    }
}
