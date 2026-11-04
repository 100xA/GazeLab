// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Satellite: must not be shot (penalty), harmless to the ship.</summary>
    public class SatelliteController : MovingTarget
    {
        float phase;

        protected override float Speed { get { return 1.5f; } }
        protected override float Spin { get { return 15f; } }
        public override TargetKind Kind { get { return TargetKind.Satellite; } }
        public override bool DamagesShip { get { return false; } }
        public override int Points(GameSettings settings) { return settings.satellitePoints; }

        protected override Mesh CreateModel()
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            return ToonModels.Satellite(Random.Range(0, ToonModels.SatelliteVariants));
        }

        // Rolls slowly around its long axis, so the solar panels catch the light.
        protected override void Animate(Transform model, float age)
        {
            model.localRotation = Quaternion.Euler(Mathf.Sin(phase + age * 0.9f) * 35f, Mathf.Sin(phase * 1.7f + age * 0.6f) * 12f, 0f);
        }
    }
}
