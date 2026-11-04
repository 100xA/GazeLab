// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>Satellite: must not be shot (penalty), harmless to the ship.</summary>
    public class SatelliteController : MovingTarget
    {
        protected override float Speed { get { return 1.5f; } }
        protected override float Spin { get { return 15f; } }
        public override bool DamagesShip { get { return false; } }
        public override int Points(GameSettings settings) { return settings.satellitePoints; }
    }
}
