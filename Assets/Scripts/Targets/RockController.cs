// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>Rock: worth points when shot, damages the hull when it hits the ship.</summary>
    public class RockController : MovingTarget
    {
        protected override float Speed { get { return 3f; } }
        protected override float Spin { get { return -15f; } }
        public override bool DamagesShip { get { return true; } }
        public override int Points(GameSettings settings) { return settings.rockPoints; }
    }
}
