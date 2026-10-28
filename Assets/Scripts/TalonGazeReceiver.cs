// Student: Jan Biernacki | Matriculation number: TO BE PROVIDED
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>Receives only local Talon gaze packets; never substitutes mouse data.</summary>
public sealed class TalonGazeReceiver : MonoBehaviour
{
    public const int Port = 27414;
    [Range(0.02f, 0.5f)] public float maximumSampleAge = 0.1f;
    public bool allowTestPackets;
    public bool IsTestData { get { return state.IsTestData; } }
    public string StatusText { get; private set; }

    readonly TalonGazeProtocolState state = new TalonGazeProtocolState();
    readonly UTF8Encoding utf8 = new UTF8Encoding(false, true);
    UdpClient socket;
    double retryAfter;
    int lastPollFrame = -1;
    Vector2 viewportTopLeft, viewportBottomRight;
    bool hasViewportBounds;
    double viewportBoundsPublishedAt;
    const double MaximumBoundsAgeSeconds = 0.25;

    void OnEnable()
    {
        state.Clear();
        retryAfter = 0;
        lastPollFrame = -1;
        StatusText = "Waiting for Talon gaze";
        OpenSocket();
    }

    void Update() { Poll(); }

    public void SetViewportBounds(Vector2 desktopTopLeft, Vector2 desktopBottomRight)
    {
        viewportTopLeft = desktopTopLeft;
        viewportBottomRight = desktopBottomRight;
        viewportBoundsPublishedAt = NowSeconds();
        hasViewportBounds = TalonGazeProtocolState.Finite(desktopTopLeft.x) &&
            TalonGazeProtocolState.Finite(desktopTopLeft.y) &&
            TalonGazeProtocolState.Finite(desktopBottomRight.x) &&
            TalonGazeProtocolState.Finite(desktopBottomRight.y) &&
            desktopBottomRight.x - desktopTopLeft.x > 1 && desktopBottomRight.y - desktopTopLeft.y > 1;
    }

    public bool TryGetScreenPoint(out Vector2 point)
    {
        point = Vector2.zero;
        if (!isActiveAndEnabled)
        {
            StatusText = "Talon input disabled";
            return false;
        }
        Poll();
        if (socket == null) return false;
        string status;
        double now = NowSeconds();
        bool boundsFresh = hasViewportBounds && now - viewportBoundsPublishedAt <= MaximumBoundsAgeSeconds;
        bool valid = state.TryGetScreenPoint(now, maximumSampleAge, allowTestPackets,
            Screen.width, Screen.height, boundsFresh, viewportTopLeft, viewportBottomRight,
            out point, out status);
        // Only replace the mapping-specific failure: no-gaze heartbeat messages stay intact.
        if (!valid && hasViewportBounds && !boundsFresh && status == "Waiting for Game view bounds")
            status = "Game view not visible or mapping stale - paused";
        StatusText = status;
        return valid;
    }

    void OpenSocket()
    {
        if (socket != null || NowSeconds() < retryAfter) return;
        try
        {
            socket = new UdpClient(AddressFamily.InterNetwork);
            socket.ExclusiveAddressUse = true;
            socket.Client.Bind(new IPEndPoint(IPAddress.Loopback, Port));
            socket.Client.Blocking = false;
            StatusText = "Waiting for Talon on 127.0.0.1:" + Port;
        }
        catch (SocketException)
        {
            CloseSocket();
            retryAfter = NowSeconds() + 2;
            StatusText = "Talon input unavailable: local port " + Port + " is busy";
        }
    }

    void Poll()
    {
        if (!isActiveAndEnabled || lastPollFrame == Time.frameCount) return;
        lastPollFrame = Time.frameCount;
        OpenSocket();
        if (socket == null) return;
        try
        {
            // A bounded drain prevents a sender from monopolizing Unity's main thread.
            for (int i = 0; i < 64 && socket.Available > 0; i++)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = socket.Receive(ref sender);
                if (!IPAddress.IsLoopback(sender.Address) || bytes.Length == 0 || bytes.Length > 4096) continue;
                try
                {
                    string json = utf8.GetString(bytes);
                    if (!json.TrimStart().StartsWith("{", StringComparison.Ordinal)) continue;
                    var packet = TalonGazePacket.Missing();
                    JsonUtility.FromJsonOverwrite(json, packet);
                    state.Accept(packet, UtcMilliseconds(), NowSeconds(), maximumSampleAge, allowTestPackets);
                }
                catch (DecoderFallbackException) { /* malformed UTF-8 is not gaze */ }
                catch (ArgumentException) { /* malformed JSON is not gaze */ }
            }
        }
        catch (SocketException exception)
        {
            if (exception.SocketErrorCode == SocketError.WouldBlock) return;
            CloseSocket();
            state.Clear();
            retryAfter = NowSeconds() + 2;
            StatusText = "Talon local connection interrupted - paused";
        }
        catch (ObjectDisposedException)
        {
            CloseSocket();
            state.Clear();
            StatusText = "Talon input stopped - paused";
        }
    }

    static double UtcMilliseconds() { return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds; }
    static double NowSeconds() { return (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency; }
    void CloseSocket() { if (socket != null) { socket.Close(); socket = null; } }
    void OnDisable() { CloseSocket(); state.Clear(); hasViewportBounds = false; StatusText = "Talon input disabled"; }
    void OnDestroy() { CloseSocket(); state.Clear(); }
}

