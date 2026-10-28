# Gaze Lab - implementation draft

Student: Jan Biernacki  
Matriculation number: **TO BE PROVIDED**

This project adapts the Moodle Space Shooter starter for the Eye Gaze Interaction exercise. It now offers Talon gaze input on macOS, the course Tobii Unity SDK on Windows, and explicit mouse simulation. The software and report remain a preparation draft: no real hardware validation, successful gaze-only play, or demonstration recording has been completed. The 4C is only available at the lab.

## Open the project

1. Open this folder in **Unity 2019.4.13f1**. Use a copy when experimenting with another Unity version.
2. Open `Assets/Scenes/Main.unity`.
3. Select a 16:9 Game view, preferably **1920 x 1080** for the first hardware trial.
4. Press Play and keep the Game view visible. On macOS, **Talon gaze** is the default; on Windows, **Tobii SDK** is the default. Missing or invalid gaze pauses the game rather than substituting mouse input.
5. Use the three buttons in the top-left panel to choose **Talon gaze**, **Tobii SDK**, or **Mouse simulation**. Changing input mode cancels any pending dwell.

The stationary ship remains at the left of the playfield. Aim by looking at an object. A continuous **600 ms** dwell confirms one shot without a button. The laser fires only when the first eligible object along the cannon path is the selected target. Rocks score +20; satellites retain the starter's -100 penalty. The gaze dot, recent trail, progress ring, target highlight, and tracking status explain what the application is doing.

Losing valid gaze pauses target movement and spawning as well as cancelling dwell. The status panel blocks selecting or shooting through it. After game over, look at Restart for one second to begin a fresh run.

## Talon gaze on macOS

**Talon 0.4.0** is installed on this Mac at `/Applications/Talon.app`. The custom bridge is installed at `~/.talon/user/gazelab/gazelab_talon_bridge.py`; a portable copy of our bridge source is included in the project's `TalonBridge` folder. Talon itself and its vendor code are not included in the source ZIP.

