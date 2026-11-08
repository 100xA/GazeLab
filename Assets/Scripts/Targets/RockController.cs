// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
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

        protected override void Animate(Transform model, float age)
        {
            model.localRotation = Quaternion.AngleAxis(age * tumbleSpeed, tumbleAxis) * startRotation;
        }
    }
}
