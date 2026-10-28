"""Talon 0.4 gaze -> GazeLab over loopback UDP. No gaze history is stored.

Student: Jan Biernacki | Matriculation number: TO BE PROVIDED

The private tracking callback API was checked against Talon 0.4's packaged
eye_mouse.py and the author's TalonEyeTracker implementation:
https://github.com/wolfmanstout/gaze-ocr/blob/master/gaze_ocr/talon_adapter.py
Talon may change this private API in a future release.

Install only this file under ~/.talon/user/gazelab/. No voice command set,
microphone, screen capture, or synthetic mouse control is used by this bridge.
"""

import atexit
import json
import math
import socket
import sys
import threading
import time
import types
import uuid

from talon import Module, app, cron, ctrl, tracking_system, ui, usb
from talon.track import tobii


DESTINATION = ("127.0.0.1", 27414)
MAX_SAMPLE_AGE_MS = 100.0
_LIFECYCLE_KEY = "_gazelab_talon_bridge_lifecycle_v1"


class GazeLabBridge:
    def __init__(self):
        self.session = str(uuid.uuid4())
        self.sequence = 0
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.setblocking(False)
        self.lock = threading.RLock()
        self.heartbeat_job = None
        self.pointer_test_job = None
        self.subscribed = False
        self.closed = False
        self.last_callback_at = float("-inf")
        self.last_status = "Waiting for Talon readiness"
        self.last_send_error = None

    def _send(self, valid, x=0.0, y=0.0, sample_age_ms=0.0, test=False, reason="", device="Tobii via Talon"):
        with self.lock:
            if self.closed:
                return
            self.sequence += 1
            packet = {
                "v": 1,
                "source": "talon",
                "session": self.session,
                "seq": self.sequence,
                "sent_ms": int(time.time() * 1000),
                "sample_age_ms": float(sample_age_ms),
                "valid": bool(valid),
                "x": float(x),
                "y": float(y),
                "space": "desktop",
                "test": bool(test),
                "device": device,
                "reason": reason,
            }
            try:
                payload = json.dumps(packet, allow_nan=False, separators=(",", ":")).encode("utf-8")
                self.socket.sendto(payload, DESTINATION)
                self.last_send_error = None
            except (OSError, ValueError) as error:
                self.last_send_error = type(error).__name__ + ": " + str(error)

    @staticmethod
    def device_status():
        # This is the same enumeration/type test used by packaged eye_mouse.py.
        try:
            present = any(isinstance(device, tobii.TobiiEC) for device in usb.devices())
            return present, "Tobii tracker detected" if present else "No Tobii eye tracker detected"
        except Exception as error:
            return False, "Device enumeration unavailable: " + type(error).__name__

    def on_gaze(self, frame):
        with self.lock:
            if self.closed or self.pointer_test_job is not None:
                return
            self.last_callback_at = time.perf_counter()
            if frame is None or frame.gaze is None or not (frame.left or frame.right):
                self.last_status = "No valid eye sample"
                self._send(False, reason=self.last_status)
                return
            try:
                age_ms = (time.perf_counter() - float(frame.ts)) * 1000.0
                gx, gy = float(frame.gaze.x), float(frame.gaze.y)
                if not all(math.isfinite(value) for value in (age_ms, gx, gy)):
                    raise ValueError("nonfinite gaze")
                if age_ms < 0.0 or age_ms > MAX_SAMPLE_AGE_MS:
                    self.last_status = "Gaze sample stale or timestamp invalid"
                    self._send(False, sample_age_ms=max(0.0, age_ms), reason=self.last_status)
                    return
                if not (0.0 <= gx <= 1.0 and 0.0 <= gy <= 1.0):
                    self.last_status = "Gaze outside tracked display"
                    self._send(False, sample_age_ms=age_ms, reason=self.last_status)
                    return
                # GazeFrame.gaze is normalized, top-left based. This is the
                # display rectangle used by Talon's packaged calibration code.
                rect = ui.main_screen().rect
                x = float(rect.x) + gx * float(rect.width)
                y = float(rect.y) + gy * float(rect.height)
                if not (math.isfinite(x) and math.isfinite(y)):
                    raise ValueError("nonfinite display coordinates")
                self.last_status = "Real Talon gaze active"
                self._send(True, x, y, age_ms, reason=self.last_status)
            except (AttributeError, TypeError, ValueError) as error:
                self.last_status = "Unsupported gaze frame: " + type(error).__name__
                self._send(False, reason=self.last_status)

    def heartbeat(self):
        with self.lock:
            if self.closed or self.pointer_test_job is not None:
                return
            if time.perf_counter() - self.last_callback_at < 0.25:
                return
            present, self.last_status = self.device_status()
            device = "Tobii via Talon" if present else "none" if self.last_status == "No Tobii eye tracker detected" else "unknown"
            if self.last_status == "Tobii tracker detected":
                self.last_status = "Tracker detected; waiting for fresh gaze"
            self._send(False, reason=self.last_status, device=device)

    def start(self):
        with self.lock:
            if self.closed or self.subscribed:
                return
            tracking_system.register("gaze", self.on_gaze)
            self.subscribed = True
            self.heartbeat_job = cron.interval("250ms", self.heartbeat)
            self.heartbeat()
            print("GazeLab Talon bridge ready on loopback UDP 27414; real gaze only.")

    def start_pointer_test(self):
        """Explicit transport test; it never masquerades as an eye sample."""
        with self.lock:
            if self.closed:
                raise RuntimeError("Bridge has been stopped")
            if self.pointer_test_job is None:
                self.pointer_test_job = cron.interval("16ms", self.pointer_test_tick)
            self.last_status = "EXPLICIT POINTER TRANSPORT TEST - no eye tracking"
            self.pointer_test_tick()

    def pointer_test_tick(self):
        with self.lock:
            if self.closed or self.pointer_test_job is None:
                return
            x, y = ctrl.mouse_pos()
            self._send(True, x, y, 0.0, test=True, reason="EXPLICIT POINTER TRANSPORT TEST")

    def stop_pointer_test(self):
        with self.lock:
            if self.pointer_test_job is not None:
                cron.cancel(self.pointer_test_job)
                self.pointer_test_job = None
            self.last_callback_at = float("-inf")
            self.last_status = "Pointer transport test stopped; waiting for real gaze"
            self._send(False, reason=self.last_status)

    def status(self):
        present, device_message = self.device_status()
        return json.dumps({
            "session": self.session,
            "subscribed": self.subscribed,
            "closed": self.closed,
            "tracker_detected": present,
            "device_status": device_message,
            "pointer_test": self.pointer_test_job is not None,
            "status": self.last_status,
            "send_error": self.last_send_error,
        }, sort_keys=True)

    def stop(self):
        with self.lock:
            if self.closed:
                return
            self._send(False, reason="Talon bridge stopping")
            for job in (self.heartbeat_job, self.pointer_test_job):
                if job is not None:
                    try:
                        cron.cancel(job)
                    except Exception:
                        pass
            self.heartbeat_job = self.pointer_test_job = None
            if self.subscribed:
                try:
                    tracking_system.unregister("gaze", self.on_gaze)
                except Exception:
                    pass
            self.subscribed = False
            try:
                app.unregister("ready", self.start)
            except Exception:
                pass
            self.closed = True
            self.socket.close()


# Talon scripts reload automatically. Retain one owned cleanup hook so an old
# socket, callback or test timer cannot remain active after a script reload.
_lifecycle = sys.modules.setdefault(_LIFECYCLE_KEY, types.ModuleType(_LIFECYCLE_KEY))
_previous = getattr(_lifecycle, "bridge", None)
if _previous is not None:
    _previous.stop()
    atexit.unregister(_previous.stop)
bridge = GazeLabBridge()
_lifecycle.bridge = bridge
atexit.register(bridge.stop)
app.register("ready", bridge.start)


mod = Module()


@mod.action_class
class Actions:
    def gazelab_talon_status() -> str:
        """Report bridge subscription and actual tracker enumeration status."""
        return bridge.status()

    def gazelab_talon_pointer_test_start():
        """Start explicitly labelled pointer packets for transport testing only."""
        bridge.start_pointer_test()

    def gazelab_talon_pointer_test_stop():
        """Stop pointer test packets and return to real-gaze-only input."""
        bridge.stop_pointer_test()

    def gazelab_talon_bridge_stop():
        """Stop the owned UDP bridge and unregister its callbacks and timers."""
        bridge.stop()
