// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GazeLabValidation
{
    const string ActiveKey = "GazeLabValidation.Active";
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static double deadline;
    static double playObservedAt = -1;
    static int checks;

    static GazeLabValidation()
    {
        if (SessionState.GetBool(ActiveKey, false)) Attach();
    }

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
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Timed out waiting for Play Mode initialization.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (playObservedAt < 0) playObservedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - playObservedAt < 1) return;
            EditorApplication.update -= Tick;
            ExecuteChecks();
            Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }

    static void ExecuteChecks()
    {
        checks = 0;
        GameController game = Object.FindObjectOfType<GameController>();
        Check(game != null, "scene contains GameController");
        GazeInput input = game.GetComponent<GazeInput>();
        Check(input != null, "scene has gaze input");
        Check(game.InMenu && CountTargets() == 0 && game.DwellTarget == null,
            "game starts in the gaze mode menu without targets");
        Vector3 initialShipPosition = game.spaceship.transform.position;
        Quaternion initialShipRotation = game.spaceship.transform.rotation;

        // Prevent uncontrolled frame input/movement while driving real controller methods.
        game.enabled = false;
        input.enabled = false;
        GazeHud hud = game.GetComponent<GazeHud>();
        if (hud != null) hud.enabled = false;
        if (input.simulateMouse) input.SetSimulation(false);
        game.StartMode(GameController.SelectionMode.Dwell);
        Check(!game.InMenu && game.Mode == GameController.SelectionMode.Dwell, "menu selection starts dwell mode");
        RemoveTargets();
        Camera camera = Camera.main;
        Check(camera != null && camera.pixelWidth > 0 && camera.pixelHeight > 0,
            "test scene has a usable camera viewport");

        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && game.DwellTarget == null && !BeamIsVisible(game),
            "empty space never emits a laser");

        Collider2D rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == rock && game.DwellProgress == 0 && game.points == 0,
            "initial target acquisition does not fire");
        SetField(game, "dwellElapsed", 0.1f);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && game.DwellProgress < 1f,
            "brief dwell does not fire");

        input.SetSimulation(true);
        Check(game.DwellTarget == null && game.DwellProgress == 0,
            "switching input source cancels pending selection");
        input.SetSimulation(false);
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");

        Collider2D otherRock = SpawnFixture(game.spacerock, new Vector2(2f, -4f));
        SetField(game, "dwellElapsed", game.DwellSeconds * 0.8f);
        InjectGaze(input, camera, new Vector2(2f, -4f), true);
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == otherRock && game.DwellProgress == 0 && game.points == 0,
            "changing targets discards previous dwell progress");
        SetField(game, "dwellElapsed", game.DwellSeconds * 0.8f);
        SetProperty(input, "HasValidGaze", false);
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == null && game.DwellProgress == 0 && game.IsPaused,
            "invalid gaze cancels dwell and pauses gameplay");

        Object.DestroyImmediate(otherRock.gameObject);
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        GameObject blocker = new GameObject("Validation cannon-path blocker");
        blocker.transform.position = Vector3.Lerp(game.lasercannon.transform.position, rock.transform.position, 0.5f);
        blocker.AddComponent<CircleCollider2D>().radius = 0.4f;
        Physics2D.SyncTransforms();
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && !BeamIsVisible(game) && game.DwellTarget == null,
            "first obstacle blocks shooting through it");
        Object.DestroyImmediate(blocker);

        Physics2D.SyncTransforms();
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        Invoke(game, "LateUpdate");
        Check(game.points == 20 && !rock.enabled && BeamIsVisible(game),
            "confirmed unobstructed rock scores 20 and disables its collider");
        int scoreAfterShot = game.points;
        Invoke(game, "LateUpdate");
        Check(game.points == scoreAfterShot && game.DwellTarget == null,
            "destroyed target cannot receive a repeated queued hit");

        RemoveTargets();
        Collider2D satellite = SpawnFixture(game.satellite, new Vector2(2f, 0f));
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        Invoke(game, "LateUpdate");
        Check(game.points == -80 && !satellite.enabled,
            "satellite remains selectable and applies the original 100-point penalty");

        RemoveTargets();
        rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        Invoke(game, "LateUpdate");
        game.damage = 4;
        Invoke(game, "Update");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        SetField(game, "spawnElapsed", 1000f);
        int targetsAtGameOver = CountTargets();
        Invoke(game, "LateUpdate");
        Invoke(game, "Update");
        Check(game.GameOver && game.points == -80 && rock.enabled && !BeamIsVisible(game)
              && CountTargets() == targetsAtGameOver,
            "game over prevents shooting and spawning");

        game.spaceship.transform.position += Vector3.down * 3;
        game.RestartGame();
        Rigidbody2D shipBody = game.spaceship.GetComponent<Rigidbody2D>();
        bool shipCollidersEnabled = true;
        foreach (Collider2D collider in game.spaceship.GetComponentsInChildren<Collider2D>())
            shipCollidersEnabled &= collider.enabled;
        Check(!game.GameOver && game.points == 0 && game.damage == 0 && game.DwellTarget == null
              && CountTargets() == 2 && shipCollidersEnabled
              && Vector3.Distance(game.spaceship.transform.position, initialShipPosition) < 0.001f
              && Quaternion.Angle(game.spaceship.transform.rotation, initialShipRotation) < 0.001f
              && shipBody.velocity == Vector2.zero && shipBody.gravityScale == 0f,
            "restart restores score, damage, ship and two fresh targets");

        CheckSelectionModes(game, input, camera);
        CheckNodDetector();
        CheckDisplayScalingCompensation();
    }

    static void CheckDisplayScalingCompensation()
    {
        // Tobii at 150 %: true window position w is reported as 0.5 + (w - 0.5) / 1.5.
        const float scale = 1.5f;
        var bounds = new Rect(0f, 0.02f, 1f, 0.98f); // Game view toolbar takes the top 2 %
        float worst = 0f;
        foreach (Vector2 truth in new[] { new Vector2(0.01f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.1f) })
        {
            float bottom = 1f - bounds.height - bounds.y;
            Vector2 window = new Vector2(bounds.x + truth.x * bounds.width, bottom + truth.y * bounds.height);
            Vector2 reportedWindow = new Vector2(0.5f + (window.x - 0.5f) / scale, 0.5f + (window.y - 0.5f) / scale);
            Vector2 reported = new Vector2((reportedWindow.x - bounds.x) / bounds.width, (reportedWindow.y - bottom) / bounds.height);
            worst = Mathf.Max(worst, (GazeInput.CompensateDisplayScaling(reported, bounds, scale) - truth).magnitude);
        }
        Check(worst < 1e-4f, "display scaling compensation restores the true gaze position");
        Check(GazeInput.CompensateDisplayScaling(new Vector2(0.3f, 0.6f), bounds, 1f) == new Vector2(0.3f, 0.6f),
            "display scaling compensation is neutral at 100 %");
    }

    static void CheckSelectionModes(GameController game, GazeInput input, Camera camera)
    {
        // Blink mode: aiming alone never fires; short (natural) blinks are ignored.
        game.StartMode(GameController.SelectionMode.Blink);
        RemoveTargets();
        Collider2D rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        SetField(game, "dwellElapsed", game.DwellSeconds * 2f);
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == rock && game.points == 0 && rock.enabled,
            "blink mode: long aiming alone does not fire");
        SetProperty(input, "BlinkEnded", true);
        SetProperty(input, "LastBlinkSeconds", 0.12f);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && game.LastConfirmText.Contains("too short"),
            "blink mode: natural short blink is ignored");
        SetProperty(input, "LastBlinkSeconds", 2.5f);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && game.LastConfirmText.Contains("too long"),
            "blink mode: very long eye closure is ignored");
        SetProperty(input, "LastBlinkSeconds", 0.5f);
        Invoke(game, "LateUpdate");
        SetProperty(input, "BlinkEnded", false);
        Check(game.points == 20 && !rock.enabled && BeamIsVisible(game),
            "blink mode: deliberate blink fires at the locked target");

        SetProperty(input, "HasValidGaze", false);
        SetProperty(input, "EyesClosedCandidate", true);
        SetProperty(input, "EyesClosedSeconds", 0.5f);
        Check(!game.IsPaused, "blink mode: closing the eyes briefly does not pause the game");
        SetProperty(input, "EyesClosedSeconds", game.BlinkMaxSeconds + 1f);
        Check(game.IsPaused, "blink mode: eyes closed too long pauses the game");
        SetProperty(input, "EyesClosedCandidate", false);
        SetProperty(input, "EyesClosedSeconds", 0f);

        // Nod mode.
        game.StartMode(GameController.SelectionMode.Nod);
        RemoveTargets();
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        SetProperty(input, "NodDetected", true);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && game.LastConfirmText.Contains("no target"),
            "nod mode: nod without a locked target does not fire");
        SetProperty(input, "NodDetected", false);
        rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == rock && game.points == 0, "nod mode: aiming locks without firing");
        SetProperty(input, "NodDetected", true);
        Invoke(game, "LateUpdate");
        SetProperty(input, "NodDetected", false);
        Check(game.points == 20 && !rock.enabled, "nod mode: nod fires at the locked target");

        // Menu.
        game.ShowMenu();
        Check(game.InMenu && CountTargets() == 0 && !game.IsPaused, "menu removes targets and stops the round");
        rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        // The menu buttons cover this spot, so gaze is injected without the HUD-free check.
        SetProperty(input, "ScreenPoint", (Vector2)camera.WorldToScreenPoint(new Vector3(2f, 0f, 0f)));
        SetProperty(input, "HasValidGaze", true);
        SetField(game, "dwellElapsed", game.DwellSeconds * 2f);
        int scoreInMenu = game.points;
        Invoke(game, "LateUpdate");
        Check(game.points == scoreInMenu && rock.enabled && game.DwellTarget == null, "menu never fires at targets");
        RemoveTargets();
    }

    static void CheckNodDetector()
    {
        var detector = new HeadNodDetector();
        const float dt = 1f / 60f;
        float t = 0f;
        int nods = 0;
        // Rest, quick nod down and back (0.4 s), rest.
        for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
        for (int i = 0; i < 24; i++, t += dt)
            if (detector.Update(15f * Mathf.Sin(Mathf.PI * i / 24f), 0f, t, dt)) nods++;
        for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
        Check(nods == 1, "head nod detector: one quick nod is detected exactly once");

        detector.Reset();
        nods = 0;
        // Slow posture change: tilt 20 degrees over 3 s and stay there.
        for (int i = 0; i < 180; i++, t += dt) if (detector.Update(20f * i / 180f, 0f, t, dt)) nods++;
        for (int i = 0; i < 60; i++, t += dt) if (detector.Update(20f, 0f, t, dt)) nods++;
        Check(nods == 0, "head nod detector: slow posture change is not a nod");

        detector.Reset();
        nods = 0;
        // Head shake: large yaw with small pitch.
        for (int i = 0; i < 60; i++, t += dt) if (detector.Update(0f, 0f, t, dt)) nods++;
        for (int i = 0; i < 48; i++, t += dt)
            if (detector.Update(4f * Mathf.Sin(Mathf.PI * i / 24f), 25f * Mathf.Sin(Mathf.PI * i / 24f), t, dt)) nods++;
        Check(nods == 0, "head nod detector: head shake is not a nod");
    }

    static void InjectGaze(GazeInput input, Camera camera, Vector2 worldPoint, bool valid)
    {
        Vector2 point = camera.WorldToScreenPoint(new Vector3(worldPoint.x, worldPoint.y, 0));
        Check(!GazeHud.ControlsContain(point), "fixture gaze lies outside HUD controls");
        SetProperty(input, "ScreenPoint", point);
        SetProperty(input, "HasValidGaze", valid);
    }

    static Collider2D SpawnFixture(GameObject prefab, Vector2 position)
    {
        GameObject fixture = Object.Instantiate(prefab);
        fixture.name = "Validation " + prefab.name;
        fixture.transform.position = position;
        fixture.transform.rotation = Quaternion.identity;
        RockController rock = fixture.GetComponent<RockController>();
        SatelliteController satellite = fixture.GetComponent<SatelliteController>();
        if (rock != null) rock.enabled = false;
        if (satellite != null) satellite.enabled = false;
        Rigidbody2D body = fixture.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            body.gravityScale = 0;
        }
        Physics2D.SyncTransforms();
        return fixture.GetComponent<Collider2D>();
    }

    static void RemoveTargets()
    {
        foreach (RockController rock in Object.FindObjectsOfType<RockController>()) Object.DestroyImmediate(rock.gameObject);
        foreach (SatelliteController satellite in Object.FindObjectsOfType<SatelliteController>()) Object.DestroyImmediate(satellite.gameObject);
        Physics2D.SyncTransforms();
    }

    static int CountTargets()
    {
        return Object.FindObjectsOfType<RockController>().Length + Object.FindObjectsOfType<SatelliteController>().Length;
    }
    static bool BeamIsVisible(GameController game) { return game.laserbeam.GetComponent<LineRenderer>().enabled; }
    static void Invoke(object target, string method) { target.GetType().GetMethod(method, PrivateInstance).Invoke(target, null); }
    static void SetField(object target, string field, object value) { target.GetType().GetField(field, PrivateInstance).SetValue(target, value); }
    static void SetProperty(object target, string property, object value)
    {
        target.GetType().GetProperty(property).GetSetMethod(true).Invoke(target, new[] { value });
    }
    static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
        Debug.Log("GAZELAB_CHECK_PASS: " + description);
    }
    static void Finish(Exception exception)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        if (exception == null) Debug.Log("GAZELAB_TEST_PASS: " + checks + " checks; injected gaze only, no hardware validation.");
        else Debug.LogError("GAZELAB_TEST_FAIL: " + exception);
        EditorApplication.Exit(exception == null ? 0 : 1);
    }
}
