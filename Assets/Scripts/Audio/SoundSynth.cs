// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using System;
using UnityEngine;

namespace GazeLab
{
    public static class SoundSynth
    {
        const int Rate = 44100;

        public static AudioClip Laser()
        {
            return Make("Laser", 0.3f, 1, (t, rng, s) =>
            {
                float freq = 1500f * Mathf.Pow(0.14f, t / 0.3f);
                s.Phase += freq / Rate;
                s.Phase2 += freq * 0.5f / Rate;
                float tone = 0.6f * Soft(Sin(s.Phase), 3f) + 0.4f * Sin(s.Phase2);
                return tone * Attack(t, 0.003f) * Mathf.Exp(-t * 9f) * 0.8f;
            });
        }

        public static AudioClip Explosion()
        {
            return Make("Explosion", 1f, 2, (t, rng, s) =>
            {
                float cutoff = Mathf.Lerp(3200f, 250f, Mathf.Sqrt(t));
                s.Low = LowPass(s.Low, Noise(rng), cutoff);
                s.Low2 = LowPass(s.Low2, s.Low, cutoff);
                float rumble = s.Low2 * 2.6f * Mathf.Exp(-t * 4f);
                s.Phase += Mathf.Lerp(85f, 38f, t * 3f) / Rate;
                float thump = Sin(s.Phase) * Mathf.Exp(-t * 11f);
                float crackle = t < 0.45f && rng.NextDouble() < 0.0015 ? Noise(rng) * 0.9f : 0f;
                return Soft((rumble + thump + crackle) * Attack(t, 0.002f), 1.5f) * 0.9f;
            });
        }

        public static AudioClip MetalCrash()
        {
            float[] partials = { 523f, 1371f, 2213f, 3187f };
            float[] decay = { 5f, 7f, 10f, 14f };
            return Make("Metal crash", 0.7f, 3, (t, rng, s) =>
            {
                float ring = 0f;
                for (int i = 0; i < partials.Length; i++) ring += Mathf.Sin(2f * Mathf.PI * partials[i] * t) * Mathf.Exp(-t * decay[i]);
                float n = Noise(rng);
                s.Low = LowPass(s.Low, n, 6000f);
                float hiss = (n - s.Low) * Mathf.Exp(-t * 12f);
                return Soft((ring * 0.35f + hiss * 0.6f) * Attack(t, 0.001f), 1.2f) * 0.8f;
            });
        }

        public static AudioClip Penalty()
        {
            return Make("Penalty", 0.55f, 4, (t, rng, s) =>
            {
                bool second = t >= 0.22f;
                float local = second ? t - 0.22f : t;
                float freq = (second ? 262f : 330f) * (1f - 0.06f * local / 0.3f);
                s.Phase += freq / Rate;
                s.Low = LowPass(s.Low, Square(s.Phase), 1800f);
                float env = Attack(local, 0.01f) * (second ? Mathf.Exp(-local * 4f) : Mathf.Clamp01((0.22f - local) / 0.03f));
                return s.Low * env * 0.45f;
            });
        }

        public static AudioClip ShipHit()
        {
            return Make("Ship hit", 0.6f, 5, (t, rng, s) =>
            {
                s.Phase += Mathf.Lerp(110f, 45f, t * 2f) / Rate;
                float thud = Sin(s.Phase) * Mathf.Exp(-t * 7f);
                float clang = Mathf.Sin(2f * Mathf.PI * 187f * t) * Mathf.Sin(2f * Mathf.PI * 431f * t) * Mathf.Exp(-t * 9f);
                s.Low = LowPass(s.Low, Noise(rng), 1500f);
                float crunch = s.Low * 2f * Mathf.Exp(-t * 14f);
                return Soft((thud + clang * 0.5f + crunch) * Attack(t, 0.002f), 2f) * 0.9f;
            });
        }

        public static AudioClip Lock()
        {
            return Make("Lock", 0.07f, 6, (t, rng, s) =>
            {
                s.Phase += (t < 0.03f ? 1320f : 1760f) / Rate;
                return Sin(s.Phase) * Attack(t, 0.002f) * Mathf.Exp(-t * 45f) * 0.5f;
            });
        }

        public static AudioClip Blocked()
        {
            return Make("Blocked", 0.26f, 7, (t, rng, s) =>
            {
                float local = t < 0.13f ? t : t - 0.13f;
                s.Phase += 140f / Rate;
                s.Low = LowPass(s.Low, Square(s.Phase), 1200f);
                return s.Low * Attack(local, 0.004f) * Mathf.Clamp01((0.11f - local) / 0.02f) * 0.35f;
            });
        }

        public static AudioClip Start() { return Arpeggio("Start", new[] { 523f, 659f, 784f, 1047f }, 0.075f, 0.35f); }

        public static AudioClip Menu() { return Arpeggio("Menu", new[] { 784f, 587f }, 0.08f, 0.3f); }

        public static AudioClip GameOver() { return Arpeggio("Game over", new[] { 392f, 330f, 262f, 196f }, 0.18f, 0.5f); }

        static AudioClip Arpeggio(string name, float[] notes, float step, float tail)
        {
            float length = step * (notes.Length - 1) + tail;
            return Make(name, length, 8, (t, rng, s) =>
            {
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float local = t - i * step;
                    if (local < 0f) continue;
                    float ring = i == notes.Length - 1 ? tail : step * 1.6f;
                    sum += Soft(Mathf.Sin(2f * Mathf.PI * notes[i] * local), 2f) * Attack(local, 0.004f) * Mathf.Exp(-local * 3f / ring);
                }
                return sum * 0.3f;
            });
        }

        sealed class State { public float Phase, Phase2, Low, Low2; }

        static AudioClip Make(string name, float seconds, int seed, Func<float, System.Random, State, float> sample)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var rng = new System.Random(seed);
            var state = new State();
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float end = Mathf.Clamp01((count - i) / (0.01f * Rate));
                data[i] = Mathf.Clamp(sample(t, rng, state) * end, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create("GazeLab " + name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sin(float phase) { return Mathf.Sin(2f * Mathf.PI * phase); }
        static float Square(float phase) { return phase - Mathf.Floor(phase) < 0.5f ? 1f : -1f; }
        static float Noise(System.Random rng) { return (float)rng.NextDouble() * 2f - 1f; }
        static float Attack(float t, float seconds) { return Mathf.Clamp01(t / seconds); }

        static float Soft(float x, float drive)
        {
            float e = Mathf.Exp(Mathf.Clamp(2f * x * drive, -20f, 20f));
            return (e - 1f) / (e + 1f);
        }

        static float LowPass(float previous, float input, float cutoff)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            return previous + a * (input - previous);
        }
    }
}
