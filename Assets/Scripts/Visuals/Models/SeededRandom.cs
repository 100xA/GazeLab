// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Deterministic random values for the model shapes (same seed, same model).</summary>
    sealed class SeededRandom
    {
        readonly System.Random random;

        public SeededRandom(int seed) { random = new System.Random(seed); }

        public float Range(float min, float max) { return min + (float)random.NextDouble() * (max - min); }

        public Vector3 Direction()
        {
            Vector3 d;
            do d = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
            while (d.sqrMagnitude < 0.05f || d.sqrMagnitude > 1f);
            return d.normalized;
        }

        /// <summary>Smooth random field on the unit sphere: a sum of three sine waves (-1..1).</summary>
        public SineField Field(float minFrequency, float maxFrequency) { return new SineField(this, minFrequency, maxFrequency); }

        public sealed class SineField
        {
            readonly Vector3[] waves = new Vector3[3];
            readonly float[] phases = new float[3];

            public SineField(SeededRandom random, float minFrequency, float maxFrequency)
            {
                for (int i = 0; i < waves.Length; i++)
                {
                    waves[i] = random.Direction() * random.Range(minFrequency, maxFrequency);
                    phases[i] = random.Range(0f, Mathf.PI * 2f);
                }
            }

            public float Sample(Vector3 point)
            {
                float sum = 0f;
                for (int i = 0; i < waves.Length; i++) sum += Mathf.Sin(Vector3.Dot(point, waves[i]) + phases[i]);
                return sum / waves.Length;
            }
        }
    }
}
