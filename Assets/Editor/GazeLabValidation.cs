using System;
using GazeLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GazeLabValidation
{
    const string ActiveKey = "GazeLabValidation.Active";
    static double deadline, playObservedAt = -1;
    static int checks;

    static GazeLabValidation() { if (SessionState.GetBool(ActiveKey, false)) Attach(); }

    public static void Run()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(exception); }
    }

    static void Attach()
    {
        deadline = EditorApplication.timeSinceStartup + 120;
        playObservedAt = -1;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Play mode did not start.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (playObservedAt < 0) playObservedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - playObservedAt < 1) return;
            EditorApplication.update -= Tick;
            new Session().RunAll();
            Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }

    static void Finish(Exception exception)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        if (exception == null) Debug.Log("GAZELAB_TEST_PASS: " + checks + " checks; fake gaze only, no hardware validation.");
        else Debug.LogError("GAZELAB_TEST_FAIL: " + exception);
        EditorApplication.Exit(exception == null ? 0 : 1);
    }

    static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
        Debug.Log("GAZELAB_CHECK_PASS: " + description);
    }

    sealed class FakeGazeSource : IGazeSource
    {
        public GazeSample Next = GazeSample.Invalid("fake: no gaze");
        public bool NodPressed;
        public bool IsSimulation { get { return true; } }
        public void Activate() { }
        public void Deactivate() { }
        public GazeSample ReadGaze(float now) { return Next; }
        public HeadReading ReadHead(float now) { return HeadReading.Simulated(NodPressed, "fake"); }
    }

    sealed class Session
    {
        const float Frame = 0.1f;
        readonly GameController game;
        readonly GazeInput gaze;
        readonly GazeHud hud;
        readonly Camera cam;
        readonly FakeGazeSource fake = new FakeGazeSource();
        float now = 1000f;

        public Session()
        {
            game = Object.FindObjectOfType<GameController>();
            Check(game != null, "scene contains the GameController");
            gaze = game.GetComponent<GazeInput>();
            hud = game.GetComponent<GazeHud>();
            cam = Camera.main;
        }

        public void RunAll()
        {
            Check(game.State == GameState.Menu && CountTargets() == 0, "game starts in the gaze mode menu without targets");
            game.enabled = false;
            gaze.enabled = false;
            hud.enabled = false;
            gaze.SetSource(fake);
            game.settings.spawnIntervalSeconds = 1e6f;
            Check(cam != null && cam.pixelWidth > 0, "scene has a usable camera");

            CheckDwell();
            CheckScoringAndGameOver();
            CheckBlink();
            CheckNod();
            CheckMenu();
            CheckNodDetector();
            CheckDisplayScaling();
            CheckGazeButtons();
        }

        static void CheckGazeButtons()
        {
            var buttons = new GazeButtons();
            int pressed = 0;
            buttons.Set(new[] { new GazeButtons.Button { Rect = new Rect(0, 0, 100, 100), Action = () => pressed++ } });
            var inside = new Vector2(50, 50);
            float t = 0f;
            for (int i = 0; i < 5; i++, t += 0.1f) buttons.Update(true, inside, t, 0.1f);
            Check(buttons.Progress == 0f, "gaze buttons ignore the gaze right after appearing");
            for (int i = 0; i < 20 && pressed == 0; i++, t += 0.1f) buttons.Update(true, inside, t, 0.1f);
            Check(pressed == 1, "gaze buttons fire after the dwell time");
            buttons.Update(true, new Vector2(500, 500), t, 0.1f);
            for (int i = 0; i < 5; i++, t += 0.1f) buttons.Update(true, new Vector2(500, 500), t, 0.1f);
            Check(buttons.Hovered == -1 && pressed == 1, "gaze far away from the buttons selects nothing");
        }

        void CheckDwell()
        {
            Start(ShotModes.Dwell);
            Look(new Vector2(2f, 0f));
            Frames(3);
            Check(game.Score == 0 && game.Targeting.AimTarget == null && !game.Cannon.IsBeamVisible, "dwell: empty space never fires");

            Collider2D rock = Spawn(game.RockPrefab, new Vector2(2f, 0f));
            Frames(1);
            Check(game.Targeting.AimTarget == rock && game.ModeProgress == 0f, "dwell: looking at a rock aims without firing");
            Frames(3);
            Check(game.Score == 0 && rock.enabled && game.ModeProgress < 1f, "dwell: a short look does not fire");

            gaze.SetSource(new FakeGazeSource());
            Check(game.Targeting.AimTarget == null, "switching the gaze source cancels the aim");
            gaze.SetSource(fake);

            Collider2D other = Spawn(game.RockPrefab, new Vector2(2f, -4f));
            Frames(4);
            Look(new Vector2(2f, -4f));
            Frames(1);
            Check(game.Targeting.AimTarget == other && game.Targeting.AimSeconds == 0f, "dwell: changing the target restarts the dwell");

            LookAway();
            Frames(1);
            Check(game.IsPaused && game.Targeting.AimTarget == null, "lost gaze pauses the round and cancels the aim");
            Object.DestroyImmediate(other.gameObject);

            Look(NearMiss(rock, 0.3f));
            Frames(1);
            Check(game.Targeting.AimTarget == rock, "a near miss within the tolerance still selects the target");
            Look(NearMiss(rock, 1.2f));
            Frames(2);
            Check(game.Targeting.AimTarget == null, "gaze far from every target selects nothing");

            Look(new Vector2(2f, 0f));
            Frames(2);
            GameObject blocker = new GameObject("Check blocker");
            blocker.transform.position = Vector2.Lerp(game.Cannon.MuzzlePosition, rock.transform.position, 0.5f);
            blocker.AddComponent<CircleCollider2D>().radius = 0.4f;
            Physics2D.SyncTransforms();
            Frames(10);
            Check(game.Score == 0 && rock.enabled && game.Targeting.AimTarget == null, "an object in the line of fire blocks the shot");
            Object.DestroyImmediate(blocker);

            int frames = FramesUntil(() => game.Score != 0, 12);
            Check(frames >= 6 && frames <= 8 && game.Score == game.settings.rockPoints && !rock.enabled && game.Cannon.IsBeamVisible,
                "dwell: 0.6 s on a rock fires and scores");
            Frames(5);
            Check(game.Score == game.settings.rockPoints, "a destroyed target cannot be hit twice");
        }

        void CheckScoringAndGameOver()
        {
            Start(ShotModes.Dwell);
            Spawn(game.SatellitePrefab, new Vector2(2f, 0f));
            Look(new Vector2(2f, 0f));
            FramesUntil(() => game.Score != 0, 12);
            Check(game.Score == game.settings.satellitePoints, "shooting a satellite costs points");

            Collider2D rock = Spawn(game.RockPrefab, new Vector2(2f, 0f));
            for (int i = 0; i < game.settings.maxShipHits; i++) game.RegisterShipHit();
            game.settings.spawnIntervalSeconds = 1f;
            int targets = CountTargets();
            Frames(10);
            game.Tick(now, 100f);
            Check(game.State == GameState.GameOver && rock.enabled && CountTargets() == targets, "game over: no shooting and no spawning");
            game.settings.spawnIntervalSeconds = 1e6f;

            Vector3 shipStart = game.Spaceship.transform.position;
            game.RestartRound();
            bool collidersOn = true;
            foreach (Collider2D part in game.Spaceship.GetComponentsInChildren<Collider2D>()) collidersOn &= part.enabled;
            Check(game.State == GameState.Playing && game.Score == 0 && game.ShipHits == 0 && CountTargets() == 2 && collidersOn
                  && Vector3.Distance(game.Spaceship.transform.position, shipStart) < 0.001f,
                "restart resets score, hull, ship and spawns two targets");
        }

        void CheckBlink()
        {
            Start(ShotModes.Blink);
            Collider2D rock = Spawn(game.RockPrefab, new Vector2(2f, 0f));
            Look(new Vector2(2f, 0f));
            Frames(15);
            Check(game.Targeting.AimTarget == rock && game.Score == 0, "blink: long aiming alone never fires");

            CloseEyes(1);
            Look(new Vector2(2f, 0f));
            Frames(1);
            Check(game.Score == 0 && game.LastFeedback.Text.Contains("too short"), "blink: a natural short blink is ignored");

            CloseEyes(5);
            Check(!game.IsPaused, "blink: closed eyes do not pause the game during a blink");
            CloseEyes(15);
            Check(game.IsPaused, "blink: eyes closed too long pause the game");
            Look(new Vector2(2f, 0f));
            Frames(1);
            Check(game.Score == 0 && game.LastFeedback.Text.Contains("too long"), "blink: a very long closure is ignored");

            Frames(2);
            CloseEyes(5);
            Look(new Vector2(-4f, -3f));
            Frames(1);
            Check(game.Score == 0 && rock.enabled && game.LastFeedback.Text.Contains("moved"),
                "blink: eyes reopening elsewhere (looking away) do not fire");

            Look(new Vector2(2f, 0f));
            Frames(2);
            CloseEyes(5);
            Look(new Vector2(2f, 0f));
            Frames(1);
            Check(game.Score == game.settings.rockPoints && !rock.enabled && game.LastFeedback.Fired,
                "blink: a deliberate blink fires at the locked target");
        }

        void CheckNod()
        {
            Start(ShotModes.Nod);
            Look(new Vector2(2f, 0f));
            Frames(1);
            Nod();
            Check(game.Score == 0 && game.LastFeedback.Text.Contains("no target"), "nod: without a locked target nothing fires");

            Collider2D rock = Spawn(game.RockPrefab, new Vector2(2f, 0f));
            Frames(15);
            Check(game.Targeting.AimTarget == rock && game.Score == 0, "nod: aiming alone never fires");
            Nod();
            Check(game.Score == game.settings.rockPoints && !rock.enabled, "nod: a nod fires at the locked target");
        }

        void CheckMenu()
        {
            game.ShowMenu();
            Check(game.State == GameState.Menu && CountTargets() == 0 && !game.IsPaused, "the menu removes all targets");
            Collider2D rock = Spawn(game.RockPrefab, new Vector2(2f, 0f));
            Look(new Vector2(2f, 0f), checkHud: false);
            int score = game.Score;
            Frames(10);
            Check(game.Score == score && rock.enabled && game.Targeting.AimTarget == null, "the menu never fires at targets");
            RemoveTargets();
        }

        static void CheckNodDetector()
        {
            var detector = new HeadNodDetector();
            const float dt = 1f / 60f;
            float t = 0f;
            int nods = 0;
            for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
            for (int i = 0; i < 24; i++, t += dt) if (detector.Update(15f * Mathf.Sin(Mathf.PI * i / 24f), 0f, t, dt)) nods++;
            for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
            Check(nods == 1, "nod detector: one quick nod is detected exactly once");

            detector.Reset();
            nods = 0;
            for (int i = 0; i < 180; i++, t += dt) if (detector.Update(20f * i / 180f, 0f, t, dt)) nods++;
            for (int i = 0; i < 60; i++, t += dt) if (detector.Update(20f, 0f, t, dt)) nods++;
            Check(nods == 0, "nod detector: a slow posture change is not a nod");

            detector.Reset();
            nods = 0;
            for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
            for (int i = 0; i < 48; i++, t += dt)
                if (detector.Update(4f * Mathf.Sin(Mathf.PI * i / 24f), 25f * Mathf.Sin(Mathf.PI * i / 24f), t, dt)) nods++;
            Check(nods == 0, "nod detector: a head shake is not a nod");
        }

        static void CheckDisplayScaling()
        {
            const float scale = 1.5f;
            var bounds = new Rect(0f, 0.02f, 1f, 0.98f);
            float bottom = 1f - bounds.height - bounds.y;
            float worst = 0f;
            foreach (Vector2 truth in new[] { new Vector2(0.01f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.1f) })
            {
                Vector2 window = new Vector2(bounds.x + truth.x * bounds.width, bottom + truth.y * bounds.height);
                Vector2 shrunk = new Vector2(0.5f + (window.x - 0.5f) / scale, 0.5f + (window.y - 0.5f) / scale);
                Vector2 reported = new Vector2((shrunk.x - bounds.x) / bounds.width, (shrunk.y - bottom) / bounds.height);
                worst = Mathf.Max(worst, (DisplayScaling.Compensate(reported, bounds, scale) - truth).magnitude);
            }
            Check(worst < 1e-4f, "display scaling: compensation restores the true gaze position");
            Check(DisplayScaling.Compensate(new Vector2(0.3f, 0.6f), bounds, 1f) == new Vector2(0.3f, 0.6f),
                "display scaling: neutral at 100 %");
        }

        void Start(ShotMode mode)
        {
            game.StartMode(mode);
            RemoveTargets();
            LookAway();
            Frames(1);
        }

        void Frames(int count)
        {
            for (int i = 0; i < count; i++) Step();
        }

        int FramesUntil(Func<bool> condition, int max)
        {
            for (int i = 1; i <= max; i++)
            {
                Step();
                if (condition()) return i;
            }
            return max + 1;
        }

        void Step()
        {
            now += Frame;
            gaze.Tick(now, Frame, true);
            game.Tick(now, Frame);
            game.ResolveShots(now, Frame);
        }

        void Nod()
        {
            fake.NodPressed = true;
            Step();
            fake.NodPressed = false;
        }

        void Look(Vector2 world, bool checkHud = true)
        {
            Vector2 screen = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            if (checkHud) Check(!hud.BlocksGaze(screen), "check gaze lies outside the HUD");
            fake.Next = GazeSample.Valid(screen, "fake gaze");
        }

        void LookAway() { fake.Next = GazeSample.Invalid("fake: looking away"); }

        void CloseEyes(int frames)
        {
            fake.Next = GazeSample.Invalid("fake: eyes closed", eyesClosed: true);
            Frames(frames);
        }

        static Vector2 NearMiss(Collider2D target, float distance)
        {
            float top = float.NegativeInfinity;
            foreach (Collider2D part in target.GetComponents<Collider2D>()) top = Mathf.Max(top, part.bounds.max.y);
            return new Vector2(target.bounds.center.x, top + distance);
        }

        static Collider2D Spawn(GameObject prefab, Vector2 position)
        {
            GameObject fixture = Object.Instantiate(prefab);
            fixture.name = "Check " + prefab.name;
            fixture.transform.position = position;
            fixture.transform.rotation = Quaternion.identity;
            fixture.GetComponent<MovingTarget>().enabled = false;
            Rigidbody2D body = fixture.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.velocity = Vector2.zero;
                body.gravityScale = 0;
            }
            Physics2D.SyncTransforms();
            return fixture.GetComponent<Collider2D>();
        }

        static void RemoveTargets()
        {
            foreach (MovingTarget target in Object.FindObjectsOfType<MovingTarget>()) Object.DestroyImmediate(target.gameObject);
            Physics2D.SyncTransforms();
        }

        static int CountTargets() { return Object.FindObjectsOfType<MovingTarget>().Length; }
    }
}
