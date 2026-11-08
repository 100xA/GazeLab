// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using System;
using UnityEngine;

namespace GazeLab
{
    [DisallowMultipleComponent]
    public class GazeInput : MonoBehaviour
    {
        public GazeInputSettings settings = new GazeInputSettings();

        readonly BlinkDetector blink = new BlinkDetector();
        readonly HeadNodDetector nod = new HeadNodDetector();
        GazeFilter filter;
        MouseGazeSource mouse;
        TobiiGazeSource tobii;

        public event Action SourceChanged;

        public IGazeSource Source { get; private set; }
        public bool IsSimulation { get { return Source != null && Source.IsSimulation; } }
        public GazeSample Gaze { get; private set; }
        public bool HasValidGaze { get { return Gaze.IsValid; } }
        public Vector2 ScreenPoint { get { return Gaze.ScreenPoint; } }
        public string StatusText { get { return Gaze.Status; } }
        public BlinkDetector Blink { get { return blink; } }

        public bool TrackHeadPose { get; set; }
        public bool NodDetected { get; private set; }
        public HeadReading Head { get; private set; }
        public float HeadPitchDeviation { get { return nod.PitchDeviation; } }

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

        void OnEnable() { if (Source != null && Source == tobii) tobii.Activate(); }

        void OnDisable() { if (tobii != null) tobii.Deactivate(); }

        void Update() { Tick(Time.unscaledTime, Time.unscaledDeltaTime, Application.isFocused); }

        public void UseTobii() { SetSource(tobii); }

        public void UseMouse() { SetSource(mouse); }

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
