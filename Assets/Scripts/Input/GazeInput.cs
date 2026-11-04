// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Reads the active gaze source once per frame and derives the confirmation signals used by the
    /// shooting modes: blink duration and head nods. The Tobii tracker is only loaded after it was
    /// selected once (see <see cref="TobiiCrashGuard"/>); otherwise the game starts with the mouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class GazeInput : MonoBehaviour
    {
        public GazeInputSettings settings = new GazeInputSettings();

        readonly BlinkDetector blink = new BlinkDetector();
        readonly HeadNodDetector nod = new HeadNodDetector();
        GazeFilter filter;
        MouseGazeSource mouse;
        TobiiGazeSource tobii;

        /// <summary>Raised whenever the source changes; pending selections must be cancelled.</summary>
        public event Action SourceChanged;

        public IGazeSource Source { get; private set; }
        public bool IsSimulation { get { return Source != null && Source.IsSimulation; } }
        public GazeSample Gaze { get; private set; }
        public bool HasValidGaze { get { return Gaze.IsValid; } }
        public Vector2 ScreenPoint { get { return Gaze.ScreenPoint; } }
        public string StatusText { get { return Gaze.Status; } }
        public BlinkDetector Blink { get { return blink; } }

        /// <summary>Set by the game while a mode needs head-pose data.</summary>
        public bool TrackHeadPose { get; set; }
        /// <summary>True for exactly one update when a head nod completes.</summary>
        public bool NodDetected { get; private set; }
        public HeadReading Head { get; private set; }
        public float HeadPitchDeviation { get { return nod.PitchDeviation; } }

        /// <summary>Windows display scaling seen by the Tobii source (1 while the mouse is used).</summary>
        public float DisplayScale { get { return IsSimulation || tobii == null ? 1f : tobii.DisplayScale; } }

        void Awake()
        {
            filter = new GazeFilter(settings);
            mouse = new MouseGazeSource();
            tobii = new TobiiGazeSource(settings);
            bool startWithTobii = TobiiCrashGuard.ShouldStartWithTobii();
            if (TobiiCrashGuard.CrashedLastTime) mouse.Notice = "Tobii crashed Unity last time (see README)";
            SetSource(startWithTobii ? (IGazeSource)tobii : mouse);
        }

        void OnDisable() { if (tobii != null) tobii.Deactivate(); }

        void Update() { Tick(Time.unscaledTime, Time.unscaledDeltaTime, Application.isFocused); }

        public void UseTobii() { SetSource(tobii); }

        public void UseMouse()
        {
            SetSource(mouse);
            TobiiCrashGuard.ForgetTobii();
        }

        /// <summary>Switches the gaze source (also used by the automated checks to inject a fake source).</summary>
        public void SetSource(IGazeSource next)
        {
            if (next == null || next == Source) return;
            if (Source != null) Source.Deactivate();
            Source = next;
            Source.Activate();
            Gaze = GazeSample.Invalid("Waiting for gaze");
            filter.Reset();
            blink.Reset();
            nod.Reset();
            NodDetected = false;
            if (SourceChanged != null) SourceChanged();
        }

        /// <summary>One input update. Called by Update, or directly with explicit time by the checks.</summary>
        public void Tick(float now, float deltaTime, bool windowFocused)
        {
            GazeSample sample = windowFocused ? Source.ReadGaze(now) : GazeSample.Invalid("Game window inactive - paused");
            if (sample.IsValid && !Source.IsSimulation)
                sample = GazeSample.Valid(filter.Apply(sample.ScreenPoint, deltaTime), sample.Status);
            Gaze = sample;
            blink.Update(sample, now);
            UpdateHead(now, deltaTime, windowFocused);
        }

        void UpdateHead(float now, float deltaTime, bool windowFocused)
        {
            NodDetected = false;
            if (!TrackHeadPose || !windowFocused)
            {
                nod.Reset();
                Head = HeadReading.Unavailable("");
                return;
            }
            Head = Source.ReadHead(now);
            if (Head.NodPressed) { NodDetected = true; return; }
            if (!Head.IsAvailable) { nod.Reset(); return; }
            NodDetected = nod.Update(Head.Pitch, Head.Yaw, now, deltaTime);
        }
    }
}
