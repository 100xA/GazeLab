// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Rock: worth points when shot, damages the hull when it hits the ship.</summary>
    public class RockController : MovingTarget
    {
        Quaternion startRotation;
        Vector3 tumbleAxis;
        float tumbleSpeed;

        protected override float Speed { get { return 3f; } }
        protected override float Spin { get { return -15f; } }
        public override TargetKind Kind { get { return TargetKind.Rock; } }
        public override bool DamagesShip { get { return true; } }
        public override int Points(GameSettings settings) { return settings.rockPoints; }

        protected override Mesh CreateModel()
        {
            startRotation = Random.rotation;
            tumbleAxis = Random.onUnitSphere;
            tumbleSpeed = Random.Range(25f, 45f);
            return ToonModels.Rock(Random.Range(0, ToonModels.RockVariants));
        }

        // Tumbles around a random axis, so the craters roll into view.
        protected override void Animate(Transform model, float age)
        {
            model.localRotation = Quaternion.AngleAxis(age * tumbleSpeed, tumbleAxis) * startRotation;
        }
    }
}
