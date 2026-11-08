// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;
using UnityEngine.Serialization;

namespace GazeLab
{
    [DisallowMultipleComponent]
    public class GameController : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField, FormerlySerializedAs("laserbeam")] GameObject laserBeam;
        [SerializeField, FormerlySerializedAs("lasercannon")] GameObject laserCannon;
        [SerializeField, FormerlySerializedAs("spacerock")] GameObject rockPrefab;
        [SerializeField, FormerlySerializedAs("satellite")] GameObject satellitePrefab;
        [SerializeField, FormerlySerializedAs("spaceship")] GameObject spaceship;

        public GameSettings settings = new GameSettings();

        TargetSpawner spawner;
        PlayerController ship;
        GazeHud hud;
        GameSounds sounds;
        float spawnTimer;

        public GameState State { get; private set; }
        public ShotMode Mode { get; private set; } = ShotModes.Dwell;
        public int Score { get; private set; }
        public int ShipHits { get; private set; }
        public ShotFeedback LastFeedback { get; private set; } = ShotFeedback.None;
        public GazeInput Gaze { get; private set; }
        public Targeting Targeting { get; private set; }
        public LaserCannon Cannon { get; private set; }

        public GameObject RockPrefab { get { return rockPrefab; } }
        public GameObject SatellitePrefab { get { return satellitePrefab; } }
        public GameObject Spaceship { get { return spaceship; } }

        public int HullPercent { get { return Mathf.Max(0, 100 * (settings.maxShipHits - ShipHits) / Mathf.Max(1, settings.maxShipHits)); } }
        public bool IsPaused { get { return State == GameState.Playing && !Gaze.HasValidGaze && !Mode.ToleratesGazeLoss(Gaze, settings); } }
        public bool IsRunning { get { return State == GameState.Playing && !IsPaused; } }
        public float ModeProgress { get { return State == GameState.Playing ? Mode.Progress(Targeting, settings) : 0f; } }

        void Awake()
        {
            Gaze = GetOrAdd<GazeInput>();
            hud = GetOrAdd<GazeHud>();
            sounds = GetOrAdd<GameSounds>();
            sounds.Init(settings);
            Cannon = new LaserCannon(laserCannon.transform, laserBeam != null ? laserBeam.GetComponent<LineRenderer>() : null, spaceship.transform);
            Targeting = new Targeting(Camera.main, Cannon, settings);
            spawner = new TargetSpawner(this, rockPrefab, satellitePrefab, settings);
            ship = spaceship.GetComponent<PlayerController>();
            if (ship == null) ship = spaceship.AddComponent<PlayerController>();
            ship.Init(this);
            Gaze.SourceChanged += CancelSelection;
            hud.Init(this, Gaze);
        }

        void Start() { ShowMenu(); }

        void OnDestroy() { if (Gaze != null) Gaze.SourceChanged -= CancelSelection; }

        void Update() { Tick(Time.unscaledTime, Time.deltaTime); }

        void LateUpdate() { ResolveShots(Time.unscaledTime, Time.unscaledDeltaTime); }

        public void Tick(float now, float deltaTime)
        {
            Cannon.UpdateBeam(now, settings.beamVisibleSeconds);
            Gaze.TrackHeadPose = State == GameState.Playing && Mode.UsesHeadPose;
            if (!IsRunning) return;
            spawnTimer += deltaTime;
            if (spawnTimer < Mathf.Max(0.1f, settings.spawnIntervalSeconds)) return;
            spawnTimer = 0f;
            spawner.SpawnRandom();
        }

        public void ResolveShots(float now, float deltaTime)
        {
            if (State != GameState.Playing) { Targeting.Clear(); return; }
            if (IsPaused) { Targeting.ResetAim(); return; }

            var context = new ShotContext(Gaze, Targeting, settings, hud.BlocksGaze);
            ShotDecision decision = Mode.Decide(context);
            if (decision.Fire) FireAtLockedTarget(decision, now);
            else if (decision.IsIgnored) SetFeedback(decision.Message, false, now);

            bool gazeUsable = Gaze.HasValidGaze && !hud.BlocksGaze(Gaze.ScreenPoint);
            Collider2D aimedBefore = Targeting.AimTarget;
            Targeting.UpdateAim(gazeUsable, Gaze.ScreenPoint, now, deltaTime);
            if (Targeting.AimTarget != null && Targeting.AimTarget != aimedBefore) sounds.TargetAimed(Targeting.AimTarget, now);
            else sounds.TargetStillAimed(Targeting.AimTarget, now);
        }

        void FireAtLockedTarget(ShotDecision decision, float now)
        {
            Collider2D target = Targeting.LockedTarget(now, decision.LockGraceSeconds);
            if (target == null)
            {
                SetFeedback(decision.Message + " - no target locked", false, now);
                return;
            }
            Physics2D.SyncTransforms();
            Vector2 hitPoint;
            if (Cannon.HasClearShot(target, Targeting.LockedAimPoint, out hitPoint))
            {
                Cannon.Fire(hitPoint, now);
                MovingTarget hitTarget = target.GetComponent<MovingTarget>();
                Score += hitTarget.Points(settings);
                sounds.Shot(hitTarget.Kind);
                hitTarget.Explode();
                SetFeedback(decision.Message + " - fired", true, now);
            }
            else
            {
                sounds.ShotBlocked();
                SetFeedback(decision.Message + " - line of fire blocked", false, now);
            }
            Targeting.Clear();
        }

        void SetFeedback(string text, bool fired, float now) { LastFeedback = new ShotFeedback(text, fired, now); }

        public void OnShipCollision(MovingTarget target)
        {
            if (!IsRunning || target == null) return;
            if (!target.DamagesShip) { target.Remove(); return; }
            sounds.ShipHit();
            target.Explode();
            RegisterShipHit();
        }

        public void RegisterShipHit()
        {
            if (State != GameState.Playing) return;
            ShipHits = Mathf.Min(settings.maxShipHits, ShipHits + 1);
            if (ShipHits >= settings.maxShipHits) EndGame();
        }

        public void StartMode(ShotMode mode)
        {
            if (mode != null) Mode = mode;
            RestartRound();
        }

        public void RestartRound()
        {
            spawner.ClearAll();
            Score = 0;
            ShipHits = 0;
            spawnTimer = 0f;
            LastFeedback = ShotFeedback.None;
            State = GameState.Playing;
            Targeting.Clear();
            Cannon.Hide();
            ship.ResetShip();
            spawner.SpawnRock(settings.firstRockY);
            spawner.SpawnSatellite(settings.firstSatelliteY);
            sounds.RoundStart();
        }

        public void ShowMenu()
        {
            if (State == GameState.GameOver) sounds.BackToMenu();
            spawner.ClearAll();
            State = GameState.Menu;
            ShipHits = 0;
            Targeting.Clear();
            Cannon.Hide();
            ship.ResetShip();
        }

        public void CancelSelection() { if (Targeting != null) Targeting.Clear(); }

        void EndGame()
        {
            State = GameState.GameOver;
            Targeting.Clear();
            Cannon.Hide();
            ship.Crash();
            sounds.GameOver();
        }

        T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