[Serializable]
internal sealed class TalonGazePacket
{
    public int v, seq;
    public string session, space, device;
    public double sent_ms, sample_age_ms;
    public bool valid, test;
    public float x, y;

    // FromJsonOverwrite preserves these sentinels when required fields are absent.
    public static TalonGazePacket Missing()
    {
        return new TalonGazePacket { v = -1, seq = -1, sent_ms = double.NaN,
            sample_age_ms = double.NaN, x = float.NaN, y = float.NaN };
    }
}

/// <summary>Deterministic packet ordering, freshness and mapping; no socket or clock dependencies.</summary>
internal sealed class TalonGazeProtocolState
{
    string session;
    int sequence = -1;
    bool seenPacket, valid, viewportCoordinates;
    float x, y;
    double receivedAt, ageAtReceipt;
    string invalidStatus;
    public bool IsTestData { get; private set; }

    public void Clear()
    {
        session = null;
        sequence = -1;
        seenPacket = valid = IsTestData = false;
        invalidStatus = "Waiting for Talon gaze";
    }

    public bool Accept(TalonGazePacket packet, double utcMilliseconds, double monotonicSeconds,
        float maximumAgeSeconds, bool allowTest)
    {
        if (packet == null || packet.v != 1 || string.IsNullOrEmpty(packet.session) ||
            packet.session.Length > 128 || packet.seq < 0) return false;
        if (packet.session == session && packet.seq <= sequence) return false;
        if (packet.test && !allowTest) return false;

        // No-gaze heartbeats cancel immediately: they do not need a gaze timestamp.
        if (!packet.valid)
        {
            CommitIdentity(packet, monotonicSeconds);
            valid = false;
            invalidStatus = packet.device == "none" ? "Talon active - no gaze/tracker" : "Talon active - gaze unavailable";
            return true;
        }

        if (!Finite(packet.sent_ms) || !Finite(packet.sample_age_ms) || packet.sample_age_ms < 0 ||
            !Finite(packet.x) || !Finite(packet.y)) return false;
        double delayMilliseconds = utcMilliseconds - packet.sent_ms;
        double ageSeconds = (Math.Max(0, delayMilliseconds) + packet.sample_age_ms) / 1000;
        if (delayMilliseconds < -100 || ageSeconds > maximumAgeSeconds) return false;
        bool isViewport = packet.space == "viewport";
        if (packet.space != "desktop" && !isViewport) return false;
        if (isViewport && (!packet.test || packet.x < 0 || packet.x > 1 || packet.y < 0 || packet.y > 1)) return false;

        CommitIdentity(packet, monotonicSeconds);
        valid = true;
        viewportCoordinates = isViewport;
        x = packet.x;
        y = packet.y;
        ageAtReceipt = ageSeconds;
        return true;
    }

    void CommitIdentity(TalonGazePacket packet, double monotonicSeconds)
    {
        session = packet.session;
        sequence = packet.seq;
        seenPacket = true;
        receivedAt = monotonicSeconds;
        IsTestData = packet.test;
    }

    public bool TryGetScreenPoint(double monotonicSeconds, float maximumAgeSeconds, bool allowTest,
        int width, int height, bool hasBounds, Vector2 topLeft, Vector2 bottomRight,
        out Vector2 point, out string status)
    {
        point = Vector2.zero;
        status = "Waiting for Talon gaze";
        if (!seenPacket) return false;
        double elapsed = Math.Max(0, monotonicSeconds - receivedAt);
        if (IsTestData && !allowTest) { status = "Talon test packets disabled"; return false; }
        if (!valid)
        {
            status = elapsed <= 1 ? invalidStatus : "Talon bridge not responding - paused";
            return false;
        }
        if (ageAtReceipt + elapsed > maximumAgeSeconds)
        { status = "Talon gaze stale - paused"; return false; }
        if (width < 2 || height < 2) { status = "Waiting for Game view size"; return false; }

        double nx = x, ny = y;
        if (!viewportCoordinates)
        {
            if (!hasBounds) { status = "Waiting for Game view bounds"; return false; }
            double boundsWidth = bottomRight.x - topLeft.x, boundsHeight = bottomRight.y - topLeft.y;
            if (boundsWidth <= 1 || boundsHeight <= 1 || !Finite(boundsWidth) || !Finite(boundsHeight))
            { status = "Waiting for Game view bounds"; return false; }
            nx = (x - topLeft.x) / boundsWidth;
            ny = (y - topLeft.y) / boundsHeight;
            if (nx < 0 || nx >= 1 || ny < 0 || ny >= 1)
            { status = "Talon gaze outside Game view - paused"; return false; }
        }
        point = new Vector2((float)Math.Min(width - 1, nx * width),
            (float)Math.Min(height - 1, (1 - ny) * height));
        status = IsTestData ? "TALON TEST DATA - no eye tracker" : "Talon gaze active";
        return true;
    }

    internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
}
