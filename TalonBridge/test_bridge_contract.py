"""Protocol/validity tests using mock Talon APIs; no hardware or Talon runtime."""
import importlib.util
import json
from pathlib import Path
import sys
import types
import unittest
from unittest.mock import patch


class FakeSocket:
    def __init__(self, *args):
        self.packets = []
        self.closed = False
    def setblocking(self, value): pass
    def sendto(self, payload, destination):
        self.packets.append((json.loads(payload), destination))
    def close(self): self.closed = True


class FakeModule:
    def action_class(self, value): return value


def load_bridge():
    talon = types.ModuleType("talon")
    talon.Module = FakeModule
    talon.app = types.SimpleNamespace(register=lambda *a: None, unregister=lambda *a: None)
    talon.cron = types.SimpleNamespace(interval=lambda spec, cb: object(), cancel=lambda job: None)
    talon.ctrl = types.SimpleNamespace(mouse_pos=lambda: (123.0, 456.0))
    talon.tracking_system = types.SimpleNamespace(register=lambda *a: None, unregister=lambda *a: None)
    talon.ui = types.SimpleNamespace(main_screen=lambda: types.SimpleNamespace(rect=types.SimpleNamespace(x=-100.0, y=25.0, width=1600.0, height=1000.0)))
    talon.usb = types.SimpleNamespace(devices=lambda: [])
    track = types.ModuleType("talon.track")
    track.tobii = types.SimpleNamespace(TobiiEC=type("TobiiEC", (), {}))
    path = Path(__file__).with_name("gazelab_talon_bridge.py")
    spec = importlib.util.spec_from_file_location("bridge_under_test", path)
    module = importlib.util.module_from_spec(spec)
    with patch.dict(sys.modules, {"talon": talon, "talon.track": track}), patch("socket.socket", FakeSocket):
        spec.loader.exec_module(module)
    return module


BRIDGE = load_bridge()


class BridgeContractTests(unittest.TestCase):
    def setUp(self):
        self.patch_socket = patch("socket.socket", FakeSocket)
        self.patch_socket.start()
        self.bridge = BRIDGE.GazeLabBridge()
        self.now = 1000.0
        self.patch_clock = patch.object(BRIDGE.time, "perf_counter", lambda: self.now)
        self.patch_clock.start()
    def tearDown(self):
        self.bridge.stop()
        self.patch_clock.stop()
        self.patch_socket.stop()
    def frame(self, x=0.5, y=0.25, age=0.01, eyes=True):
        return types.SimpleNamespace(gaze=types.SimpleNamespace(x=x, y=y), ts=self.now-age, left=eyes, right=False)
    def packet(self): return self.bridge.socket.packets[-1][0]

    def test_real_frame_maps_normalized_gaze_to_desktop_points(self):
        self.bridge.on_gaze(self.frame())
        p = self.packet()
        self.assertTrue(p["valid"])
        self.assertFalse(p["test"])
        self.assertEqual(p["device"], "Tobii via Talon")
        self.assertEqual((p["x"], p["y"]), (700.0, 275.0))
        self.assertAlmostEqual(p["sample_age_ms"], 10.0)
        self.assertEqual(p["space"], "desktop")
        self.assertEqual(self.bridge.socket.packets[-1][1], ("127.0.0.1", 27414))

    def test_invalid_eyes_stale_and_outside_gaze_never_become_valid(self):
        for frame in [self.frame(eyes=False), self.frame(age=0.2), self.frame(age=-0.1), self.frame(x=-0.1), self.frame(x=float("nan"))]:
            self.bridge.on_gaze(frame)
            self.assertFalse(self.packet()["valid"])

    def test_heartbeat_reports_missing_tracker_and_never_replays_gaze(self):
        self.bridge.on_gaze(self.frame())
        self.now += 0.3
        self.bridge.heartbeat()
        p = self.packet()
        self.assertFalse(p["valid"])
        self.assertEqual((p["x"], p["y"]), (0.0, 0.0))
        self.assertIn("No Tobii", p["reason"])
        self.assertEqual(p["device"], "none")
        self.assertFalse(json.loads(self.bridge.status())["tracker_detected"])

    def test_pointer_test_is_explicit_and_labelled(self):
        self.assertIsNone(self.bridge.pointer_test_job)
        self.bridge.start_pointer_test()
        p = self.packet()
        self.assertTrue(p["test"])
        self.assertTrue(p["valid"])
        self.assertEqual((p["x"], p["y"]), (123.0, 456.0))
        count = len(self.bridge.socket.packets)
        self.bridge.on_gaze(self.frame())
        self.assertEqual(len(self.bridge.socket.packets), count)
        self.bridge.stop_pointer_test()
        self.assertFalse(self.packet()["valid"])
        self.bridge.on_gaze(self.frame())
        self.assertTrue(self.packet()["valid"])
        self.assertFalse(self.packet()["test"])

    def test_sequence_increases_and_stop_closes_transport(self):
        self.bridge.start()
        first = self.packet()
        self.bridge.on_gaze(self.frame())
        second = self.packet()
        self.assertEqual(first["session"], second["session"])
        self.assertGreater(second["seq"], first["seq"])
        self.bridge.stop()
        self.assertFalse(self.packet()["valid"])
        self.assertTrue(self.bridge.socket.closed)
        count = len(self.bridge.socket.packets)
        self.bridge.on_gaze(self.frame())
        self.assertEqual(len(self.bridge.socket.packets), count)


if __name__ == "__main__":
    unittest.main()