1. On another Mac, install Talon from [the official website](https://talonvoice.com/) and review its license and any operating-system permission prompts.
2. Copy only `TalonBridge/gazelab_talon_bridge.py` to `~/.talon/user/gazelab/gazelab_talon_bridge.py`. Talon loads user scripts automatically. A voice command set or speech model is not required for this bridge.
3. Run Talon. This Mac is configured with microphone **None** and speech recognition disabled; the bridge uses eye tracking rather than speech.
4. Connect the 4C and complete the tracker/display calibration offered by Talon. For the first trial, keep the Game view on Talon's calibrated main display.
5. In Unity Play mode, select **Talon gaze** and confirm fresh gaze and correct marker alignment at several positions before playing.

The bridge subscribes to Talon's gaze events and sends temporary samples only to **127.0.0.1:27414**. Unity listens only on that local loopback address. No gaze history is written to disk. The visible trail is a short in-memory display.

Samples must have a valid eye observation and a combined sensor/transport age of at most **100 ms**. The receiver also expires samples as local time passes. Duplicate, out-of-order, malformed, non-finite, and outside-view samples are rejected. Game view bounds must have been published within **250 ms**, limiting reuse of an old mapping when the view is hidden; an inactive Unity window also pauses input. Explicit test packets are rejected by default; if enabled for a development check, they are labelled **TALON TEST DATA - no eye tracker**.

The bridge uses a private Talon tracking API verified against the installed 0.4.0 package. A future Talon update may require adapter changes. See [Talon's documentation](https://talonvoice.com/docs/reference/official.html) for setup information.

### What was actually verified with Talon

On **27 October 2026**, the installed Talon runtime loaded the bridge and subscribed to gaze events. Its status reported `subscribed: true`, `tracker_detected: false`, and `send_error: null`. Actual UDP heartbeats were received at the loopback endpoint with `device: none` and `valid: false`. The Mac's USB inventory contained no Tobii tracker. These observations verify startup and the local bridge connection; they do not verify eye tracking.

Real 4C detection, calibration, Retina/Game view coordinate alignment, gaze-only gameplay, and the demonstration video remain unverified until hardware is connected.

## Course Tobii SDK on Windows

Use a **Windows** laboratory computer with the **Tobii 4C**, the course-provided Tobii Core software, and a completed calibration. Follow the lab slides for the appropriate Core/Experience installer.

The course **Tobii Unity SDK 4.0.5** framework, resources, and Windows libraries are included under `Assets/Tobii`. The optional vendor demo scenes are omitted. Native libraries are restricted to the matching Windows architecture and Windows Editor. This Windows SDK remains separate from the Talon provider; it does not supply a macOS driver.

1. The SDK license acceptance is recorded in this project. The vendor acceptance check remains intact; if Unity requests agreement acceptance on another workstation, review that prompt there before using the tracker.
2. The `GAZELAB_TOBII` symbol enables the adapter for standalone projects. **Tools > Gaze Lab > Enable Tobii SDK** can restore that setting if it is removed later.
3. On the Windows laboratory PC, use **File > Build Settings > PC, Mac & Linux Standalone > Windows**, preferably x86_64, and select **Switch Platform** if required. Transfer the source ZIP without `Library` so Mac editor caches are not carried over.
4. Install the course Tobii Core/Experience driver, connect the 4C, and complete calibration. Select the **Game** view, enter Play mode, and choose **Tobii SDK**.
5. Check real tracking status and marker alignment at several positions. Recalibrate if the tracker or monitor moves.

The Tobii adapter uses `TobiiAPI.GetGazePoint().Screen`, whose coordinates start at the bottom left of the Unity screen. Invalid coordinates and samples older than **100 ms** cancel selection. Do not invert the input Y coordinate a second time when converting to world space; only the GUI overlay uses a top-left origin.

## Explicit mouse simulation

Without a tracker, enter Play mode and click **Mouse simulation**. Move the pointer to simulate gaze; no click is required to shoot. The banner reads **MOUSE SIMULATION - no eye tracker**. Select **Talon gaze** or **Tobii SDK** to return to the corresponding real input provider.

Simulation is a development mode, not evidence of real gaze-only interaction. Neither real provider falls back to mouse control automatically.

## Software verification

The integrated project compiled in Unity 2019.4.13f1 on macOS and passed **21 Unity Play Mode checks** in an isolated project copy. One check verified receipt of the actual running Talon bridge's no-tracker heartbeat and confirmed that it keeps gaze invalid. The remaining checks covered default input, empty-space and brief-dwell behavior, input-mode cancellation, target changes, invalid gaze, cannon-path blocking, scoring, repeated-hit prevention, game over, and restart using injected gaze. No real eye sample was involved.

For the Talon addition, **20 deterministic receiver protocol checks** and **5 Python mock-API bridge checks** passed. The integrated C# sources also compile with Mac and Windows Editor/player defines. Representative final Editor checks include input-mode dwell cancellation and stale Game view bounds expiry. These are protocol and source-compatibility checks, not hardware tests or a Windows native build.

The included editor checks can be run with `Unity -batchmode -nographics -projectPath <project-folder> -executeMethod GazeLabValidation.Run -logFile <log-file>`. Do not add `-quit`; the runner exits after its checks. The Python mock checks can be run separately with `python3 TalonBridge/test_bridge_contract.py`. To repeat the live no-hardware integration check, run Talon with the bridge loaded and no tracker connected, leave other GazeLab sessions out of Play Mode so UDP port 27414 is free, then use `-executeMethod GazeLabValidation.RunWithTalonNoHardware`.

## Validate before recording

- Looking at empty space emits no laser.
- A brief glance at a target does not fire; continuous dwell does.
- Switching targets, changing input mode, or losing valid/recent gaze resets the progress ring.
- A nearer object blocks a shot at an object behind it.
- A destroyed target cannot receive a delayed or repeated queued hit.
- Rocks increase the score by 20; satellites reduce it by 100.
- The game stops firing and spawning after the ship is lost; one-second gaze dwell on Restart begins a fresh run.
- Verify Talon or Tobii SDK input with the calibrated tracker, including gaze-only play without mouse input or a shooting confirmation button.
- Check marker alignment, target visibility, readable feedback, target spacing, and dwell duration at the actual display resolution and viewing distance.
- Keep the selected real provider and its tracking status visible in the recording. Do not present mouse or labelled test packets as eye-tracker validation.

Record observations and adjust the report if the final implementation or tuning changes. The 400-word report draft explains the design; it does not claim measured tracking accuracy or completed user testing.

## Prepare the three separate submission files

Moodle deadline: **9 November 2026, 10:00, Europe/Berlin**.

1. `GazeLab-<student_names_separated_by_underscores>.zip`: Unity source project. Keep `Assets`, `Packages`, `ProjectSettings`, this README, and our custom `TalonBridge` scripts. Omit generated `Library`, `Temp`, `Obj`, `Logs`, builds, IDE caches, and Talon application binaries.
2. `GazeLab-<student_names_separated_by_underscores>.pdf`: approximately 400 words addressing **C1, C2, and C3**. Submit separately from the source ZIP.
3. `GazeLab-<student_names_separated_by_underscores>.mp4` or `.mov`: compressed video of the real implementation in use, including the active gaze tracing marker. Submit separately from the source ZIP.

Add every participant's name and matriculation number to the report, source-code headers, and video. Replace every **TO BE PROVIDED** field before submission. For the video, show the identification title, real tracking status, marker and dwell feedback, a successful rock shot, a cancelled short glance, and ordinary gameplay.

No submission has been uploaded by this preparation work.
