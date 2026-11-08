// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public sealed class EffectPiece : MonoBehaviour
    {
        public Vector3 Velocity, SpinAxis = Vector3.forward;
        public float Size = 1f, Lifetime = 0.5f, Delay, Drag, SpinSpeed;
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
                float u = t / 0.22f - 1f;
                return 1f + 2.2f * u * u * u + 1.2f * u * u;
            }
            float v = (t - 0.22f) / 0.78f;
            return 1f - v * v;
        }

        static float FragmentScale(float t) { return t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f; }
    }
}
