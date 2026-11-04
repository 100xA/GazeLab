// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;
using UnityEngine.Serialization;

namespace GazeLab
{
    /// <summary>
    /// Owns the round (menu, playing, game over), score and ship hits, and wires the other parts
    /// together: gaze input, HUD, targeting, laser and spawner. Each frame it asks the active
    /// <see cref="ShotMode"/> whether to fire, then updates the aim with the newest gaze sample.
    /// </summary>
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
        /// <summary>Playing, but no usable gaze: targets and spawning stop.</summary>
        public bool IsPaused { get { return State == GameState.Playing && !Gaze.HasValidGaze && !Mode.ToleratesGazeLoss(Gaze, settings); } }
        public bool IsRunning { get { return State == GameState.Playing && !IsPaused; } }
        public float ModeProgress { get { return State == GameState.Playing ? Mode.Progress(Targeting, settings) : 0f; } }

        void Awake()
        {
            Gaze = GetOrAdd<GazeInput>();
            hud = GetOrAdd<GazeHud>();
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

        // After all Updates: targets have moved and the newest gaze sample is available.
        void LateUpdate() { ResolveShots(Time.unscaledTime, Time.unscaledDeltaTime); }

        /// <summary>Laser timeout and spawning.</summary>
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

        /// <summary>Confirmation by the active mode first, then aiming with the current gaze sample.</summary>
        public void ResolveShots(float now, float deltaTime)
        {
            if (State != GameState.Playing) { Targeting.Clear(); return; }
            if (IsPaused) { Targeting.ResetAim(); return; }

            // The confirmation acts on the target locked in earlier frames: a blink or nod moves the eyes.
            var context = new ShotContext(Gaze, Targeting, settings, hud.BlocksGaze);
            ShotDecision decision = Mode.Decide(context);
            if (decision.Fire) FireAtLockedTarget(decision, now);
            else if (decision.IsIgnored) SetFeedback(decision.Message, false, now);

            bool gazeUsable = Gaze.HasValidGaze && !hud.BlocksGaze(Gaze.ScreenPoint);
            Targeting.UpdateAim(gazeUsable, Gaze.ScreenPoint, now, deltaTime);
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
                hitTarget.Remove();
                SetFeedback(decision.Message + " - fired", true, now);
            }
            else SetFeedback(decision.Message + " - line of fire blocked", false, now);
            Targeting.Clear();
        }

        void SetFeedback(string text, bool fired, float now) { LastFeedback = new ShotFeedback(text, fired, now); }

        /// <summary>Called by the ship when a target hits it. Only rocks damage the hull.</summary>
        public void OnShipCollision(MovingTarget target)
        {
            if (!IsRunning || target == null) return;
            if (target.DamagesShip) RegisterShipHit();
            target.Remove();
        }

        public void RegisterShipHit()
        {
            if (State != GameState.Playing) return;
            ShipHits = Mathf.Min(settings.maxShipHits, ShipHits + 1);
            if (ShipHits >= settings.maxShipHits) EndGame();
        }

        /// <summary>Starts a fresh round with the mode chosen in the gaze menu.</summary>
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
        }

        /// <summary>Shows the gaze-operated mode menu; no targets exist and nothing can be fired.</summary>
        public void ShowMenu()
        {
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
        }

        T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
